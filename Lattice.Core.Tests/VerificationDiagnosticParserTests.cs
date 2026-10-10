namespace Lattice.Core.Tests;

public sealed class VerificationDiagnosticParserTests
{
    [Fact]
    public void ParsesErrorLine()
    {
        Assert.True(VerificationDiagnosticParser.TryParseLine(
            "C:\\src\\App\\Program.cs(12,5): error CS0103: The name 'x' does not exist",
            out VerificationDiagnostic? diagnostic));
        Assert.Equal(VerificationSeverity.Error, diagnostic.Severity);
        Assert.Equal("C:\\src\\App\\Program.cs", diagnostic.File);
        Assert.Equal(12, diagnostic.Line);
        Assert.Equal(5, diagnostic.Column);
        Assert.Equal("CS0103", diagnostic.Code);
        Assert.Equal("The name 'x' does not exist", diagnostic.Message);
    }

    [Fact]
    public void ParsesWarningLine()
    {
        Assert.True(VerificationDiagnosticParser.TryParseLine(
            "src/A.cs(1,1): warning CS1591: Missing XML comment",
            out VerificationDiagnostic? diagnostic));
        Assert.Equal(VerificationSeverity.Warning, diagnostic.Severity);
        Assert.Equal("CS1591", diagnostic.Code);
    }

    [Fact]
    public void StripsTrailingProjectSuffix()
    {
        Assert.True(VerificationDiagnosticParser.TryParseLine(
            "src/A.cs(3,7): error CS0246: The type 'Foo' could not be found [C:\\src\\App.csproj]",
            out VerificationDiagnostic? diagnostic));
        Assert.Equal("The type 'Foo' could not be found", diagnostic.Message);
    }

    [Fact]
    public void ParsesSdkStyleCode()
    {
        Assert.True(VerificationDiagnosticParser.TryParseLine(
            "Program.cs(1,1): error NETSDK1057: preview version [App.csproj]",
            out VerificationDiagnostic? diagnostic));
        Assert.Equal("NETSDK1057", diagnostic.Code);
    }

    [Fact]
    public void ParsesMsBuildStyleCode()
    {
        Assert.True(VerificationDiagnosticParser.TryParseLine(
            "App.csproj(10,3): error MSB3021: Unable to copy file [App.csproj]",
            out VerificationDiagnostic? diagnostic));
        Assert.Equal("MSB3021", diagnostic.Code);
    }

    [Fact]
    public void SeverityIsCaseInsensitive()
    {
        Assert.True(VerificationDiagnosticParser.TryParseLine(
            "a.cs(1,1): ERROR CS0001: boom",
            out VerificationDiagnostic? diagnostic));
        Assert.Equal(VerificationSeverity.Error, diagnostic.Severity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Build succeeded.")]
    [InlineData("  Determining projects to restore...")]
    [InlineData("1 Error(s)")]
    [InlineData("a.cs: error CS0001: no position")]
    [InlineData("a.cs(1): error CS0001: only one number")]
    [InlineData("a.cs(1,1): error: no code")]
    [InlineData("a.cs(1,1): something CS0001: unknown severity")]
    public void IgnoresNonDiagnosticLines(string line)
    {
        Assert.False(VerificationDiagnosticParser.TryParseLine(line, out _));
    }

    [Fact]
    public void ParseExtractsOnlyDiagnosticLines()
    {
        const string output = """
            Determining projects to restore...
            Restored C:\src\App.csproj (in 120 ms).
            C:\src\A.cs(5,9): warning CS0168: The variable 'x' is declared but never used [C:\src\App.csproj]
            C:\src\B.cs(8,3): error CS0103: The name 'y' does not exist [C:\src\App.csproj]
                Build FAILED.
            1 Warning(s)
            1 Error(s)
            """;
        System.Collections.Immutable.ImmutableArray<VerificationDiagnostic> diagnostics =
            VerificationDiagnosticParser.Parse(output);
        Assert.Equal(2, diagnostics.Length);
        Assert.Equal(VerificationSeverity.Warning, diagnostics[0].Severity);
        Assert.Equal("CS0168", diagnostics[0].Code);
        Assert.Equal(VerificationSeverity.Error, diagnostics[1].Severity);
        Assert.Equal("CS0103", diagnostics[1].Code);
    }

    [Fact]
    public void EmptyOutputYieldsNoDiagnostics()
    {
        Assert.Empty(VerificationDiagnosticParser.Parse(string.Empty));
    }

    [Fact]
    public void OutputWithNoDiagnosticsYieldsEmpty()
    {
        Assert.Empty(VerificationDiagnosticParser.Parse("Build succeeded.\n0 Error(s)"));
    }

    [Fact]
    public void CarriageReturnsAreTolerated()
    {
        System.Collections.Immutable.ImmutableArray<VerificationDiagnostic> diagnostics =
            VerificationDiagnosticParser.Parse("a.cs(1,1): error CS0001: boom\r\nBuild FAILED.\r\n");
        Assert.Single(diagnostics);
        Assert.Equal("boom", diagnostics[0].Message);
    }

    [Fact]
    public void MessageKeepsInternalBrackets()
    {
        Assert.True(VerificationDiagnosticParser.TryParseLine(
            "a.cs(1,1): error CS0001: unexpected [ token [App.csproj]",
            out VerificationDiagnostic? diagnostic));
        Assert.Equal("unexpected [ token", diagnostic.Message);
    }

    [Fact]
    public void ParsingIsDeterministic()
    {
        const string output = "a.cs(1,1): error CS0001: boom\nb.cs(2,2): warning CS0002: hmm";
        Assert.True(VerificationDiagnosticParser.Parse(output)
            .SequenceEqual(VerificationDiagnosticParser.Parse(output)));
    }
}