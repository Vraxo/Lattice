using System.Collections.Immutable;
namespace Lattice.Core;
/// <summary>
/// Answers a narrow set of workspace questions from file evidence. Every answered question
/// cites the files and lines it relied on; anything it cannot ground in evidence is reported
/// as insufficient or unsupported rather than guessed. Response text is realized from
/// <see cref="ResponseTemplates"/>; each slot is filled from an observed fact or tool result, so
/// the wording cannot introduce a claim the evidence does not support.
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
                Realize(ResponseTemplates.UnsupportedInterpretation));
        }
        string target = interpretation.Match.Slot.Value;
        return interpretation.Intent switch
        {
            RequestIntentKind.FindSymbol => AnswerFindSymbol(interpretation.Text, target),
            RequestIntentKind.InspectPath => AnswerInspectPath(interpretation.Text, target),
            _ => EvidenceAnswer.Unsupported(
                interpretation.Text,
                Realize(ResponseTemplates.UnsupportedIntent)),
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
            return EvidenceAnswer.Unsupported(
                question,
                Realize(ResponseTemplates.SearchUnavailable));
        }
        if (!invocation.Result.IsSuccess)
        {
            return EvidenceAnswer.Insufficient(
                question,
                Realize(ResponseTemplates.SearchFailed, ("errorCode", invocation.Result.Error!.Code)));
        }
        ImmutableArray<EvidenceReference> evidence = ParseSearchEvidence(invocation.Result.Output!.AsString());
        if (evidence.IsEmpty)
        {
            return EvidenceAnswer.Insufficient(
                question,
                Realize(ResponseTemplates.FindSymbolNotFound, ("symbol", symbol)));
        }
        int files = evidence.Select(reference => reference.RelativePath).Distinct().Count();
        return EvidenceAnswer.Answered(
            question,
            Realize(
                ResponseTemplates.FindSymbolAnswered,
                ("symbol", symbol),
                ("files", EnglishPlural.Of(files, "file", "files")),
                ("lines", EnglishPlural.Of(evidence.Length, "line", "lines"))),
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
            return EvidenceAnswer.Unsupported(
                question,
                Realize(ResponseTemplates.ReadUnavailable));
        }
        if (read.Result.IsSuccess)
        {
            string text = read.Result.Output!.AsString();
            int lines = text.Length == 0 ? 1 : text.Split('\n').Length;
            EvidenceReference reference = new(path, 1, lines, Snippet(text));
            return EvidenceAnswer.Answered(
                question,
                Realize(
                    ResponseTemplates.ReadAnswered,
                    ("path", path),
                    ("lines", EnglishPlural.Of(lines, "line", "lines"))),
                [reference]);
        }
        if (read.Result.Error!.Code == FileToolErrorCodes.NotAFile)
        {
            return AnswerDirectoryListing(question, path);
        }
        return EvidenceAnswer.Insufficient(
            question,
            Realize(
                ResponseTemplates.ReadFailed,
                ("path", path),
                ("errorCode", read.Result.Error.Code)));
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
            return EvidenceAnswer.Insufficient(
                question,
                Realize(ResponseTemplates.DirectoryFailed, ("path", path)));
        }
        string text = listing.Result.Output!.AsString();
        int count = text.Length == 0 ? 0 : text.Split('\n').Length;
        EvidenceReference reference = new(path, 1, 1, Snippet(text));
        return EvidenceAnswer.Answered(
            question,
            Realize(
                ResponseTemplates.DirectoryAnswered,
                ("path", path),
                ("entries", EnglishPlural.Of(count, "entry", "entries"))),
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
    /// <summary>
    /// Fills a template from observed values. A missing slot here means a call site and its
    /// template disagree, which is a programming error rather than an operational failure, so it
    /// throws instead of producing text with an unfilled placeholder.
    /// </summary>
    private static string Realize(ResponseTemplate template, params (string Name, string Value)[] slots)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach ((string name, string value) in slots)
        {
            values[name] = value;
        }
        Result<string> realized = template.Realize(values);
        if (!realized.IsSuccess)
        {
            throw new InvalidOperationException(realized.Error!.Message);
        }
        return realized.Value;
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