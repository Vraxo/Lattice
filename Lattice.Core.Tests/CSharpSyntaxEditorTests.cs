using Lattice.CSharp;

namespace Lattice.Core.Tests;

public sealed class CSharpSyntaxEditorTests
{
    [Fact]
    public void AddsSealedToPlainClass()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("class C { }", "C");
        edit.IsApplied.Should().BeTrue();
        edit.PatchedText.Should().Contain("sealed class C");
    }

    [Fact]
    public void PreservesExistingModifiers()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public class C { }", "C");
        edit.IsApplied.Should().BeTrue();
        edit.PatchedText.Should().Contain("public sealed class C");
    }

    [Fact]
    public void OutputParsesAsValidCSharp()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public class C { }", "C");
        edit.IsApplied.Should().BeTrue();
        Result<SyntaxInspection> validation = CSharpSyntaxInspector.Inspect(edit.PatchedText!);
        validation.IsSuccess.Should().BeTrue();
        validation.Value.IsValid.Should().BeTrue();
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
        edit.IsApplied.Should().BeTrue();
        int differing = DifferingLines(source, edit.PatchedText!);
        differing.Should().Be(1);
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
        edit.IsApplied.Should().BeTrue();
        edit.PatchedText.Should().Contain("// leading comment");
        edit.PatchedText.Should().Contain("// inner comment");
        edit.PatchedText.Should().Contain("public int Value => 1;");
        LineCount(edit.PatchedText!).Should().Be(LineCount(source));
    }

    [Fact]
    public void FindsNestedClass()
    {
        const string source = "class Outer { class Inner { } }";
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier(source, "Inner");
        edit.IsApplied.Should().BeTrue();
        edit.PatchedText.Should().Contain("sealed class Inner");
    }

    [Fact]
    public void MissingClassIsRejected()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("class C { }", "Missing");
        edit.IsApplied.Should().BeFalse();
        edit.Status.Should().Be(CSharpEditStatus.TypeNotFound);
        edit.PatchedText.Should().BeNull();
    }

    [Fact]
    public void DuplicateClassNamesAreRejected()
    {
        const string source = "namespace A { class C { } } namespace B { class C { } }";
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        edit.IsApplied.Should().BeFalse();
        edit.Status.Should().Be(CSharpEditStatus.AmbiguousType);
    }

    [Fact]
    public void AlreadySealedIsRejected()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public sealed class C { }", "C");
        edit.IsApplied.Should().BeFalse();
        edit.Status.Should().Be(CSharpEditStatus.AlreadyApplied);
    }

    [Fact]
    public void EditThatWouldNotParseIsRejected()
    {
        // A class cannot be both abstract and sealed; the appended modifier makes the output
        // invalid, and the validation step must catch that rather than emit broken source.
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public abstract class C { }", "C");
        edit.IsApplied.Should().BeFalse();
        edit.Status.Should().Be(CSharpEditStatus.OutputInvalid);
        edit.PatchedText.Should().BeNull();
    }

    [Fact]
    public void InterfaceIsNotTreatedAsClass()
    {
        CSharpEdit edit = CSharpSyntaxEditor.AddSealedModifier("public interface C { }", "C");
        edit.IsApplied.Should().BeFalse();
        edit.Status.Should().Be(CSharpEditStatus.TypeNotFound);
    }

    [Fact]
    public void NullSourceIsRejected()
    {
        FluentActions.Invoking(() => CSharpSyntaxEditor.AddSealedModifier(null!, "C")).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void EmptyClassNameIsRejected()
    {
        FluentActions.Invoking(() => CSharpSyntaxEditor.AddSealedModifier("class C { }", "  ")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void EditingIsDeterministic()
    {
        const string source = "public class C { }";
        CSharpEdit first = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        CSharpEdit second = CSharpSyntaxEditor.AddSealedModifier(source, "C");
        second.PatchedText.Should().Be(first.PatchedText);
    }

    private static int LineCount(string text)
    {
        return text.Split('\n').Length;
    }

    private static int DifferingLines(string before, string after)
    {
        string[] beforeLines = before.Split('\n');
        string[] afterLines = after.Split('\n');
        afterLines.Length.Should().Be(beforeLines.Length);
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