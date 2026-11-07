using Lattice.Core;

namespace Lattice.Desktop;

internal sealed class DesktopRequestProcessor(RequestInterpreter? interpreter = null)
{
    private const string BroadFindPatternId = "find.symbol";
    private const string SupportedFormsNote =
        "Ask to view a file (for example, \"Show me README.md\") or find where an identifier appears (for example, \"Where does WorkspaceRoot appear?\").";

    private readonly RequestInterpreter _interpreter = interpreter ?? CreateDefaultInterpreter();

    public static DesktopRequestProcessor Default { get; } = new();

    public RequestInterpreter Interpreter => _interpreter;

    public DesktopTurnEntry Execute(ConnectedWorkspace? workspace, string rawRequest)
    {
        string requestText = rawRequest?.Trim() ?? string.Empty;
        if (workspace is null)
        {
            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InvalidWorkspace,
                "No workspace is connected. Connect an existing local folder before sending a workspace question.",
                WorkspacePath: null);
        }

        RequestInterpretation interpretation = _interpreter.Interpret(requestText);
        return interpretation.Kind switch
        {
            IntentMatchKind.Ambiguous => BuildAmbiguousEntry(workspace, requestText, interpretation),
            IntentMatchKind.MissingValue => BuildMissingValueEntry(workspace, requestText, interpretation),
            IntentMatchKind.Unknown => BuildUnsupportedEntry(
                workspace,
                requestText,
                $"Unsupported request. {SupportedFormsNote}"),
            IntentMatchKind.Matched => ExecuteMatched(workspace, requestText, interpretation),
            _ => BuildUnsupportedEntry(
                workspace,
                requestText,
                $"Unsupported request. {SupportedFormsNote}"),
        };
    }

    private static DesktopTurnEntry ExecuteMatched(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation)
    {
        IntentMatch match = interpretation.Match!;
        if (interpretation.Intent == RequestIntentKind.InspectPath && match.Slot is not null)
        {
            string relativePath = UnwrapMatchingQuotes(match.Slot.Value.Trim());
            return ExecuteInspect(workspace, requestText, relativePath, match.PatternId);
        }

        if (interpretation.Intent == RequestIntentKind.FindSymbol && match.Slot is not null)
        {
            if (match.PatternId == BroadFindPatternId)
            {
                return BuildUnsupportedEntry(
                    workspace,
                    requestText,
                    $"Could you clarify whether you want to search for occurrences of '{match.Slot.Value.Trim()}' or open a file path? {SupportedFormsNote}",
                    matchedPatternId: match.PatternId,
                    capturedValue: match.Slot.Value);
            }

            string symbol = UnwrapMatchingQuotes(match.Slot.Value.Trim());
            return ExecuteFindSymbol(workspace, requestText, interpretation, symbol, match.PatternId);
        }

        if (interpretation.Intent is RequestIntentKind.Question or RequestIntentKind.Explanation)
        {
            EvidenceAnswer unsupported = workspace.QuestionAnswerer.Answer(interpretation);
            return BuildUnsupportedEntry(
                workspace,
                requestText,
                $"{unsupported.Text} {SupportedFormsNote}",
                matchedPatternId: match.PatternId,
                capturedValue: match.Slot?.Value);
        }

        return BuildUnsupportedEntry(
            workspace,
            requestText,
            $"Unsupported request form. {SupportedFormsNote}",
            matchedPatternId: match.PatternId,
            capturedValue: match.Slot?.Value);
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
                "Which identifier would you like to search for in this workspace?",
                matchedPatternId: patternId);
        }

        if (symbol.Any(char.IsWhiteSpace))
        {
            return BuildUnsupportedEntry(
                workspace,
                requestText,
                $"Source-text symbol search expects a single identifier without spaces (received '{symbol}').",
                matchedPatternId: patternId,
                capturedValue: symbol);
        }

        RequestInterpretation normalizedInterpretation =
            interpretation.Match!.Slot!.Value == symbol
                ? interpretation
                : RequestInterpretation.Matched(
                    interpretation.Text,
                    new IntentMatch(patternId, RequestIntentKind.FindSymbol, new IntentSlot(interpretation.Match.Slot.Name, symbol)));

        EvidenceAnswer answer = workspace.QuestionAnswerer.Answer(normalizedInterpretation);
        return answer.Status switch
        {
            EvidenceAnswerStatus.Answered => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.SearchResults,
                answer.Text,
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
                answer.Text,
                workspace.Root.FullPath,
                SearchQuery: symbol,
                MatchedPatternId: patternId,
                CapturedValue: symbol),
            EvidenceAnswerStatus.InsufficientEvidence => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                answer.Text,
                workspace.Root.FullPath,
                SearchQuery: symbol,
                MatchedPatternId: patternId,
                CapturedValue: symbol),
            _ => BuildUnsupportedEntry(
                workspace,
                requestText,
                $"{answer.Text} {SupportedFormsNote}",
                matchedPatternId: patternId,
                capturedValue: symbol),
        };
    }

    private static DesktopTurnEntry ExecuteInspect(
        ConnectedWorkspace workspace,
        string requestText,
        string relativePath,
        string patternId)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            return BuildUnsupportedEntry(
                workspace,
                requestText,
                "Which relative file path would you like to view?",
                matchedPatternId: patternId);
        }

        if (!workspace.Registry.TryResolve(workspace.Toolset.Read, out ITool? readTool))
        {
            return new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.UnexpectedError,
                "The workspace file-read tool is not registered.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                MatchedPatternId: patternId,
                CapturedValue: relativePath);
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
                $"Inspected '{relativePath}' ({lineLabel}).",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                FileContents: contents,
                LineCount: lines,
                MatchedPatternId: patternId,
                CapturedValue: relativePath);
        }

        Error error = invocation.Result.Error!;
        return error.Code switch
        {
            FileToolErrorCodes.PathNotFound => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.MissingFile,
                $"File '{relativePath}' was not found in the connected workspace.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: relativePath),
            FileToolErrorCodes.PathOutsideRoot => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.AccessRejected,
                $"Rejected '{relativePath}': path must be relative and stay inside the connected workspace root.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: relativePath),
            FileToolErrorCodes.ReparsePointNotAllowed => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.AccessRejected,
                $"Rejected '{relativePath}': symbolic links and reparse points are blocked by the static workspace check.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: relativePath),
            FileToolErrorCodes.NotAFile => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"'{relativePath}' is a directory, not a file.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: relativePath),
            FileToolErrorCodes.FileTooLarge => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"'{relativePath}' exceeds the {ReadFileTool.MaxBytes:N0}-byte inspection limit.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: relativePath),
            FileToolErrorCodes.EncodingInvalid => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"'{relativePath}' is not valid UTF-8 text and cannot be inspected.",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: relativePath),
            _ => new DesktopTurnEntry(
                requestText,
                DesktopTurnKind.InspectError,
                $"Could not inspect '{relativePath}': {error.Message}",
                workspace.Root.FullPath,
                InspectedRelativePath: relativePath,
                ErrorCode: error.Code,
                MatchedPatternId: patternId,
                CapturedValue: relativePath),
        };
    }

    private static DesktopTurnEntry BuildAmbiguousEntry(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation)
    {
        ClarificationRequest? clarification = ClarificationGenerator.TryCreate(interpretation);
        string detail = clarification?.Question ?? "More than one interpretation matched.";
        return new DesktopTurnEntry(
            requestText,
            DesktopTurnKind.AmbiguousInput,
            $"Ambiguous request ({detail}) {SupportedFormsNote}",
            workspace.Root.FullPath);
    }

    private static DesktopTurnEntry BuildMissingValueEntry(
        ConnectedWorkspace workspace,
        string requestText,
        RequestInterpretation interpretation)
    {
        string message = interpretation.Intent switch
        {
            RequestIntentKind.InspectPath => "Which file path in the connected workspace would you like to view?",
            RequestIntentKind.FindSymbol => "Which identifier would you like to search for in this workspace?",
            _ => $"Incomplete request. {SupportedFormsNote}",
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
