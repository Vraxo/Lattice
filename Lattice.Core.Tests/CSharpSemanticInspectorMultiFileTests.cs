using Lattice.CSharp;

namespace Lattice.Core.Tests;

public sealed class CSharpSemanticInspectorMultiFileTests
{
    [Fact]
    public void TypeInOneFileResolvesFromAnotherFile()
    {
        CSharpSourceFile[] files =
        [
            new CSharpSourceFile("helper.cs", "namespace Demo; public sealed class Helper { }"),
            new CSharpSourceFile("consumer.cs", "namespace Demo; public sealed class Consumer { private Helper _h = new Helper(); }"),
        ];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(files);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid, Describe(result));
        Assert.DoesNotContain(result.Value.Diagnostics, d => d.Id == "CS0246");
    }

    [Fact]
    public void DeclarationsReportTheirSourcePath()
    {
        CSharpSourceFile[] files =
        [
            new CSharpSourceFile("alpha.cs", "class Alpha { }"),
            new CSharpSourceFile("beta.cs", "class Beta { }"),
        ];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(files);
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Declarations, d => d.Name == "Alpha" && d.Path == "alpha.cs");
        Assert.Contains(result.Value.Declarations, d => d.Name == "Beta" && d.Path == "beta.cs");
    }

    [Fact]
    public void SymbolLookupAndTypesStillWorkAcrossFiles()
    {
        CSharpSourceFile[] files =
        [
            new CSharpSourceFile("types.cs", "class Types { public int Count => 1; }"),
            new CSharpSourceFile("use.cs", "class Use { private Types _t = new Types(); }"),
        ];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(files, nameFilter: "Count");
        Assert.True(result.IsSuccess);
        SemanticSymbol symbol = Assert.Single(result.Value.Declarations);
        Assert.Equal("int", symbol.TypeName);
        Assert.Equal("types.cs", symbol.Path);
    }

    [Fact]
    public void GenuinelyUnresolvedIdentifierIsStillReportedAsSourceDiagnostic()
    {
        CSharpSourceFile[] files =
        [
            new CSharpSourceFile("a.cs", "class A { }"),
            new CSharpSourceFile("b.cs", "class B { private Missing _x = new Missing(); }"),
        ];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(files);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsValid);
        Assert.Contains(result.Value.Diagnostics, d => d.Id == "CS0246");
    }

    [Fact]
    public void EmptyFileListFails()
    {
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect([]);
        Assert.False(result.IsSuccess);
        Assert.Equal(CSharpErrorCodes.SourceRequired, result.Error!.Code);
    }

    [Fact]
    public void EmptyReferenceSetIsHostFailureNotSourceDiagnostic()
    {
        CSharpSourceFile[] files = [new CSharpSourceFile("a.cs", "class A { }")];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(files, referencePaths: []);
        Assert.False(result.IsSuccess);
        Assert.Equal(CSharpErrorCodes.ReferencesUnavailable, result.Error!.Code);
    }

    [Fact]
    public void NonExistentReferenceIsHostFailure()
    {
        CSharpSourceFile[] files = [new CSharpSourceFile("a.cs", "class A { }")];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(
            files,
            referencePaths: ["does-not-exist.dll"]);
        Assert.False(result.IsSuccess);
        Assert.Equal(CSharpErrorCodes.ReferencesUnavailable, result.Error!.Code);
    }

    [Fact]
    public void ExplicitReferencePathsAreUsedWhenSupplied()
    {
        // A compilation needs a corlib defining System.Object, so supply the running runtime's
        // core library explicitly rather than relying on the platform default.
        string coreLib = typeof(object).Assembly.Location;
        CSharpSourceFile[] files = [new CSharpSourceFile("a.cs", "class A { }")];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(files, referencePaths: [coreLib]);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsValid, Describe(result));
    }

    [Fact]
    public void MultiFileInspectionIsDeterministic()
    {
        CSharpSourceFile[] files =
        [
            new CSharpSourceFile("a.cs", "class A { }"),
            new CSharpSourceFile("b.cs", "class B { }"),
        ];
        Result<SemanticInspection> first = CSharpSemanticInspector.Inspect(files);
        Result<SemanticInspection> second = CSharpSemanticInspector.Inspect(files);
        Assert.Equal(first.Value, second.Value);
    }

    private static string Describe(Result<SemanticInspection> result)
    {
        return string.Join("; ", result.Value.Diagnostics.Select(d => d.ToString()));
    }
}