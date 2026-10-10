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
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid);
        Assert.Contains(result.Value.Declarations, d => d.Name == "Calculator" && d.Kind == "NamedType");
        Assert.Contains(result.Value.Declarations, d => d.Name == "Add" && d.Kind == "Method");
    }

    [Fact]
    public void NameFilterNarrowsDeclarations()
    {
        const string source = "class C { int Alpha; int Beta; }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source, nameFilter: "Alpha");
        Assert.True(result.IsSuccess);
        Assert.All(result.Value.Declarations, d => Assert.Equal("Alpha", d.Name));
        Assert.Contains(result.Value.Declarations, d => d.Name == "Alpha");
    }

    [Fact]
    public void ReportsTypeInformationForProperty()
    {
        const string source = "class C { public int Value => 1; }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source, nameFilter: "Value");
        Assert.True(result.IsSuccess);
        SemanticSymbol symbol = Assert.Single(result.Value.Declarations);
        Assert.Equal("int", symbol.TypeName);
    }

    [Fact]
    public void ReportsTypeInformationForField()
    {
        const string source = "class C { public string Name = \"x\"; }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source, nameFilter: "Name");
        Assert.True(result.IsSuccess);
        SemanticSymbol symbol = Assert.Single(result.Value.Declarations);
        Assert.Equal("string", symbol.TypeName);
    }

    [Fact]
    public void UnresolvedIdentifierIsReportedAsError()
    {
        const string source = "class C { void M() { MissingType x; } }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsValid);
        Assert.Contains(result.Value.Diagnostics, d => d.Id == "CS0246");
    }

    [Fact]
    public void PlatformTypesResolveWithoutAdditionalReferences()
    {
        const string source = "class C { void M() { System.Console.WriteLine(\"hi\"); } }";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid);
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
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid);
    }

    [Fact]
    public void DiagnosticCarriesOneBasedPosition()
    {
        const string source = "class C\n{\n    void M() { MissingType x; }\n}";
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(source);
        Assert.True(result.IsSuccess);
        SyntaxDiagnostic error = result.Value.Diagnostics.First(d => d.Id == "CS0246");
        Assert.True(error.Line >= 1);
        Assert.True(error.Column >= 1);
    }

    [Fact]
    public void NullSourceFails()
    {
        // Cast disambiguates the null literal between the string and multi-file overloads.
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect((string)null!);
        Assert.False(result.IsSuccess);
        Assert.Equal(CSharpErrorCodes.SourceRequired, result.Error!.Code);
    }

    [Fact]
    public void InspectionIsDeterministic()
    {
        const string source = "class C { public int Value => 1; }";
        Result<SemanticInspection> first = CSharpSemanticInspector.Inspect(source);
        Result<SemanticInspection> second = CSharpSemanticInspector.Inspect(source);
        Assert.Equal(first.Value, second.Value);
    }
}