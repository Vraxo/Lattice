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
        answer.Status.ToString().Should().Be(expectedStatus);
        string actualFiles = string.Join(
            ",",
            answer.Evidence
                .Select(reference => reference.RelativePath)
                .Distinct()
                .OrderBy(f => f, StringComparer.Ordinal));
        actualFiles.Should().Be(expectedFiles);
    }

    [Fact]
    public void AnsweredTextIsFullyRealizedWithNoPlaceholders()
    {
        using TempWorkspace workspace = BuildWorkspace();
        EvidenceAnswer answer = Answer(workspace, "find symbol Calculator");
        answer.Status.Should().Be(EvidenceAnswerStatus.Answered);
        answer.Text.Should().NotContain('{'.ToString());
        answer.Text.Should().NotContain('}'.ToString());
        answer.Text.Should().Contain("'Calculator'");
    }

    [Fact]
    public void AnsweredSingularSymbolTextIsGrammatical()
    {
        using TempWorkspace workspace = BuildWorkspace();
        EvidenceAnswer answer = Answer(workspace, "find symbol Strings");
        answer.Status.Should().Be(EvidenceAnswerStatus.Answered);
        answer.Text.Should().Be("Found 'Strings' in 1 file, on 1 line.");
    }

    [Fact]
    public void AnsweredAlwaysCarriesEvidence()
    {
        using TempWorkspace workspace = BuildWorkspace();
        EvidenceAnswer answer = Answer(workspace, "find symbol Calculator");
        answer.Status.Should().Be(EvidenceAnswerStatus.Answered);
        answer.Evidence.Should().NotBeEmpty();
    }

    [Fact]
    public void DirectoryQuestionFallsBackToListing()
    {
        using TempWorkspace workspace = BuildWorkspace();
        EvidenceAnswer answer = Answer(workspace, "read src");
        answer.Status.Should().Be(EvidenceAnswerStatus.Answered);
        answer.Evidence[0].RelativePath.Should().Be("src");
    }

    [Fact]
    public void UnsupportedIntentDoesNotClaimAnAnswer()
    {
        using TempWorkspace workspace = BuildWorkspace();
        EvidenceAnswer answer = Answer(workspace, "explain gravity");
        answer.Status.Should().NotBe(EvidenceAnswerStatus.Answered);
        answer.Evidence.Should().BeEmpty();
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
        parsed.IsSuccess.Should().BeTrue("{0}", parsed.Error?.Message ?? string.Empty);
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