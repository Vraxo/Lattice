using Lattice.CSharp;

namespace Lattice.Core.Tests;

public sealed class CSharpSyntaxInspectorTests
{
    [Fact]
    public void ValidSourceIsReportedValidWithNoErrors()
    {
        const string source = "namespace Demo;\n\npublic sealed class Thing\n{\n    public int Value => 1;\n}\n";
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect(source);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid);
        Assert.DoesNotContain(result.Value.Diagnostics, d => d.Severity == SyntaxDiagnosticSeverity.Error);
    }

    [Fact]
    public void MissingSemicolonIsReportedAsError()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect("class C { int x = 1 }");
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsValid);
        Assert.Contains(result.Value.Diagnostics, d => d.Severity == SyntaxDiagnosticSeverity.Error);
    }

    [Fact]
    public void IncompleteCodeIsReportedAsErrorWithoutThrowing()
    {
        // Roslyn error-recovers on truncated input; the inspector must report, not crash.
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect("class Foo {");
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsValid);
        Assert.NotEmpty(result.Value.Diagnostics);
    }

    [Fact]
    public void DiagnosticCarriesOneBasedPosition()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect("class C\n{\n    int x = 1\n}");
        Assert.True(result.IsSuccess);
        SyntaxDiagnostic error = result.Value.Diagnostics.First(d => d.Severity == SyntaxDiagnosticSeverity.Error);
        Assert.True(error.Line >= 1);
        Assert.True(error.Column >= 1);
    }

    [Fact]
    public void DiagnosticIdsAreNonEmpty()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect("class C {");
        Assert.True(result.IsSuccess);
        Assert.All(result.Value.Diagnostics, d => Assert.False(string.IsNullOrWhiteSpace(d.Id)));
    }

    [Fact]
    public void EmptySourceIsValid()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect(string.Empty);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid);
    }

    [Fact]
    public void NullSourceFails()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect(null!);
        Assert.False(result.IsSuccess);
        Assert.Equal(CSharpErrorCodes.SourceRequired, result.Error!.Code);
    }

    [Fact]
    public void InspectionIsDeterministic()
    {
        const string source = "class C { int x = 1 }";
        Result<SyntaxInspection> first = CSharpSyntaxInspector.Inspect(source);
        Result<SyntaxInspection> second = CSharpSyntaxInspector.Inspect(source);
        Assert.Equal(first.Value, second.Value);
    }
}