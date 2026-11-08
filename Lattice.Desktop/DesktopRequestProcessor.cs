using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Lattice.Core;

namespace Lattice.Desktop;

internal sealed class DesktopRequestProcessor(
    RequestInterpreter? interpreter = null,
    int maxDisplayedCandidatePaths = DesktopRequestProcessor.DefaultMaxDisplayedCandidatePaths)
{
    internal const int DefaultMaxDisplayedCandidatePaths = 10;
    internal const string ShowThatFilePatternId = "inspect.show-that-file";
    internal const string CandidateSelectionPatternId = "inspect.candidate-selection";
    private const string BroadFindPatternId = "find.symbol";
    private const string CapabilitiesHelpText =
        "I can open UTF-8 files in the connected workspace (for example, \"Show me Program.cs\" or \"Show me README.md\") or find where an identifier appears in source text (for example, \"Where is WorkspaceRoot used in this project?\").";

    private readonly RequestInterpreter _interpreter = interpreter ?? CreateDefaultInterpreter();
    private readonly int _maxDisplayedCandidatePaths = Math.Max(1, maxDisplayedCandidatePaths);

    public static DesktopRequestProcessor Default { get; } = new();

    public RequestInterpreter Interpreter => _interpreter;

    public DesktopTurnEntry Execute(
        ConnectedWorkspace? workspace,
        string rawRequest,
        RecentFileContext? recentFileContext = null)
    {
        string requestText = rawRequest?.Trim() ?? string.Empty;
        if (workspace is null)
        {
            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InvalidWorkspace,
                "I'm not connected to a workspace yet. Connect a local project folder in the header bar above, then ask me about a file or identifier.",
                WorkspacePath: null);
        }

        RecentFileContext activeContext = recentFileContext ?? RecentFileContext.None;
        RequestInterpretation interpretation = _interpreter.Interpret(requestText);
        if (interpretation.Kind == IntentMatchKind.Unknown
            && TryResolveCandidateSelection(requestText, activeContext, out string? selectedRelativePath))
        {
            return ExecuteDirectInspect(
                workspace,
                requestText,
                selectedRelativePath!,
                CandidateSelectionPatternId);
        }

        return interpretation.Kind switch
        {
            IntentMatchKind.Ambiguous => BuildAmbiguousEntry(workspace, requestText, interpretation),
            IntentMatchKind.MissingValue => BuildMissingValueEntry(workspace, requestText, interpretation),
            IntentMatchKind.Unknown => BuildUnsupportedEntry(
                workspace,
                requestText,
                $"I'm not able to answer that kind of request yet. {CapabilitiesHelpText}"),
            IntentMatchKind.Matched => ExecuteMatched(workspace, requestText, interpretation, activeContext),
            _ => BuildUnsupportedEntry(
                workspace,
                requestText,
                $"I'm not able to answer that kind of request yet. {CapabilitiesHelpText}"),
        };
    }

    internal static bool IsFollowUpReferenceTurn(DesktopTurnEntry entry)
    {
        if (entry.MatchedPatternId == ShowThatFilePatternId)
        {
            return true;
        }

        return entry.CapturedValue is not null && IsFollowUpFileReference(entry.CapturedValue);
    }

    private DesktopTurnEntry ExecuteMatched(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation,
        RecentFileContext recentFileContext)
    {
        IntentMatch match = interpretation.Match!;
        if (interpretation.Intent == RequestIntentKind.InspectPath)
        {
            if (match.PatternId == ShowThatFilePatternId)
            {
                return ExecuteFollowUpInspect(
                    workspace,
                    requestText,
                    recentFileContext,
                    match.PatternId,
                    capturedReference: "that file");
            }

            if (match.Slot is not null)
            {
                string requestedPath = UnwrapMatchingQuotes(match.Slot.Value.Trim());
                if (IsFollowUpFileReference(requestedPath))
                {
                    return ExecuteFollowUpInspect(
                        workspace,
                        requestText,
                        recentFileContext,
                        match.PatternId,
                        requestedPath);
                }

                return ExecuteInspectWithBasenameFallback(
                    workspace,
                    requestText,
                    requestedPath,
                    match.PatternId);
            }
        }

        if (interpretation.Intent == RequestIntentKind.FindSymbol && match.Slot is not null)
        {
            if (match.PatternId == BroadFindPatternId)
            {
                string target = UnwrapMatchingQuotes(match.Slot.Value.Trim());
                return BuildUnsupportedEntry(
                    workspace,
                    requestText,
                    $"I'm not sure whether you want to open a file or search for occurrences of '{target}'. Try asking \"Where does {target} appear?\" or \"Show me {target}\".",
                    matchedPatternId: match.PatternId,
                    capturedValue: target);
            }

            string symbol = UnwrapMatchingQuotes(match.Slot.Value.Trim());
            return ExecuteFindSymbol(workspace, requestText, interpretation, symbol, match.PatternId);
        }

        if (interpretation.Intent is RequestIntentKind.Question or RequestIntentKind.Explanation)
        {
            return BuildUnsupportedEntry(
                workspace,
                requestText,
                $"I can't explain code behavior or answer open-ended conceptual questions — I only report directly observed workspace files and literal text matches. {CapabilitiesHelpText}",
                matchedPatternId: match.PatternId,
                capturedValue: match.Slot?.Value);
        }

        return BuildUnsupportedEntry(
            workspace,
            requestText,
            $"I'm not able to handle that request yet. {CapabilitiesHelpText}",
            matchedPatternId: match.PatternId,
            capturedValue: match.Slot?.Value);
    }

    private DesktopTurnEntry ExecuteInspectWithBasenameFallback(
        ConnectedWorkspace workspace,
        string requestText,
        string requestedPath,
        string patternId)
    {
        if (string.IsNullOrEmpty(requestedPath))
        {
            return BuildUnsupportedEntry(
                workspace,
                requestText,
                "Which file in the connected workspace would you like me to open?",
                matchedPatternId: patternId);
        }

        // Exact relative path always takes precedence.
        DesktopTurnEntry directResult = ExecuteDirectInspect(
            workspace,
            requestText,
            requestedPath,
            patternId);

        if (!WorkspaceFileLocator.IsBareFileName(requestedPath)
            || directResult.Kind != DesktopTurnKind.MissingFile)
        {
            return directResult;
        }

        // Preserve root-level precedence case-insensitively across platforms before nested lookup.
        Result<string> rootMatch = workspace.FileLocator.TryFindSingleRootFile(requestedPath);
        if (!rootMatch.IsSuccess)
        {
            return MapFileError(workspace, requestText, requestedPath, patternId, rootMatch.Error!);
        }

        if (!string.IsNullOrEmpty(rootMatch.Value))
        {
            return ExecuteDirectInspect(
                workspace,
                requestText,
                rootMatch.Value,
                patternId,
                capturedValueOverride: requestedPath);
        }

        Result<WorkspaceFileLookupResult> lookupResult = workspace.FileLocator.FindByFileName(requestedPath);
        if (!lookupResult.IsSuccess)
        {
            return MapFileError(workspace, requestText, requestedPath, patternId, lookupResult.Error!);
        }

        WorkspaceFileLookupResult lookup = lookupResult.Value;
        if (lookup.TotalMatches == 0)
        {
            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.MissingFile,
                $"I couldn't find a file named '{requestedPath}' anywhere in the connected workspace.",
                workspace.Root.FullPath,
                InspectedRelativePath: requestedPath,
                ErrorCode: FileToolErrorCodes.PathNotFound,
                MatchedPatternId: patternId,
                CapturedValue: requestedPath);
        }

        if (lookup.TotalMatches == 1 && lookup.SafeRelativePaths.Length == 1)
        {
            return ExecuteDirectInspect(
                workspace,
                requestText,
                lookup.SafeRelativePaths[0],
                patternId,
                capturedValueOverride: requestedPath);
        }

        return BuildMultipleBasenameMatchesEntry(
            workspace,
            requestText,
            requestedPath,
            patternId,
            lookup);
    }

    private DesktopTurnEntry BuildMultipleBasenameMatchesEntry(
        ConnectedWorkspace workspace,
        string requestText,
        string requestedFileName,
        string patternId,
        WorkspaceFileLookupResult lookup)
    {
        ImmutableArray<string> displayed = [.. lookup.SafeRelativePaths.Take(_maxDisplayedCandidatePaths)];
        int omittedCount = Math.Max(0, lookup.TotalMatches - displayed.Length);

        string lead;
        if (lookup.HasMoreMatches)
        {
            lead =
                $"I found more than {lookup.TotalMatches} files named '{requestedFileName}' in the connected workspace (showing the first {displayed.Length}; at least {omittedCount} more omitted). Which file would you like me to open? Reply with one of the relative paths below:";
        }
        else if (omittedCount > 0)
        {
            string omittedLabel = omittedCount == 1 ? "1 more omitted" : $"{omittedCount} more omitted";
            lead =
                $"I found {lookup.TotalMatches} files named '{requestedFileName}' in the connected workspace (showing the first {displayed.Length}; {omittedLabel}). Which file would you like me to open? Reply with one of the relative paths below:";
        }
        else
        {
            lead =
                $"I found {lookup.TotalMatches} files named '{requestedFileName}' in the connected workspace. Which file would you like me to open? Reply with one of the relative paths below:";
        }

        StringBuilder fullSummary = new(lead);
        for (int i = 0; i < displayed.Length; i++)
        {
            fullSummary.Append('\n').Append(i + 1).Append(". ").Append(displayed[i]);
        }

        return new DesktopTurnEntry(
            requestText,
            DesktopTurnKind.AmbiguousInput,
            fullSummary.ToString(),
            workspace.Root.FullPath,
            MatchedPatternId: patternId,
            CapturedValue: requestedFileName,
            CandidatePaths: displayed,
            AllCandidatePaths: lookup.SafeRelativePaths,
            TotalCandidateCount: lookup.TotalMatches,
            HasMoreCandidates: lookup.HasMoreMatches,
            LeadSummary: lead);
    }

    private static bool TryResolveCandidateSelection(
        string requestText,
        RecentFileContext recentFileContext,
        out string? selectedRelativePath)
    {
        selectedRelativePath = null;
        if (recentFileContext.Kind != RecentFileContextKind.MultipleFiles
            || recentFileContext.CandidateRelativePaths.IsEmpty)
        {
            return false;
        }

        string normalized = NormalizeCandidateReply(requestText);
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        if (int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out int candidateNumber)
            && candidateNumber >= 1
            && candidateNumber <= Math.Min(
                recentFileContext.CandidateRelativePaths.Length,
                DefaultMaxDisplayedCandidatePaths))
        {
            selectedRelativePath = recentFileContext.CandidateRelativePaths[candidateNumber - 1];
            return true;
        }

        string? exactCandidate = recentFileContext.CandidateRelativePaths
            .FirstOrDefault(candidate => candidate.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (exactCandidate is not null)
        {
            selectedRelativePath = exactCandidate;
            return true;
        }

        if (!string.IsNullOrEmpty(recentFileContext.AmbiguousFileName)
            && normalized.Contains('/', StringComparison.Ordinal)
            && Path.GetFileName(normalized).Equals(
                recentFileContext.AmbiguousFileName,
                StringComparison.OrdinalIgnoreCase))
        {
            selectedRelativePath = normalized;
            return true;
        }

        return false;
    }

    private static string NormalizeCandidateReply(string rawInput)
    {
        string value = UnwrapMatchingQuotes(rawInput.Trim());
        while (value.Length > 1 && value[^1] is '.' or '?' or '!')
        {
            // Trim ordinary sentence-final punctuation, e.g. "Lattice.Desktop/Program.cs."
            value = value[..^1].TrimEnd();
            value = UnwrapMatchingQuotes(value);
        }

        return value.Replace('\\', '/');
    }

    private static DesktopTurnEntry ExecuteFollowUpInspect(
        ConnectedWorkspace workspace,
        string requestText,
        RecentFileContext recentFileContext,
        string patternId,
        string capturedReference)
    {
        if (recentFileContext.Kind == RecentFileContextKind.SingleFile
            && !string.IsNullOrWhiteSpace(recentFileContext.SingleRelativePath))
        {
            return ExecuteDirectInspect(
                workspace,
                requestText,
                recentFileContext.SingleRelativePath,
                patternId,
                capturedValueOverride: capturedReference);
        }

        if (recentFileContext.Kind == RecentFileContextKind.MultipleFiles
            && !recentFileContext.CandidateRelativePaths.IsEmpty)
        {
            ImmutableArray<string> candidates = recentFileContext.CandidateRelativePaths;
            const int maxListed = 5;
            string listed = string.Join(
                ", ",
                candidates.Take(maxListed).Select(path => $"'{path}'"));
            string summary = candidates.Length <= maxListed
                ? $"Multiple files are in context ({listed}). Which file would you like me to open? Reply with its relative path."
                : $"Multiple files ({candidates.Length}) are in context, including {listed}. Which file would you like me to open? Reply with its relative path.";

            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.AmbiguousInput,
                summary,
                workspace.Root.FullPath,
                MatchedPatternId: patternId,
                CapturedValue: capturedReference);
        }

        return new DesktopTurnEntry(
            requestText,
            DesktopTurnKind.AmbiguousInput,
            "There isn't a single recent file in our conversation yet. Which file in the connected workspace would you like me to open?",
            workspace.Root.FullPath,
            MatchedPatternId: patternId,
            CapturedValue: capturedReference);
    }

    private static bool IsFollowUpFileReference(string candidate)
    {
        return candidate.Equals("it", StringComparison.OrdinalIgnoreCase)
            || candidate.Equals("that", StringComparison.OrdinalIgnoreCase)
            || candidate.Equals("that file", StringComparison.OrdinalIgnoreCase)
            || candidate.Equals("this file", StringComparison.OrdinalIgnoreCase);
    }

    private static DesktopTurnEntry ExecuteFindSymbol(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation,
        string symbol,
        string patternId)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return BuildUnsupportedEntry(
                workspace,
                requestText,
                "Which identifier would you like me to look for? Try asking \"Where does WorkspaceRoot appear?\"",
                matchedPatternId: patternId);
        }

        if (symbol.Any(char.IsWhiteSpace))
        {
            return BuildUnsupportedEntry(
                workspace,
                requestText,
                $"I can search for one identifier name at a time, and '{symbol}' contains spaces. Ask about a single type, member, or token name instead.",
                matchedPatternId: patternId,
                capturedValue: symbol);
        }

        RequestInterpretation normalizedInterpretation =
            interpretation.Match!.Slot!.Value == symbol
                ? interpretation
                : RequestInterpretation.Matched(
                    interpretation.Text,
                    new IntentMatch(
                        patternId,
                        RequestIntentKind.FindSymbol,
                        new IntentSlot(interpretation.Match.Slot.Name, symbol)));

        EvidenceAnswer answer = workspace.QuestionAnswerer.Answer(normalizedInterpretation);
        return answer.Status switch
        {
            EvidenceAnswerStatus.Answered => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.SearchResults,
                FormatSearchAnsweredSummary(symbol, answer.Evidence),
                workspace.Root.FullPath,
                SearchQuery: symbol,
                Evidence: answer.Evidence,
                MatchedPatternId: patternId,
                CapturedValue: symbol),
            EvidenceAnswerStatus.InsufficientEvidence when answer.Text.StartsWith(
                "No occurrences of ",
                StringComparison.Ordinal) => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.NoMatches,
                $"I searched the connected workspace and found no literal source-text occurrences of '{symbol}'.",
                workspace.Root.FullPath,
                SearchQuery: symbol,
                MatchedPatternId: patternId,
                CapturedValue: symbol),
            EvidenceAnswerStatus.InsufficientEvidence => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"I couldn't complete the search for '{symbol}' due to a workspace read error.",
                workspace.Root.FullPath,
                SearchQuery: symbol,
                MatchedPatternId: patternId,
                CapturedValue: symbol),
            _ => BuildUnsupportedEntry(
                workspace,
                requestText,
                $"I couldn't run that search request. {CapabilitiesHelpText}",
                matchedPatternId: patternId,
                capturedValue: symbol),
        };
    }

    private static string FormatSearchAnsweredSummary(
        string symbol,
        ImmutableArray<EvidenceReference> evidence)
    {
        int occurrenceCount = evidence.Length;
        int fileCount = evidence
            .Select(reference => reference.RelativePath)
            .Distinct(StringComparer.Ordinal)
            .Count();
        string occurrencePhrase = occurrenceCount == 1
            ? "1 matching occurrence"
            : $"{occurrenceCount} matching occurrences";
        if (fileCount == 1)
        {
            return $"I found {occurrencePhrase} of '{symbol}' in 1 file.";
        }

        return $"I found {occurrencePhrase} of '{symbol}' across {fileCount} files.";
    }

    private static DesktopTurnEntry ExecuteDirectInspect(
        ConnectedWorkspace workspace,
        string requestText,
        string relativePath,
        string patternId,
        string? capturedValueOverride = null)
    {
        string capturedValue = capturedValueOverride ?? relativePath;
        if (string.IsNullOrEmpty(relativePath))
        {
            return BuildUnsupportedEntry(
                workspace,
                requestText,
                "Which file in the connected workspace would you like me to open?",
                matchedPatternId: patternId);
        }

        if (!workspace.Registry.TryResolve(workspace.Toolset.Read, out ITool? readTool))
        {
            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.UnexpectedError,
                "File reading is currently unavailable in this workspace session.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                MatchedPatternId: patternId,
                CapturedValue: capturedValue);
        }

        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("path", ArgumentValue.FromString(relativePath)),
        ]);
        ToolInvocation invocation = ToolExecutor.Execute(readTool, arguments, workspace.Policy);
        if (invocation.Result.IsSuccess)
        {
            string contents = invocation.Result.Output!.AsString();
            int lines = contents.Length == 0 ? 0 : contents.Split('\n').Length;
            string lineLabel = lines == 1 ? "1 line" : $"{lines} lines";
            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.FileInspected,
                $"Here is '{relativePath}' ({lineLabel}).",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                FileContents: contents,
                LineCount: lines,
                MatchedPatternId: patternId,
                CapturedValue: capturedValue);
        }

        return MapFileError(workspace, requestText, relativePath, patternId, invocation.Result.Error!, capturedValue);
    }

    private static DesktopTurnEntry MapFileError(
        ConnectedWorkspace workspace,
        string requestText,
        string relativePath,
        string patternId,
        Error error,
        string? capturedValueOverride = null)
    {
        string capturedValue = capturedValueOverride ?? relativePath;
        return error.Code switch
        {
            FileToolErrorCodes.PathNotFound => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.MissingFile,
                $"'{relativePath}' was not found in the connected workspace. Check the relative path and try again.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: capturedValue),
            FileToolErrorCodes.PathOutsideRoot => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.AccessRejected,
                $"I can only read files inside the connected workspace folder, so I can't open '{relativePath}'. Please give me a relative path within this project.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: capturedValue),
            FileToolErrorCodes.ReparsePointNotAllowed => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.AccessRejected,
                $"I can't open '{relativePath}' because symbolic links and reparse points are blocked for workspace safety.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: capturedValue),
            FileToolErrorCodes.NotAFile => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"'{relativePath}' is a directory, not a file. Ask me to open a specific file inside that folder instead.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: capturedValue),
            FileToolErrorCodes.FileTooLarge => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"'{relativePath}' is larger than the {ReadFileTool.MaxBytes:N0}-byte limit for inline viewing.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: capturedValue),
            FileToolErrorCodes.EncodingInvalid => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"'{relativePath}' isn't valid UTF-8 text, so I can't display its contents safely.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: capturedValue),
            _ => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"I couldn't read '{relativePath}' due to a file-access error in the workspace.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: capturedValue),
        };
    }

    private static DesktopTurnEntry BuildAmbiguousEntry(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation)
    {
        ImmutableArray<RequestIntentKind> intents = [.. interpretation.Candidates
            .Select(candidate => candidate.Intent)
            .Distinct(),];
        string? sharedTarget = interpretation.Candidates
            .Select(candidate => candidate.Slot?.Value.Trim())
            .Where(value => !string.IsNullOrEmpty(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SingleOrDefault();

        string question;
        if (sharedTarget is not null
            && intents.Contains(RequestIntentKind.InspectPath)
            && intents.Contains(RequestIntentKind.FindSymbol))
        {
            question =
                $"Did you want me to open the file '{sharedTarget}' or search where the identifier '{sharedTarget}' appears in this project?";
        }
        else if (sharedTarget is not null && intents.Contains(RequestIntentKind.InspectPath))
        {
            question =
                $"Did you want me to open the file '{sharedTarget}'? Ask \"Show me {sharedTarget}\" to view its contents, or ask where an identifier appears.";
        }
        else
        {
            ClarificationRequest? clarification = ClarificationGenerator.TryCreate(interpretation);
            question = clarification?.Question
                ?? "Could you clarify whether you want to open a specific file or search where an identifier appears in this project?";
        }

        return new DesktopTurnEntry(
            requestText,
            DesktopTurnKind.AmbiguousInput,
            question,
            workspace.Root.FullPath);
    }

    private static DesktopTurnEntry BuildMissingValueEntry(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation)
    {
        string message = interpretation.Intent switch
        {
            RequestIntentKind.InspectPath =>
                "Which file in the connected workspace would you like me to open? Include its name or relative path, such as \"Show me Program.cs\" or \"Show me README.md\".",
            RequestIntentKind.FindSymbol =>
                "Which identifier would you like me to search for in this project? For example, ask \"Where does WorkspaceRoot appear?\"",
            _ =>
                $"Could you be a bit more specific? {CapabilitiesHelpText}",
        };
        return new DesktopTurnEntry(
            requestText,
            DesktopTurnKind.UnsupportedInput,
            message,
            workspace.Root.FullPath,
            MatchedPatternId: interpretation.Match?.PatternId);
    }

    private static DesktopTurnEntry BuildUnsupportedEntry(
        ConnectedWorkspace workspace,
        string requestText,
        string message,
        string? matchedPatternId = null,
        string? capturedValue = null)
    {
        return new DesktopTurnEntry(
            requestText,
            DesktopTurnKind.UnsupportedInput,
            message,
            workspace.Root.FullPath,
            MatchedPatternId: matchedPatternId,
            CapturedValue: capturedValue);
    }

    private static string UnwrapMatchingQuotes(string value)
    {
        if (value.Length >= 2)
        {
            char first = value[0];
            char last = value[^1];
            if ((first == '"' && last == '"')
                || (first == '\'' && last == '\'')
                || (first == '`' && last == '`'))
            {
                return value[1..^1].Trim();
            }
        }

        return value;
    }

    private static RequestInterpreter CreateDefaultInterpreter()
    {
        string candidate = Path.Combine(AppContext.BaseDirectory, "config", "nlp", "patterns.json");
        if (!File.Exists(candidate))
        {
            throw new FileNotFoundException(
                $"Intent pattern configuration was not found at '{candidate}'.",
                candidate);
        }

        Result<IntentPatternCatalog> parsed = IntentPatternCatalogParser.Parse(File.ReadAllText(candidate));
        if (!parsed.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Intent pattern configuration failed to parse: {parsed.Error!.Message}");
        }

        return new RequestInterpreter(parsed.Value);
    }
}
