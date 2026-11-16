using Lattice.CSharp;

namespace Lattice.Core.Tests;

public sealed class CSharpSemanticInspectorTests
{
    [Fact]
    public void FindsDeclaredSymbolsByKind()
    {
        const string source = """
            namespace Demo;
            public sealed class Calculator
            {
                public int Add(int a, int b) => a + b;
            }
            """;
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Declarations.Where(d => d.Name == "Calculator" && d.Kind == "NamedType").Should().NotBeEmpty();
        result.Value.Declarations.Where(d => d.Name == "Add" && d.Kind == "Method").Should().NotBeEmpty();
    }

    [Fact]
    public void NameFilterNarrowsDeclarations()
    {
        const string source = "class C { int Alpha; int Beta; }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source, nameFilter: "Alpha");
        result.IsSuccess.Should().BeTrue();
        result.Value.Declarations.Should().AllSatisfy(d => d.Name.Should().Be("Alpha"));
        result.Value.Declarations.Where(d => d.Name == "Alpha").Should().NotBeEmpty();
    }

    [Fact]
    public void ReportsTypeInformationForProperty()
    {
        const string source = "class C { public int Value => 1; }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source, nameFilter: "Value");
        result.IsSuccess.Should().BeTrue();
        SemanticSymbol symbol = result.Value.Declarations.Should().ContainSingle().Which;
        symbol.TypeName.Should().Be("int");
    }

    [Fact]
    public void ReportsTypeInformationForField()
    {
        const string source = "class C { public string Name = \"x\"; }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source, nameFilter: "Name");
        result.IsSuccess.Should().BeTrue();
        SemanticSymbol symbol = result.Value.Declarations.Should().ContainSingle().Which;
        symbol.TypeName.Should().Be("string");
    }

    [Fact]
    public void UnresolvedIdentifierIsReportedAsError()
    {
        const string source = "class C { void M() { MissingType x; } }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Diagnostics.Where(d => d.Id == "CS0246").Should().NotBeEmpty();
    }

    [Fact]
    public void PlatformTypesResolveWithoutAdditionalReferences()
    {
        const string source = "class C { void M() { System.Console.WriteLine(\"hi\"); } }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DeclarationsInSameCompilationResolve()
    {
        const string source = """
            class Helper { }
            class Consumer
            {
                private Helper _helper = new Helper();
            }
            """;
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
    }

    [Fact]
    public void DiagnosticCarriesOneBasedPosition()
    {
        const string source = "class C\n{\n    void M() { MissingType x; }\n}";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source);
        result.IsSuccess.Should().BeTrue();
        SyntaxDiagnostic error = result.Value.Diagnostics.First(d => d.Id == "CS0246");
        (error.Line >= 1).Should().BeTrue();
        (error.Column >= 1).Should().BeTrue();
    }

    [Fact]
    public void NullSourceFails()
    {
        // Cast disambiguates the null literal between the string and multi-file overloads.
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect((string)null!);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CSharpErrorCodes.SourceRequired);
    }

    [Fact]
    public void InspectionIsDeterministic()
    {
        const string source = "class C { public int Value => 1; }";
        Result<SemanticInspection> first = CSharpSemanticInspector.Inspect(source);
        Result<SemanticInspection> second = CSharpSemanticInspector.Inspect(source);
        second.Value.Should().Be(first.Value);
    }
}