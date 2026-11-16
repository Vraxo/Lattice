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
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue("{0}", Describe(result) ?? string.Empty);
        result.Value.Diagnostics.Where(d => d.Id == "CS0246").Should().BeEmpty();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Declarations.Where(d => d.Name == "Alpha" && d.Path == "alpha.cs").Should().NotBeEmpty();
        result.Value.Declarations.Where(d => d.Name == "Beta" && d.Path == "beta.cs").Should().NotBeEmpty();
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
        result.IsSuccess.Should().BeTrue();
        SemanticSymbol symbol = result.Value.Declarations.Should().ContainSingle().Which;
        symbol.TypeName.Should().Be("int");
        symbol.Path.Should().Be("types.cs");
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
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Diagnostics.Where(d => d.Id == "CS0246").Should().NotBeEmpty();
    }

    [Fact]
    public void EmptyFileListFails()
    {
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect([]);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CSharpErrorCodes.SourceRequired);
    }

    [Fact]
    public void EmptyReferenceSetIsHostFailureNotSourceDiagnostic()
    {
        CSharpSourceFile[] files = [new CSharpSourceFile("a.cs", "class A { }")];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(files, referencePaths: []);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CSharpErrorCodes.ReferencesUnavailable);
    }

    [Fact]
    public void NonExistentReferenceIsHostFailure()
    {
        CSharpSourceFile[] files = [new CSharpSourceFile("a.cs", "class A { }")];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(
            files,
            referencePaths: ["does-not-exist.dll"]);
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(CSharpErrorCodes.ReferencesUnavailable);
    }

    [Fact]
    public void ExplicitReferencePathsAreUsedWhenSupplied()
    {
        // A compilation needs a corlib defining System.Object, so supply the running runtime's
        // core library explicitly rather than relying on the platform default.
        string coreLib = typeof(object).Assembly.Location;
        CSharpSourceFile[] files = [new CSharpSourceFile("a.cs", "class A { }")];
        Result<SemanticInspection> result = CSharpSemanticInspector.Inspect(files, referencePaths: [coreLib]);
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue("{0}", Describe(result) ?? string.Empty);
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
        second.Value.Should().Be(first.Value);
    }

    private static string Describe(Result<SemanticInspection> result)
    {
        return string.Join("; ", result.Value.Diagnostics.Select(d => d.ToString()));
    }
}