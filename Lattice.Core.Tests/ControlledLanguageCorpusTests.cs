using System.Text.Json;

namespace Lattice.Core.Tests;

public sealed class ControlledLanguageCorpusTests
{
    public static TheoryData<string, string, string> Entries()
    {
        TheoryData<string, string, string> data = [];
        string path = Path.Combine(AppContext.BaseDirectory, "corpus", "controlled", "v1.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement entry in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            data.Add(
                entry.GetProperty("id").GetString()!,
                entry.GetProperty("input").GetString()!,
                entry.GetProperty("expect").GetRawText());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void MatchesCorpusEntry(string id, string input, string expectedJson)
    {
        Result<IControlledStatement> result = ControlledLanguageParser.Parse(input);
        using JsonDocument expected = JsonDocument.Parse(expectedJson);
        JsonElement root = expected.RootElement;
        if (root.TryGetProperty("error", out JsonElement error))
        {
            Assert.False(result.IsSuccess, $"{id}: expected an error but parsing succeeded.");
            Assert.Equal(error.GetProperty("kind").GetString(), result.Error!.Code);
            return;
        }

        Assert.True(result.IsSuccess, $"{id}: expected success but got '{result.Error?.Code}'.");
        Assert.Equal(SignatureFromJson(root.GetProperty("statement")), Signature(result.Value));
    }

    private static string Signature(IControlledStatement statement)
    {
        return statement switch
        {
            FactStatement fact => Join("fact", fact.Subject, fact.Predicate, fact.Value),
            GoalStatement goal => Join("goal", goal.Description, goal.Completion),
            ConstraintStatement constraint => Join("constraint", constraint.Description),
            ActionStatement action => ActionSignature(
                action.ToolId,
                action.Arguments.Select(a => (a.Name, a.Type.ToString().ToLowerInvariant(), a.Value))),
            _ => throw new InvalidOperationException($"Unhandled statement type '{statement.GetType().Name}'."),
        };
    }

    private static string SignatureFromJson(JsonElement statement)
    {
        string? kind = statement.GetProperty("kind").GetString();
        return kind switch
        {
            "fact" => Join("fact", Str(statement, "subject"), Str(statement, "predicate"), Str(statement, "value")),
            "goal" => Join("goal", Str(statement, "description"), Str(statement, "completion")),
            "constraint" => Join("constraint", Str(statement, "description")),
            "action" => ActionSignatureFromJson(statement),
            _ => throw new InvalidOperationException($"Unhandled statement kind '{kind}'."),
        };
    }

    private static string ActionSignatureFromJson(JsonElement statement)
    {
        string toolId = statement.GetProperty("toolId").GetString()!;
        IEnumerable<(string, string, string)> arguments = statement.GetProperty("arguments")
            .EnumerateArray()
            .Select(a => (
                a.GetProperty("name").GetString()!,
                a.GetProperty("type").GetString()!,
                a.GetProperty("value").GetString()!));
        return ActionSignature(toolId, arguments);
    }

    private static string ActionSignature(string toolId, IEnumerable<(string Name, string Type, string Value)> arguments)
    {
        List<string> parts = ["action", toolId, .. arguments.Select(a => $"{a.Name}={a.Type}:{a.Value}")];
        return string.Join("|", parts);
    }

    private static string Str(JsonElement element, string name)
    {
        return element.GetProperty(name).GetString()!;
    }

    private static string Join(params string[] parts)
    {
        return string.Join("|", parts);
    }
}