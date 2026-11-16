namespace Lattice.Core.Tests;

public sealed class VerificationDiagnosticParserTests
{
    [Fact]
    public void ParsesErrorLine()
    {
        VerificationDiagnosticParser.TryParseLine(
            "C:\\src\\App\\Program.cs(12,5): error CS0103: The name 'x' does not exist",
            out VerificationDiagnostic? diagnostic).Should().BeTrue();
        diagnostic.Severity.Should().Be(VerificationSeverity.Error);
        diagnostic.File.Should().Be("C:\\src\\App\\Program.cs");
        diagnostic.Line.Should().Be(12);
        diagnostic.Column.Should().Be(5);
        diagnostic.Code.Should().Be("CS0103");
        diagnostic.Message.Should().Be("The name 'x' does not exist");
    }

    [Fact]
    public void ParsesWarningLine()
    {
        VerificationDiagnosticParser.TryParseLine(
            "src/A.cs(1,1): warning CS1591: Missing XML comment",
            out VerificationDiagnostic? diagnostic).Should().BeTrue();
        diagnostic.Severity.Should().Be(VerificationSeverity.Warning);
        diagnostic.Code.Should().Be("CS1591");
    }

    [Fact]
    public void StripsTrailingProjectSuffix()
    {
        VerificationDiagnosticParser.TryParseLine(
            "src/A.cs(3,7): error CS0246: The type 'Foo' could not be found [C:\\src\\App.csproj]",
            out VerificationDiagnostic? diagnostic).Should().BeTrue();
        diagnostic.Message.Should().Be("The type 'Foo' could not be found");
    }

    [Fact]
    public void ParsesSdkStyleCode()
    {
        VerificationDiagnosticParser.TryParseLine(
            "Program.cs(1,1): error NETSDK1057: preview version [App.csproj]",
            out VerificationDiagnostic? diagnostic).Should().BeTrue();
        diagnostic.Code.Should().Be("NETSDK1057");
    }

    [Fact]
    public void ParsesMsBuildStyleCode()
    {
        VerificationDiagnosticParser.TryParseLine(
            "App.csproj(10,3): error MSB3021: Unable to copy file [App.csproj]",
            out VerificationDiagnostic? diagnostic).Should().BeTrue();
        diagnostic.Code.Should().Be("MSB3021");
    }

    [Fact]
    public void SeverityIsCaseInsensitive()
    {
        VerificationDiagnosticParser.TryParseLine(
            "a.cs(1,1): ERROR CS0001: boom",
            out VerificationDiagnostic? diagnostic).Should().BeTrue();
        diagnostic.Severity.Should().Be(VerificationSeverity.Error);
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
        VerificationDiagnosticParser.TryParseLine(line, out _).Should().BeFalse();
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
        diagnostics.Length.Should().Be(2);
        diagnostics[0].Severity.Should().Be(VerificationSeverity.Warning);
        diagnostics[0].Code.Should().Be("CS0168");
        diagnostics[1].Severity.Should().Be(VerificationSeverity.Error);
        diagnostics[1].Code.Should().Be("CS0103");
    }

    [Fact]
    public void EmptyOutputYieldsNoDiagnostics()
    {
        VerificationDiagnosticParser.Parse(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void OutputWithNoDiagnosticsYieldsEmpty()
    {
        VerificationDiagnosticParser.Parse("Build succeeded.\n0 Error(s)").Should().BeEmpty();
    }

    [Fact]
    public void CarriageReturnsAreTolerated()
    {
        System.Collections.Immutable.ImmutableArray<VerificationDiagnostic> diagnostics =
            VerificationDiagnosticParser.Parse("a.cs(1,1): error CS0001: boom\r\nBuild FAILED.\r\n");
        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Be("boom");
    }

    [Fact]
    public void MessageKeepsInternalBrackets()
    {
        VerificationDiagnosticParser.TryParseLine(
            "a.cs(1,1): error CS0001: unexpected [ token [App.csproj]",
            out VerificationDiagnostic? diagnostic).Should().BeTrue();
        diagnostic.Message.Should().Be("unexpected [ token");
    }

    [Fact]
    public void ParsingIsDeterministic()
    {
        const string output = "a.cs(1,1): error CS0001: boom\nb.cs(2,2): warning CS0002: hmm";
        VerificationDiagnosticParser.Parse(output)
            .SequenceEqual(VerificationDiagnosticParser.Parse(output)).Should().BeTrue();
    }
}