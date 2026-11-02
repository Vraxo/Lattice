using System.Text.Json;

namespace Lattice.Core.Tests;

public sealed class UntrustedToolOutputTests
{
    public static TheoryData<string, string, string, string, string, string, string, string> Entries()
    {
        TheoryData<string, string, string, string, string, string, string, string> data = [];
        string path = Path.Combine(AppContext.BaseDirectory, "corpus", "adversarial", "v1.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement entry in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            data.Add(
                String(entry, "id"),
                String(entry, "source"),
                String(entry, "toolId"),
                String(entry, "path"),
                String(entry, "payload"),
                entry.GetProperty("expect").GetRawText(),
                String(entry, "requestedAction"),
                String(entry, "rationale"));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void UntrustedTextCannotExecuteControlOrBypassPermission(
        string id,
        string source,
        string toolId,
        string path,
        string payload,
        string expectedJson,
        string requestedAction,
        string rationale)
    {
        Assert.False(string.IsNullOrWhiteSpace(rationale), $"{id}: fixture rationale is required.");
        using JsonDocument expectedDocument = JsonDocument.Parse(expectedJson);
        JsonElement expected = expectedDocument.RootElement;
        using TempWorkspace workspace = new();
        ITool sourceTool;
        string sourceRequest;
        if (source == "file")
        {
            workspace.WriteFile(path, payload);
            sourceTool = new ReadFileTool(new WorkspaceRoot(workspace.Path));
            sourceRequest = $"action {toolId} | path={path}";
        }
        else if (source is "web" or "tool")
        {
            sourceTool = new UntrustedOutputTool(toolId, payload);
            sourceRequest = $"action {toolId}";
        }
        else
        {
            throw new InvalidDataException($"{id}: unknown adversarial source '{source}'.");
        }

        WriteRecordingTool writer = new();
        AgentLoop loop = new(
            new ToolRegistry([sourceTool, writer]),
            ToolPermissionPolicy.ReadOnlyOnly);
        Session original = Session.Empty(SessionId.New());

        AgentTurnResult sourceResult = loop.Run(original, Parse(sourceRequest));
        Equal(id, rationale, "sourceOutcome", Enum.Parse<TurnOutcome>(String(expected, "sourceOutcome")), sourceResult.Outcome);
        Equal(
            id,
            rationale,
            "sourceInvocationCount",
            expected.GetProperty("sourceInvocationCount").GetInt32(),
            sourceResult.Session.ToolInvocations.Length);
        ToolInvocation sourceInvocation = Assert.Single(sourceResult.Session.ToolInvocations);
        Equal(id, rationale, "sourceToolId", toolId, sourceInvocation.ToolId.Value);
        Equal(id, rationale, "payload", payload, sourceInvocation.Result.Output!.AsString());
        Equal(
            id,
            rationale,
            "payloadParsesAsControl",
            expected.GetProperty("payloadParsesAsControl").GetBoolean(),
            ControlledLanguageParser.Parse(payload).IsSuccess);
        Equal(id, rationale, "factsAfterSource", expected.GetProperty("factsAfterSource").GetInt32(), sourceResult.Session.Facts.Length);
        Equal(id, rationale, "goalsAfterSource", expected.GetProperty("goalsAfterSource").GetInt32(), sourceResult.Session.Goals.Length);
        Equal(
            id,
            rationale,
            "observationsAfterSource",
            expected.GetProperty("observationsAfterSource").GetInt32(),
            sourceResult.Session.Observations.Length);
        Equal(
            id,
            rationale,
            "writerExecutedAfterSource",
            expected.GetProperty("writerExecuted").GetBoolean(),
            writer.WasExecuted);

        AgentTurnResult attemptedWrite = loop.Run(sourceResult.Session, Parse(requestedAction));
        Equal(id, rationale, "attemptOutcome", Enum.Parse<TurnOutcome>(String(expected, "attemptOutcome")), attemptedWrite.Outcome);
        Equal(
            id,
            rationale,
            "attemptErrorCode",
            String(expected, "attemptErrorCode"),
            attemptedWrite.Invocation!.Result.Error!.Code);
        Equal(id, rationale, "attemptToolId", "writer", attemptedWrite.Invocation.ToolId.Value);
        Equal(
            id,
            rationale,
            "writerExecuted",
            expected.GetProperty("writerExecuted").GetBoolean(),
            writer.WasExecuted);
        Equal(
            id,
            rationale,
            "invocationCountAfterAttempt",
            expected.GetProperty("invocationCountAfterAttempt").GetInt32(),
            attemptedWrite.Session.ToolInvocations.Length);
        Equal(id, rationale, "factsAfterAttempt", expected.GetProperty("factsAfterAttempt").GetInt32(), attemptedWrite.Session.Facts.Length);
        Equal(id, rationale, "goalsAfterAttempt", expected.GetProperty("goalsAfterAttempt").GetInt32(), attemptedWrite.Session.Goals.Length);
        Equal(
            id,
            rationale,
            "observationsAfterAttempt",
            expected.GetProperty("observationsAfterAttempt").GetInt32(),
            attemptedWrite.Session.Observations.Length);
    }

    private static IControlledStatement Parse(string statement)
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse(statement);
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value;
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
}
