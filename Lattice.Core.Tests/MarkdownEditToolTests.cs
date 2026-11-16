namespace Lattice.Core.Tests;

public sealed class MarkdownEditToolTests : IDisposable
{
    private const string Document =
        "# Title\n" +
        "intro\n" +
        "## Alpha\n" +
        "alpha body\n" +
        "## Beta\n" +
        "beta body\n";

    private readonly TempWorkspace _workspace = new();

    public void Dispose()
    {
        _workspace.Dispose();
    }

    [Fact]
    public void ReplacesSectionContentAndPreservesHeading()
    {
        string path = _workspace.WriteFile("doc.md", Document);
        MarkdownEditTool tool = Create();
        string hash = HashOfSection(Document, "Alpha");
        ToolInvocation invocation = Run(tool, "doc.md", "Alpha", "new alpha\n", hash);
        invocation.Result.IsSuccess.Should().BeTrue();
        string updated = File.ReadAllText(path);
        updated.Should().Contain("## Alpha\n");
        updated.Should().Contain("new alpha\n");
        updated.Should().NotContain("alpha body");
    }

    [Fact]
    public void ContentOutsideTheSectionIsUntouched()
    {
        string path = _workspace.WriteFile("doc.md", Document);
        MarkdownEditTool tool = Create();
        string hash = HashOfSection(Document, "Alpha");
        Run(tool, "doc.md", "Alpha", "replacement\n", hash);
        string updated = File.ReadAllText(path);
        updated.Should().StartWith("# Title\nintro\n## Alpha\n");
        updated.Should().EndWith("## Beta\nbeta body\n");
    }

    [Fact]
    public void StaleSectionHashIsRejectedAndFileUnchanged()
    {
        string path = _workspace.WriteFile("doc.md", Document);
        MarkdownEditTool tool = Create();
        ToolInvocation invocation = Run(tool, "doc.md", "Alpha", "new\n", "deadbeef");
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(DocumentErrorCodes.StaleContent);
        File.ReadAllText(path).Should().Be(Document);
    }

    [Fact]
    public void ChangeElsewhereInTheFileDoesNotInvalidateTheSectionHash()
    {
        string path = _workspace.WriteFile("doc.md", Document);
        MarkdownEditTool tool = Create();
        string hash = HashOfSection(Document, "Alpha");

        // Edit a different section first, then apply the Alpha edit with its original hash.
        Run(tool, "doc.md", "Beta", "changed beta\n", HashOfSection(Document, "Beta"));
        ToolInvocation invocation = Run(tool, "doc.md", "Alpha", "new alpha\n", hash);
        invocation.Result.IsSuccess.Should().BeTrue();
        File.ReadAllText(path).Should().Contain("new alpha");
    }

    [Fact]
    public void MissingSectionIsRejected()
    {
        _workspace.WriteFile("doc.md", Document);
        MarkdownEditTool tool = Create();
        ToolInvocation invocation = Run(tool, "doc.md", "Missing", "x\n", "anyhash");
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(DocumentErrorCodes.TargetNotFound);
    }

    [Fact]
    public void AmbiguousSectionIsRejected()
    {
        _workspace.WriteFile("doc.md", "# Same\n# Same\n");
        MarkdownEditTool tool = Create();
        string hash = ContentHash.Of(string.Empty);
        ToolInvocation invocation = Run(tool, "doc.md", "Same", string.Empty, hash);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(DocumentErrorCodes.TargetAmbiguous);
    }

    [Fact]
    public void LevelDisambiguatesDuplicateHeadings()
    {
        string path = _workspace.WriteFile("doc.md", "# Same\n## Same\nbody\n");
        MarkdownEditTool tool = Create();
        string hash = ContentHash.Of("body\n");
        ToolInvocation invocation = Run(tool, "doc.md", "Same", "new\n", hash, level: 2);
        invocation.Result.IsSuccess.Should().BeTrue();
        File.ReadAllText(path).Should().Contain("# Same\n## Same\nnew\n");
    }

    [Fact]
    public void EmptyExpectedHashIsRejected()
    {
        _workspace.WriteFile("doc.md", Document);
        MarkdownEditTool tool = Create();
        ToolInvocation invocation = Run(tool, "doc.md", "Alpha", "x\n", string.Empty);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(DocumentErrorCodes.ExpectedHashMissing);
    }

    [Fact]
    public void TraversalIsRejected()
    {
        MarkdownEditTool tool = Create();
        ToolInvocation invocation = Run(tool, "../outside.md", "A", "x\n", "hash");
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(FileToolErrorCodes.PathOutsideRoot);
    }

    [Fact]
    public void MissingFileIsRejected()
    {
        MarkdownEditTool tool = Create();
        ToolInvocation invocation = Run(tool, "nope.md", "A", "x\n", "hash");
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be(FileToolErrorCodes.PathNotFound);
    }

    [Fact]
    public void DeniedPolicyPreventsAnyWrite()
    {
        string path = _workspace.WriteFile("doc.md", Document);
        MarkdownEditTool tool = Create();
        string hash = HashOfSection(Document, "Alpha");
        ToolInvocation invocation = ToolExecutor.Execute(
            tool,
            Arguments("doc.md", "Alpha", "x\n", hash, null),
            ToolPermissionPolicy.ReadOnlyOnly);
        invocation.Result.IsSuccess.Should().BeFalse();
        invocation.Result.Error!.Code.Should().Be("tool.permission.denied");
        File.ReadAllText(path).Should().Be(Document);
    }

    [Fact]
    public void SuccessfulEditCreatesBackup()
    {
        string path = _workspace.WriteFile("doc.md", Document);
        MarkdownEditTool tool = Create();
        string hash = HashOfSection(Document, "Alpha");
        Run(tool, "doc.md", "Alpha", "new\n", hash);
        string backup = path + WriteFileTool.BackupSuffix;
        File.Exists(backup).Should().BeTrue();
        File.ReadAllText(backup).Should().Be(Document);
    }

    [Fact]
    public void SuccessfulEditReturnsADiff()
    {
        _workspace.WriteFile("doc.md", Document);
        MarkdownEditTool tool = Create();
        string hash = HashOfSection(Document, "Alpha");
        ToolInvocation invocation = Run(tool, "doc.md", "Alpha", "new\n", hash);
        string output = invocation.Result.Output!.AsString();
        output.Should().Contain("-alpha body");
        output.Should().Contain("+new");
    }

    [Fact]
    public void MarkdownListOutsideTheTargetIsUnchanged()
    {
        // M8 does not parse or target list structure. This proves the weaker, required guarantee:
        // a list in an untouched region is not damaged by an edit elsewhere.
        const string source =
            "# A\n" +
            "- one\n" +
            "  - nested\n" +
            "- two\n" +
            "# B\n" +
            "old\n";
        const string listBlock = "- one\n  - nested\n- two\n";
        string path = _workspace.WriteFile("doc.md", source);
        MarkdownEditTool tool = Create();
        ToolInvocation invocation = Run(tool, "doc.md", "B", "new\n", ContentHash.Of("old\n"));
        invocation.Result.IsSuccess.Should().BeTrue();
        string updated = File.ReadAllText(path);
        updated.Should().Contain(listBlock);
        updated.Should().EndWith("# B\nnew\n");
    }

    [Fact]
    public void CarriageReturnTerminatorsSurviveAnEdit()
    {
        const string crlf = "# A\r\nold\r\n# B\r\nkeep\r\n";
        string path = _workspace.WriteFile("doc.md", crlf);
        MarkdownEditTool tool = Create();
        string hash = ContentHash.Of("old\r\n");
        Run(tool, "doc.md", "A", "new\r\n", hash);
        File.ReadAllText(path).Should().Be("# A\r\nnew\r\n# B\r\nkeep\r\n");
    }

    private MarkdownEditTool Create()
    {
        return new(new WorkspaceRoot(_workspace.Path));
    }

    private static string HashOfSection(string source, string heading)
    {
        MarkdownDocument document = MarkdownDocument.Parse(source);
        Result<MarkdownSection> section = document.FindSection(heading);
        section.IsSuccess.Should().BeTrue();
        Result<string> content = document.Text.GetText(section.Value.ContentRange);
        content.IsSuccess.Should().BeTrue();
        return ContentHash.Of(content.Value);
    }

    private static ToolInvocation Run(
        MarkdownEditTool tool,
        string path,
        string heading,
        string replacement,
        string expectedHash,
        int? level = null)
    {
        return ToolExecutor.Execute(
            tool,
            Arguments(path, heading, replacement, expectedHash, level),
            ToolPermissionPolicy.AllowAll);
    }

    private static ArgumentBag Arguments(
        string path,
        string heading,
        string replacement,
        string expectedHash,
        int? level)
    {
        System.Collections.Generic.List<ArgumentEntry> entries =
        [
            new("path", ArgumentValue.FromString(path)),
            new("heading", ArgumentValue.FromString(heading)),
            new("replacement", ArgumentValue.FromString(replacement)),
            new("expectedHash", ArgumentValue.FromString(expectedHash)),
        ];
        if (level is not null)
        {
            entries.Add(new ArgumentEntry("level", ArgumentValue.FromInteger(level.Value)));
        }

        return ArgumentBag.From(entries);
    }
}