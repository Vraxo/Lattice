using Lattice.CSharp;

namespace Lattice.Core.Tests;

public sealed class CSharpSyntaxInspectorTests
{
    [Fact]
    public void ValidSourceIsReportedValidWithNoErrors()
    {
        const string source = "namespace Demo;\n\npublic sealed class Thing\n{\n    public int Value => 1;\n}\n";
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect(source);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Diagnostics.Where(d => d.Severity == SyntaxDiagnosticSeverity.Error).Should().BeEmpty();
    }

    [Fact]
    public void MissingSemicolonIsReportedAsError()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect("class C { int x = 1 }");
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Diagnostics.Where(d => d.Severity == SyntaxDiagnosticSeverity.Error).Should().NotBeEmpty();
    }

    [Fact]
    public void IncompleteCodeIsReportedAsErrorWithoutThrowing()
    {
        // Roslyn error-recovers on truncated input; the inspector must report, not crash.
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect("class Foo {");
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Diagnostics.Should().BeNotEmpty();
    }

    [Fact]
    public void DiagnosticCarriesOneBasedPosition()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect("class C\n{\n    int x = 1\n}");
        result.IsSuccess.Should().BeTrue();
        SyntaxDiagnostic error = result.Value.Diagnostics.First(d => d.Severity == SyntaxDiagnosticSeverity.Error);
        (error.Line >= 1).Should().BeTrue();
        (error.Column >= 1).Should().BeTrue();
    }

    [Fact]
    public void DiagnosticIdsAreNonEmpty()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect("class C {");
        result.IsSuccess.Should().BeTrue();
        result.Value.Diagnostics.Should().AllSatisfy(d => string.IsNullOrWhiteSpace(d.Id).Should().BeFalse());
    }

    [Fact]
    public void EmptySourceIsValid()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect(string.Empty);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
    }

    [Fact]
    public void NullSourceFails()
    {
        Result<SyntaxInspection> result = CSharpSyntaxInspector.Inspect(null!);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CSharpErrorCodes.SourceRequired);
    }

    [Fact]
    public void InspectionIsDeterministic()
    {
        const string source = "class C { int x = 1 }";
        Result<SyntaxInspection> first = CSharpSyntaxInspector.Inspect(source);
        Result<SyntaxInspection> second = CSharpSyntaxInspector.Inspect(source);
        second.Value.Should().Be(first.Value);
    }
}