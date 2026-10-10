using System.Collections.Immutable;

namespace Lattice.Core;

/// <summary>
/// Answers a narrow set of workspace questions from file evidence. Every answered question
/// cites the files and lines it relied on; anything it cannot ground in evidence is reported
/// as insufficient or unsupported rather than guessed.
/// </summary>
public sealed class WorkspaceQuestionAnswerer
{
    public const int MaxEvidence = 20;
    private const int SnippetLength = 120;
    private readonly ToolRegistry _registry;
    private readonly ToolPermissionPolicy _policy;
    private readonly WorkspaceToolset _tools;

    public WorkspaceQuestionAnswerer(
        ToolRegistry registry,
        ToolPermissionPolicy policy,
        WorkspaceToolset tools)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(tools);
        _registry = registry;
        _policy = policy;
        _tools = tools;
    }

    public EvidenceAnswer Answer(RequestInterpretation interpretation)
    {
        ArgumentNullException.ThrowIfNull(interpretation);
        if (interpretation.Kind != IntentMatchKind.Matched || interpretation.Match?.Slot is null)
        {
            return EvidenceAnswer.Unsupported(
                interpretation.Text,
                "I could not interpret that as a supported workspace question.");
        }

        string target = interpretation.Match.Slot.Value;
        return interpretation.Intent switch
        {
            RequestIntentKind.FindSymbol => AnswerFindSymbol(interpretation.Text, target),
            RequestIntentKind.InspectPath => AnswerInspectPath(interpretation.Text, target),
            _ => EvidenceAnswer.Unsupported(
                interpretation.Text,
                "I can only search for a symbol or inspect a path so far."),
        };
    }

    private EvidenceAnswer AnswerFindSymbol(string question, string symbol)
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("query", ArgumentValue.FromString(symbol)),
        ]);
        ToolInvocation? invocation = Invoke(_tools.Search, arguments);
        if (invocation is null)
        {
            return EvidenceAnswer.Unsupported(question, "The search capability is not available.");
        }

        if (!invocation.Result.IsSuccess)
        {
            return EvidenceAnswer.Insufficient(
                question,
                $"The search failed: {invocation.Result.Error!.Code}.");
        }

        ImmutableArray<EvidenceReference> evidence = ParseSearchEvidence(invocation.Result.Output!.AsString());
        if (evidence.IsEmpty)
        {
            return EvidenceAnswer.Insufficient(
                question,
                $"No occurrences of '{symbol}' were found in the workspace.");
        }

        int files = evidence.Select(reference => reference.RelativePath).Distinct().Count();
        return EvidenceAnswer.Answered(
            question,
            $"Found '{symbol}' in {files} file(s), on {evidence.Length} line(s).",
            evidence);
    }

    private EvidenceAnswer AnswerInspectPath(string question, string path)
    {
        ArgumentBag readArguments = ArgumentBag.From(
        [
            new ArgumentEntry("path", ArgumentValue.FromString(path)),
        ]);
        ToolInvocation? read = Invoke(_tools.Read, readArguments);
        if (read is null)
        {
            return EvidenceAnswer.Unsupported(question, "The read capability is not available.");
        }

        if (read.Result.IsSuccess)
        {
            string text = read.Result.Output!.AsString();
            int lines = text.Length == 0 ? 1 : text.Split('\n').Length;
            EvidenceReference reference = new(path, 1, lines, Snippet(text));
            return EvidenceAnswer.Answered(
                question,
                $"Read '{path}' ({lines} line(s)).",
                [reference]);
        }

        if (read.Result.Error!.Code == FileToolErrorCodes.NotAFile)
        {
            return AnswerDirectoryListing(question, path);
        }

        return EvidenceAnswer.Insufficient(
            question,
            $"'{path}' could not be read: {read.Result.Error.Code}.");
    }

    private EvidenceAnswer AnswerDirectoryListing(string question, string path)
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("path", ArgumentValue.FromString(path)),
        ]);
        ToolInvocation? listing = Invoke(_tools.List, arguments);
        if (listing is null || !listing.Result.IsSuccess)
        {
            return EvidenceAnswer.Insufficient(question, $"'{path}' could not be listed.");
        }

        string text = listing.Result.Output!.AsString();
        int count = text.Length == 0 ? 0 : text.Split('\n').Length;
        EvidenceReference reference = new(path, 1, 1, Snippet(text));
        return EvidenceAnswer.Answered(
            question,
            $"'{path}' is a directory containing {count} entr(y/ies).",
            [reference]);
    }

    private ToolInvocation? Invoke(ToolId toolId, ArgumentBag arguments)
    {
        if (!_registry.TryResolve(toolId, out ITool? tool))
        {
            return null;
        }

        return ToolExecutor.Execute(tool, arguments, _policy);
    }

    private static ImmutableArray<EvidenceReference> ParseSearchEvidence(string output)
    {
        ImmutableArray<EvidenceReference>.Builder builder = ImmutableArray.CreateBuilder<EvidenceReference>();
        if (string.IsNullOrEmpty(output))
        {
            return builder.ToImmutable();
        }

        foreach (string line in output.Split('\n'))
        {
            if (builder.Count >= MaxEvidence)
            {
                break;
            }

            if (SearchMatch.TryParse(line, out SearchMatch? match))
            {
                builder.Add(new EvidenceReference(
                    match.RelativePath,
                    match.LineNumber,
                    match.LineNumber,
                    Snippet(match.LineText)));
            }
        }

        return builder.ToImmutable();
    }

    private static string Snippet(string text)
    {
        string single = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return single.Length <= SnippetLength ? single : single[..SnippetLength] + "...";
    }
}