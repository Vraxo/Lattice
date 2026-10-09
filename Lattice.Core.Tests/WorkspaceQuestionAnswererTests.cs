using System.Text.Json;
namespace Lattice.Core.Tests;
public sealed class WorkspaceQuestionAnswererTests
{
    public static TheoryData<string, string, string, string> Entries()
    {
        var data = new TheoryData<string, string, string, string>();
        var path = Path.Combine(AppContext.BaseDirectory, "corpus", "workspace", "v1.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var entry in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            var files = entry.GetProperty("expect").GetProperty("files")
                .EnumerateArray()
                .Select(f => f.GetString()!)
                .OrderBy(f => f, StringComparer.Ordinal);
            data.Add(
                entry.GetProperty("id").GetString()!,
                entry.GetProperty("question").GetString()!,
                entry.GetProperty("expect").GetProperty("status").GetString()!,
                string.Join(",", files));
        }
        return data;
    }
    [Theory]
    [MemberData(nameof(Entries))]
    public void MatchesCorpusEntry(string id, string question, string expectedStatus, string expectedFiles)
    {
        using var workspace = BuildWorkspace();
        var answer = Answer(workspace, question);
        Assert.Equal(expectedStatus, answer.Status.ToString());
        var actualFiles = string.Join(
            ",",
            answer.Evidence
                .Select(reference => reference.RelativePath)
                .Distinct()
                .OrderBy(f => f, StringComparer.Ordinal));
        Assert.Equal(expectedFiles, actualFiles);
    }
    [Fact]
    public void AnsweredAlwaysCarriesEvidence()
    {
        using var workspace = BuildWorkspace();
        var answer = Answer(workspace, "find symbol Calculator");
        Assert.Equal(EvidenceAnswerStatus.Answered, answer.Status);
        Assert.NotEmpty(answer.Evidence);
    }
    [Fact]
    public void DirectoryQuestionFallsBackToListing()
    {
        using var workspace = BuildWorkspace();
        var answer = Answer(workspace, "read src");
        Assert.Equal(EvidenceAnswerStatus.Answered, answer.Status);
        Assert.Equal("src", answer.Evidence[0].RelativePath);
    }
    [Fact]
    public void UnsupportedIntentDoesNotClaimAnAnswer()
    {
        using var workspace = BuildWorkspace();
        var answer = Answer(workspace, "explain gravity");
        Assert.NotEqual(EvidenceAnswerStatus.Answered, answer.Status);
        Assert.Empty(answer.Evidence);
    }
    private static EvidenceAnswer Answer(TempWorkspace workspace, string question)
    {
        var root = new WorkspaceRoot(workspace.Path);
        var registry = new ToolRegistry(new ITool[]
        {
            new FileSearchTool(root),
            new ReadFileTool(root),
            new ListDirectoryTool(root),
        });
        var answerer = new WorkspaceQuestionAnswerer(
            registry,
            ToolPermissionPolicy.ReadOnlyOnly,
            WorkspaceToolset.FileTools);
        var interpretation = Interpreter().Interpret(question);
        return answerer.Answer(interpretation);
    }
    private static RequestInterpreter Interpreter()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "config", "nlp", "patterns.json");
        var parsed = IntentPatternCatalogParser.Parse(File.ReadAllText(path));
        Assert.True(parsed.IsSuccess, parsed.Error?.Message);
        return new RequestInterpreter(parsed.Value);
    }
    private static TempWorkspace BuildWorkspace()
    {
        var workspace = new TempWorkspace();
        var path = Path.Combine(AppContext.BaseDirectory, "corpus", "workspace", "v1.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var file in document.RootElement.GetProperty("files").EnumerateArray())
        {
            workspace.WriteFile(
                file.GetProperty("path").GetString()!,
                file.GetProperty("content").GetString()!);
        }
        return workspace;
    }
}