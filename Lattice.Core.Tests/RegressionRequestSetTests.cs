using System.Text.Json;

namespace Lattice.Core.Tests;

public sealed class RegressionRequestSetTests
{
    private const string RegressionCorpusPath = "corpus/regression/v1.json";
    private const string WorkspaceCorpusPath = "corpus/workspace/v1.json";
    private const string PatternsPath = "config/nlp/patterns.json";

    public static TheoryData<string, string, string, string, string> Entries()
    {
        TheoryData<string, string, string, string, string> data = [];
        string path = Path.Combine(AppContext.BaseDirectory, RegressionCorpusPath);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        if (root.GetProperty("corpusVersion").GetString() != "1.0")
        {
            throw new InvalidDataException("Unsupported regression corpus version.");
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        HashSet<string> categories = new(StringComparer.Ordinal);
        foreach (JsonElement entry in root.GetProperty("entries").EnumerateArray())
        {
            string id = entry.GetProperty("id").GetString()!;
            string category = entry.GetProperty("category").GetString()!;
            string rationale = entry.GetProperty("rationale").GetString()!;
            if (!ids.Add(id))
            {
                throw new InvalidDataException($"Duplicate regression case id '{id}'.");
            }

            if (string.IsNullOrWhiteSpace(rationale))
            {
                throw new InvalidDataException($"Regression case '{id}' must have a rationale.");
            }

            categories.Add(category);
            data.Add(
                id,
                category,
                entry.GetProperty("input").GetRawText(),
                entry.GetProperty("expect").GetRawText(),
                rationale);
        }

        HashSet<string> requiredCategories = new(StringComparer.Ordinal)
        {
            "understanding",
            "clarification",
            "tool-selection",
            "state-update",
            "tool-error",
            "permission",
            "file-preservation",
            "output-grounding",
        };
        if (!categories.SetEquals(requiredCategories))
        {
            throw new InvalidDataException(
                $"Regression corpus categories must be exactly: {string.Join(", ", requiredCategories.OrderBy(category => category, StringComparer.Ordinal))}.");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void MatchesDocumentedRegressionCase(
        string id,
        string category,
        string inputJson,
        string expectedJson,
        string rationale)
    {
        using JsonDocument inputDocument = JsonDocument.Parse(inputJson);
        using JsonDocument expectedDocument = JsonDocument.Parse(expectedJson);
        JsonElement input = inputDocument.RootElement;
        JsonElement expected = expectedDocument.RootElement;

        switch (category)
        {
            case "understanding":
                AssertUnderstanding(id, rationale, input, expected);
                break;
            case "clarification":
                AssertClarification(id, rationale, input, expected);
                break;
            case "tool-selection":
                AssertToolSelection(id, rationale, input, expected);
                break;
            case "state-update":
                AssertStateUpdate(id, rationale, input, expected);
                break;
            case "tool-error":
                AssertToolError(id, rationale, input, expected);
                break;
            case "permission":
                AssertPermission(id, rationale, input, expected);
                break;
            case "file-preservation":
                AssertFilePreservation(id, rationale, input, expected);
                break;
            case "output-grounding":
                AssertOutputGrounding(id, rationale, input, expected);
                break;
            default:
                throw new InvalidDataException($"Unknown regression category '{category}' in '{id}'.");
        }
    }

    private static void AssertUnderstanding(
        string id,
        string rationale,
        JsonElement input,
        JsonElement expected)
    {
        RequestInterpretation interpretation = CreateInterpreter().Interpret(String(input, "request"));
        Equal(id, rationale, "kind", Enum.Parse<IntentMatchKind>(String(expected, "kind")), interpretation.Kind);
        Equal(id, rationale, "intent", Enum.Parse<RequestIntentKind>(String(expected, "intent")), interpretation.Intent);
        IntentMatch match = interpretation.Match ?? throw new InvalidDataException($"{id}: expected a matched intent.");
        Equal(id, rationale, "patternId", String(expected, "patternId"), match.PatternId);
        IntentSlot slot = match.Slot ?? throw new InvalidDataException($"{id}: expected an extracted slot.");
        Equal(id, rationale, "slotName", String(expected, "slotName"), slot.Name);
        Equal(id, rationale, "slotValue", String(expected, "slotValue"), slot.Value);
    }

    private static void AssertClarification(
        string id,
        string rationale,
        JsonElement input,
        JsonElement expected)
    {
        RequestInterpretation interpretation = CreateInterpreter().Interpret(String(input, "request"));
        Equal(id, rationale, "kind", Enum.Parse<IntentMatchKind>(String(expected, "kind")), interpretation.Kind);
        Equal(id, rationale, "intent", Enum.Parse<RequestIntentKind>(String(expected, "intent")), interpretation.Intent);
        Equal(id, rationale, "missingSlotName", String(expected, "missingSlotName"), interpretation.MissingSlotName!);

        ActionSelection selection = PolicySelector.Default.Select(
            new ActionSelectionContext(Session.Empty(SessionId.New()), interpretation));
        Equal(id, rationale, "selection", ActionSelectionKind.Selected, selection.Kind);
        AskUserProposal ask = selection.Proposal as AskUserProposal
            ?? throw new InvalidDataException($"{id}: expected an AskUserProposal.");
        Equal(
            id,
            rationale,
            "clarificationKind",
            Enum.Parse<ClarificationKind>(String(expected, "clarificationKind")),
            ask.Request.Kind);
        Equal(id, rationale, "question", String(expected, "question"), ask.Request.Question);
    }

    private static void AssertToolSelection(
        string id,
        string rationale,
        JsonElement input,
        JsonElement expected)
    {
        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("left", ArgumentValue.FromInteger(input.GetProperty("left").GetInt64())),
            new ArgumentEntry("right", ArgumentValue.FromInteger(input.GetProperty("right").GetInt64())),
        ]);
        ToolRegistry registry = new([new CalculatorTool()]);
        Session original = Session.Empty(SessionId.New());
        AgentLoop loop = new(registry, ToolPermissionPolicy.ReadOnlyOnly);
        AgentTurnResult result = loop.Run(original, new CapabilityRequest(String(input, "operation"), arguments));

        Equal(id, rationale, "outcome", Enum.Parse<TurnOutcome>(String(expected, "outcome")), result.Outcome);
        ToolInvocation invocation = result.Invocation ?? throw new InvalidDataException($"{id}: expected one tool invocation.");
        Equal(id, rationale, "toolId", String(expected, "toolId"), invocation.ToolId.Value);
        ArgumentValue output = invocation.Result.Output
            ?? throw new InvalidDataException($"{id}: successful invocation returned no output.");
        Equal(id, rationale, "outputType", String(expected, "outputType"), output.Type.ToString());
        Equal(id, rationale, "outputValue", expected.GetProperty("outputValue").GetInt64(), output.AsInteger());
        Equal(id, rationale, "invocationCount", expected.GetProperty("invocationCount").GetInt32(), result.Session.ToolInvocations.Length);
    }

    private static void AssertStateUpdate(
        string id,
        string rationale,
        JsonElement input,
        JsonElement expected)
    {
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse(String(input, "statement"));
        if (!parsed.IsSuccess)
        {
            throw new InvalidDataException($"{id}: expected a valid controlled statement, got {parsed.Error!.Code}.");
        }

        Session original = Session.Empty(SessionId.New());
        AgentLoop loop = new(new ToolRegistry([new CalculatorTool()]), ToolPermissionPolicy.ReadOnlyOnly);
        AgentTurnResult result = loop.Run(original, parsed.Value);

        Equal(id, rationale, "outcome", Enum.Parse<TurnOutcome>(String(expected, "outcome")), result.Outcome);
        Equal(id, rationale, "factCount", expected.GetProperty("factCount").GetInt32(), result.Session.Facts.Length);
        Fact fact = result.Session.Facts.Single();
        Equal(id, rationale, "factContent", String(expected, "factContent"), fact.Content);
        Equal(id, rationale, "factSource", String(expected, "factSource"), fact.Source);
        Equal(id, rationale, "factStatus", Enum.Parse<FactStatus>(String(expected, "factStatus")), fact.Status);
        Equal(
            id,
            rationale,
            "originalSessionFactCount",
            expected.GetProperty("originalSessionFactCount").GetInt32(),
            original.Facts.Length);
    }

    private static void AssertToolError(
        string id,
        string rationale,
        JsonElement input,
        JsonElement expected)
    {
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse(String(input, "statement"));
        if (!parsed.IsSuccess)
        {
            throw new InvalidDataException($"{id}: expected a valid controlled statement, got {parsed.Error!.Code}.");
        }

        AgentLoop loop = new(new ToolRegistry([new FailingTool()]), ToolPermissionPolicy.ReadOnlyOnly);
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), parsed.Value);

        Equal(id, rationale, "outcome", Enum.Parse<TurnOutcome>(String(expected, "outcome")), result.Outcome);
        ToolInvocation invocation = result.Invocation ?? throw new InvalidDataException($"{id}: expected one tool invocation.");
        Equal(id, rationale, "toolId", String(expected, "toolId"), invocation.ToolId.Value);
        Equal(id, rationale, "errorCode", String(expected, "errorCode"), invocation.Result.Error!.Code);
        Equal(id, rationale, "invocationCount", expected.GetProperty("invocationCount").GetInt32(), result.Session.ToolInvocations.Length);
    }

    private static void AssertPermission(
        string id,
        string rationale,
        JsonElement input,
        JsonElement expected)
    {
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse(String(input, "statement"));
        if (!parsed.IsSuccess)
        {
            throw new InvalidDataException($"{id}: expected a valid controlled statement, got {parsed.Error!.Code}.");
        }

        WriteRecordingTool tool = new();
        AgentLoop loop = new(new ToolRegistry([tool]), ToolPermissionPolicy.ReadOnlyOnly);
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), parsed.Value);

        Equal(id, rationale, "outcome", Enum.Parse<TurnOutcome>(String(expected, "outcome")), result.Outcome);
        ToolInvocation invocation = result.Invocation ?? throw new InvalidDataException($"{id}: expected a denied invocation.");
        Equal(id, rationale, "errorCode", String(expected, "errorCode"), invocation.Result.Error!.Code);
        Equal(id, rationale, "toolExecuted", expected.GetProperty("toolExecuted").GetBoolean(), tool.WasExecuted);
        Equal(id, rationale, "invocationCount", expected.GetProperty("invocationCount").GetInt32(), result.Session.ToolInvocations.Length);
    }

    private static void AssertFilePreservation(
        string id,
        string rationale,
        JsonElement input,
        JsonElement expected)
    {
        using TempWorkspace workspace = new();
        string relativePath = String(input, "path");
        string source = String(input, "source");
        string fullPath = workspace.WriteFile(relativePath, source);
        string heading = String(input, "heading");

        MarkdownDocument document = MarkdownDocument.Parse(source);
        Result<MarkdownSection> section = document.FindSection(heading);
        if (!section.IsSuccess)
        {
            throw new InvalidDataException($"{id}: fixture heading was not found.");
        }

        Result<string> sectionContent = document.Text.GetText(section.Value.ContentRange);
        if (!sectionContent.IsSuccess)
        {
            throw new InvalidDataException($"{id}: fixture section content could not be read.");
        }

        ArgumentBag arguments = ArgumentBag.From(
        [
            new ArgumentEntry("path", ArgumentValue.FromString(relativePath)),
            new ArgumentEntry("heading", ArgumentValue.FromString(heading)),
            new ArgumentEntry("replacement", ArgumentValue.FromString(String(input, "replacement"))),
            new ArgumentEntry("expectedHash", ArgumentValue.FromString(ContentHash.Of(sectionContent.Value))),
        ]);
        ToolInvocation invocation = ToolExecutor.Execute(
            new MarkdownEditTool(new WorkspaceRoot(workspace.Path)),
            arguments,
            ToolPermissionPolicy.AllowAll);

        if (!invocation.Result.IsSuccess)
        {
            throw new InvalidDataException($"{id}: edit failed with {invocation.Result.Error!.Code}.");
        }

        Equal(id, rationale, "updatedFile", String(expected, "updatedFile"), File.ReadAllText(fullPath));
        string backupPath = fullPath + WriteFileTool.BackupSuffix;
        bool backupEqualsOriginal = File.Exists(backupPath) && File.ReadAllText(backupPath) == source;
        Equal(
            id,
            rationale,
            "backupEqualsOriginal",
            expected.GetProperty("backupEqualsOriginal").GetBoolean(),
            backupEqualsOriginal);
    }

    private static void AssertOutputGrounding(
        string id,
        string rationale,
        JsonElement input,
        JsonElement expected)
    {
        using TempWorkspace workspace = WorkspaceFromCorpus();
        WorkspaceRoot root = new(workspace.Path);
        ToolRegistry registry = new(
        [
            new FileSearchTool(root),
            new ReadFileTool(root),
            new ListDirectoryTool(root),
        ]);
        WorkspaceQuestionAnswerer answerer = new(registry, ToolPermissionPolicy.ReadOnlyOnly, WorkspaceToolset.FileTools);
        EvidenceAnswer answer = answerer.Answer(CreateInterpreter().Interpret(String(input, "question")));

        Equal(id, rationale, "status", Enum.Parse<EvidenceAnswerStatus>(String(expected, "status")), answer.Status);
        Equal(id, rationale, "text", String(expected, "text"), answer.Text);

        string[] expectedEvidence = expected.GetProperty("evidence")
            .EnumerateArray()
            .Select(item => EvidenceSignature(
                String(item, "relativePath"),
                item.GetProperty("startLine").GetInt32(),
                item.GetProperty("endLine").GetInt32(),
                String(item, "snippet")))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        string[] actualEvidence = answer.Evidence
            .Select(reference => EvidenceSignature(
                reference.RelativePath,
                reference.StartLine,
                reference.EndLine,
                reference.Snippet))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        SequenceEqual(id, rationale, "evidence", expectedEvidence, actualEvidence);
    }

    private static RequestInterpreter CreateInterpreter()
    {
        string path = Path.Combine(AppContext.BaseDirectory, PatternsPath);
        Result<IntentPatternCatalog> parsed = IntentPatternCatalogParser.Parse(File.ReadAllText(path));
        if (!parsed.IsSuccess)
        {
            throw new InvalidDataException($"Intent pattern fixture failed to parse: {parsed.Error!.Message}");
        }

        return new RequestInterpreter(parsed.Value);
    }

    private static TempWorkspace WorkspaceFromCorpus()
    {
        TempWorkspace workspace = new();
        string path = Path.Combine(AppContext.BaseDirectory, WorkspaceCorpusPath);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement file in document.RootElement.GetProperty("files").EnumerateArray())
        {
            workspace.WriteFile(
                String(file, "path"),
                String(file, "content"));
        }

        return workspace;
    }

    private static string EvidenceSignature(string path, int startLine, int endLine, string snippet)
    {
        return $"{path}|{startLine}|{endLine}|{snippet}";
    }

    private static string String(JsonElement element, string property)
    {
        return element.GetProperty(property).GetString()!;
    }

    private static void Equal<T>(string id, string rationale, string field, T expected, T actual)
    {
        Assert.True(
            EqualityComparer<T>.Default.Equals(expected, actual),
            $"{id}: {field} differed. Expected '{expected}', actual '{actual}'. Rationale: {rationale}");
    }

    private static void SequenceEqual(
        string id,
        string rationale,
        string field,
        IEnumerable<string> expected,
        IEnumerable<string> actual)
    {
        string[] expectedArray = [.. expected];
        string[] actualArray = [.. actual];
        Assert.True(
            expectedArray.SequenceEqual(actualArray, StringComparer.Ordinal),
            $"{id}: {field} differed. Expected [{string.Join(", ", expectedArray)}], actual [{string.Join(", ", actualArray)}]. Rationale: {rationale}");
    }
}
