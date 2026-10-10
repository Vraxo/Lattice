using Lattice.CSharp;

namespace Lattice.Core.Tests;

public sealed class CSharpSyntaxEditorTests
{
    [Fact]
    public void AddsSealedToPlainClass()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("class C { }", "C");
        Assert.True(edit.IsApplied);
        Assert.Contains("sealed class C", edit.PatchedText, StringComparison.Ordinal);
    }

    [Fact]
    public void PreservesExistingModifiers()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public class C { }", "C");
        Assert.True(edit.IsApplied);
        Assert.Contains("public sealed class C", edit.PatchedText, StringComparison.Ordinal);
    }

    [Fact]
    public void OutputParsesAsValidCSharp()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public class C { }", "C");
        Assert.True(edit.IsApplied);
        Result<SyntaxInspection> validation = CSharpSyntaxInspector.Inspect(edit.PatchedText!);
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
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        Assert.True(edit.IsApplied);
        int differing = DifferingLines(source, edit.PatchedText!);
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
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier(source, "C");
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
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier(source, "Inner");
        Assert.True(edit.IsApplied);
        Assert.Contains("sealed class Inner", edit.PatchedText, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingClassIsRejected()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("class C { }", "Missing");
        Assert.False(edit.IsApplied);
        Assert.Equal(CSharpEditStatus.TypeNotFound, edit.Status);
        Assert.Null(edit.PatchedText);
    }

    [Fact]
    public void DuplicateClassNamesAreRejected()
    {
        const string source = "namespace A { class C { } } namespace B { class C { } }";
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        Assert.False(edit.IsApplied);
        Assert.Equal(CSharpEditStatus.AmbiguousType, edit.Status);
    }

    [Fact]
    public void AlreadySealedIsRejected()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public sealed class C { }", "C");
        Assert.False(edit.IsApplied);
        Assert.Equal(CSharpEditStatus.AlreadyApplied, edit.Status);
    }

    [Fact]
    public void EditThatWouldNotParseIsRejected()
    {
        // A class cannot be both abstract and sealed; the appended modifier makes the output
        // invalid, and the validation step must catch that rather than emit broken source.
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public abstract class C { }", "C");
        Assert.False(edit.IsApplied);
        Assert.Equal(CSharpEditStatus.OutputInvalid, edit.Status);
        Assert.Null(edit.PatchedText);
    }

    [Fact]
    public void InterfaceIsNotTreatedAsClass()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public interface C { }", "C");
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
        CSharpEdit first = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        CSharpEdit second = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        Assert.Equal(first.PatchedText, second.PatchedText);
    }

    private static int LineCount(string text)
    {
        return text.Split('\n').Length;
    }

    private static int DifferingLines(string before, string after)
    {
        string[] beforeLines = before.Split('\n');
        string[] afterLines = after.Split('\n');
        Assert.Equal(beforeLines.Length, afterLines.Length);
        int differing = 0;
        for (int i = 0; i < beforeLines.Length; i++)
        {
            if (!string.Equals(beforeLines[i], afterLines[i], StringComparison.Ordinal))
            {
                differing++;
            }
        }

        return differing;
    }
}