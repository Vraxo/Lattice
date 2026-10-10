using Lattice.CSharp;
namespace Lattice.Core.Tests;
public sealed class CSharpSyntaxEditorTests
{
    [Fact]
    public void AddsSealedToPlainClass()
    {
        var edit = CSharpSyntaxEditor.AddSealedModifier("class C { }", "C");
        Assert.True(edit.IsApplied);
        Assert.Contains("sealed class C", edit.PatchedText, StringComparison.Ordinal);
    }
    [Fact]
    public void PreservesExistingModifiers()
    {
        var edit = CSharpSyntaxEditor.AddSealedModifier("public class C { }", "C");
        Assert.True(edit.IsApplied);
        Assert.Contains("public sealed class C", edit.PatchedText, StringComparison.Ordinal);
    }
    [Fact]
    public void OutputParsesAsValidCSharp()
    {
        var edit = CSharpSyntaxEditor.AddSealedModifier("public class C { }", "C");
        Assert.True(edit.IsApplied);
        var validation = CSharpSyntaxInspector.Inspect(edit.PatchedText!);
        Assert.True(validation.IsSuccess);
        Assert.True(validation.Value.IsValid);
    }
    [Fact]
    public void OnlyTheIntendedLineDiffers()
    {
        const string source = """
            namespace Demo;
            public class C
            {
                public int Value => 1;
            }
            """;
        var edit = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        Assert.True(edit.IsApplied);
        var differing = DifferingLines(source, edit.PatchedText!);
        Assert.Equal(1, differing);
    }
    [Fact]
    public void UntouchedRegionsArePreservedExactly()
    {
        const string source = """
            // leading comment
            namespace Demo;
            public class C
            {
                // inner comment
                public int Value => 1;
            }
            """;
        var edit = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        Assert.True(edit.IsApplied);
        Assert.Contains("// leading comment", edit.PatchedText, StringComparison.Ordinal);
        Assert.Contains("// inner comment", edit.PatchedText, StringComparison.Ordinal);
        Assert.Contains("public int Value => 1;", edit.PatchedText, StringComparison.Ordinal);
        Assert.Equal(LineCount(source), LineCount(edit.PatchedText!));
    }
    [Fact]
    public void FindsNestedClass()
    {
        const string source = "class Outer { class Inner { } }";
        var edit = CSharpSyntaxEditor.AddSealedModifier(source, "Inner");
        Assert.True(edit.IsApplied);
        Assert.Contains("sealed class Inner", edit.PatchedText, StringComparison.Ordinal);
    }
    [Fact]
    public void MissingClassIsRejected()
    {
        var edit = CSharpSyntaxEditor.AddSealedModifier("class C { }", "Missing");
        Assert.False(edit.IsApplied);
        Assert.Equal(CSharpEditStatus.TypeNotFound, edit.Status);
        Assert.Null(edit.PatchedText);
    }
    [Fact]
    public void DuplicateClassNamesAreRejected()
    {
        const string source = "namespace A { class C { } } namespace B { class C { } }";
        var edit = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        Assert.False(edit.IsApplied);
        Assert.Equal(CSharpEditStatus.AmbiguousType, edit.Status);
    }
    [Fact]
    public void AlreadySealedIsRejected()
    {
        var edit = CSharpSyntaxEditor.AddSealedModifier("public sealed class C { }", "C");
        Assert.False(edit.IsApplied);
        Assert.Equal(CSharpEditStatus.AlreadyApplied, edit.Status);
    }
    [Fact]
    public void EditThatWouldNotParseIsRejected()
    {
        // A class cannot be both abstract and sealed; the appended modifier makes the output
        // invalid, and the validation step must catch that rather than emit broken source.
        var edit = CSharpSyntaxEditor.AddSealedModifier("public abstract class C { }", "C");
        Assert.False(edit.IsApplied);
        Assert.Equal(CSharpEditStatus.OutputInvalid, edit.Status);
        Assert.Null(edit.PatchedText);
    }
    [Fact]
    public void InterfaceIsNotTreatedAsClass()
    {
        var edit = CSharpSyntaxEditor.AddSealedModifier("public interface C { }", "C");
        Assert.False(edit.IsApplied);
        Assert.Equal(CSharpEditStatus.TypeNotFound, edit.Status);
    }
    [Fact]
    public void NullSourceIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => CSharpSyntaxEditor.AddSealedModifier(null!, "C"));
    }
    [Fact]
    public void EmptyClassNameIsRejected()
    {
        Assert.Throws<ArgumentException>(() => CSharpSyntaxEditor.AddSealedModifier("class C { }", "  "));
    }
    [Fact]
    public void EditingIsDeterministic()
    {
        const string source = "public class C { }";
        var first = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        var second = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        Assert.Equal(first.PatchedText, second.PatchedText);
    }
    private static int LineCount(string text) => text.Split('\n').Length;
    private static int DifferingLines(string before, string after)
    {
        var beforeLines = before.Split('\n');
        var afterLines = after.Split('\n');
        Assert.Equal(beforeLines.Length, afterLines.Length);
        var differing = 0;
        for (var i = 0; i < beforeLines.Length; i++)
        {
            if (!string.Equals(beforeLines[i], afterLines[i], StringComparison.Ordinal))
            {
                differing++;
            }
        }
        return differing;
    }
}