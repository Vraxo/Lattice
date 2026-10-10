using System.Text.Json;

namespace Lattice.Core.Tests;

public sealed class WorkspaceQuestionAnswererTests
{
    public static TheoryData<string, string, string> Entries()
    {
        TheoryData<string, string, string> data = [];
        string path = Path.Combine(AppContext.BaseDirectory, "corpus", "workspace", "v1.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement entry in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            IOrderedEnumerable<string> files = entry.GetProperty("expect").GetProperty("files")
                .EnumerateArray()
                .Select(f => f.GetString()!)
                .OrderBy(f => f, StringComparer.Ordinal);
            data.Add(
                entry.GetProperty("question").GetString()!,
                entry.GetProperty("expect").GetProperty("status").GetString()!,
                string.Join(",", files));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void MatchesCorpusEntry(string question, string expectedStatus, string expectedFiles)
    {
        using TempWorkspace workspace = BuildWorkspace();
        EvidenceAnswer answer = Answer(workspace, question);
        Assert.Equal(expectedStatus, answer.Status.ToString());
        string actualFiles = string.Join(
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
        using TempWorkspace workspace = BuildWorkspace();
        EvidenceAnswer answer = Answer(workspace, "find symbol Calculator");
        Assert.Equal(EvidenceAnswerStatus.Answered, answer.Status);
        Assert.NotEmpty(answer.Evidence);
    }

    [Fact]
    public void DirectoryQuestionFallsBackToListing()
    {
        using TempWorkspace workspace = BuildWorkspace();
        EvidenceAnswer answer = Answer(workspace, "read src");
        Assert.Equal(EvidenceAnswerStatus.Answered, answer.Status);
        Assert.Equal("src", answer.Evidence[0].RelativePath);
    }

    [Fact]
    public void UnsupportedIntentDoesNotClaimAnAnswer()
    {
        using TempWorkspace workspace = BuildWorkspace();
        EvidenceAnswer answer = Answer(workspace, "explain gravity");
        Assert.NotEqual(EvidenceAnswerStatus.Answered, answer.Status);
        Assert.Empty(answer.Evidence);
    }

    private static EvidenceAnswer Answer(TempWorkspace workspace, string question)
    {
        WorkspaceRoot root = new(workspace.Path);
        ToolRegistry registry = new(
        [
            new FileSearchTool(root),
            new ReadFileTool(root),
            new ListDirectoryTool(root),
        ]);
        WorkspaceQuestionAnswerer answerer = new(
            registry,
            ToolPermissionPolicy.ReadOnlyOnly,
            WorkspaceToolset.FileTools);
        RequestInterpretation interpretation = Interpreter().Interpret(question);
        return answerer.Answer(interpretation);
    }

    private static RequestInterpreter Interpreter()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "config", "nlp", "patterns.json");
        Result<IntentPatternCatalog> parsed = IntentPatternCatalogParser.Parse(File.ReadAllText(path));
        Assert.True(parsed.IsSuccess, parsed.Error?.Message);
        return new RequestInterpreter(parsed.Value);
    }

    private static TempWorkspace BuildWorkspace()
    {
        TempWorkspace workspace = new();
        string path = Path.Combine(AppContext.BaseDirectory, "corpus", "workspace", "v1.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonElement file in document.RootElement.GetProperty("files").EnumerateArray())
        {
            workspace.WriteFile(
                file.GetProperty("path").GetString()!,
                file.GetProperty("content").GetString()!);
        }

        return workspace;
    }
}