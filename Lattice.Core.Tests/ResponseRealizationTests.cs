using System.Text.Json;

namespace Lattice.Core.Tests;

/// <summary>
/// Verifies that realized response text contains exactly the semantic slots it was given and no
/// claim it was not. The fixture corpus is the source of truth for wording: a template change that
/// alters meaning must change the fixture deliberately.
/// </summary>
public sealed class ResponseRealizationTests
{
    public static TheoryData<string, string> RealizationCases()
    {
        TheoryData<string, string> data = [];
        foreach (JsonElement entry in Load().GetProperty("entries").EnumerateArray())
        {
            data.Add(entry.GetProperty("id").GetString()!, entry.GetRawText());
        }

        return data;
    }

    public static TheoryData<string, string, string> MissingSlotCases()
    {
        TheoryData<string, string, string> data = [];
        foreach (JsonElement entry in Load().GetProperty("missingSlotCases").EnumerateArray())
        {
            data.Add(
                entry.GetProperty("id").GetString()!,
                entry.GetRawText(),
                entry.GetProperty("missing").GetString()!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RealizationCases))]
    public void RealizesToExpectedText(string id, string entryJson)
    {
        using JsonDocument document = JsonDocument.Parse(entryJson);
        JsonElement entry = document.RootElement;
        ResponseTemplate template = Template(entry.GetProperty("template").GetString()!);
        Result<string> realized = template.Realize(Slots(entry.GetProperty("slots")));
        realized.IsSuccess.Should().BeTrue("{0}", $"{id}: {realized.Error?.Message}" ?? string.Empty);
        realized.Value.Should().Be(entry.GetProperty("expect").GetString());
    }

    [Theory]
    [MemberData(nameof(MissingSlotCases))]
    public void MissingSlotIsRejectedRatherThanLeftUnfilled(string id, string entryJson, string missing)
    {
        using JsonDocument document = JsonDocument.Parse(entryJson);
        JsonElement entry = document.RootElement;
        ResponseTemplate template = Template(entry.GetProperty("template").GetString()!);
        Result<string> realized = template.Realize(Slots(entry.GetProperty("slots")));
        realized.IsSuccess.Should().BeFalse("{0}", $"{id}: expected a rejection but realization succeeded." ?? string.Empty);
        realized.Error!.Code.Should().Be(ResponseErrorCodes.MissingSlot);
        realized.Error.Message.Should().Contain($"'{missing}'");
    }

    [Fact]
    public void NoRealizedAnswerLeavesAnUnfilledPlaceholder()
    {
        foreach (JsonElement entry in Load().GetProperty("entries").EnumerateArray())
        {
            ResponseTemplate template = Template(entry.GetProperty("template").GetString()!);
            string text = template.Realize(Slots(entry.GetProperty("slots"))).Value;
            text.Should().NotContain('{'.ToString());
            text.Should().NotContain('}'.ToString());
        }
    }

    [Fact]
    public void EveryTemplateInTheCatalogAppearsInTheCorpus()
    {
        HashSet<string> covered = [];
        foreach (JsonElement entry in Load().GetProperty("entries").EnumerateArray())
        {
            covered.Add(entry.GetProperty("template").GetString()!);
        }

        ResponseTemplates.All.Should().AllSatisfy(template => covered.Should().Contain(template.Id));
    }

    [Fact]
    public void EveryTemplateIdIsUnique()
    {
        IEnumerable<string> ids = ResponseTemplates.All.Select(template => template.Id);
        ids.Distinct(StringComparer.Ordinal).Count().Should().Be(ids.Count());
    }

    [Fact]
    public void SlotNamesAreDiscoveredFromTheTemplate()
    {
        ResponseTemplates.FindSymbolAnswered.SlotNames
                .OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(["files", "lines", "symbol"]).Should().BeTrue();
    }

    [Fact]
    public void ValueContainingBracesIsNotTreatedAsASlot()
    {
        Result<string> realized = ResponseTemplates.FindSymbolNotFound.Realize(
            new Dictionary<string, string>(StringComparer.Ordinal) { ["symbol"] = "a{b}" });
        realized.IsSuccess.Should().BeTrue("{0}", realized.Error?.Message ?? string.Empty);
        realized.Value.Should().Be("No occurrences of 'a{b}' were found in the workspace.");
    }

    [Fact]
    public void MalformedSlotInATemplateIsRejectedAtConstruction()
    {
        FluentActions.Invoking(() => new ResponseTemplate("bad", "missing {brace")).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void PluralUsesSingularOnlyForExactlyOne()
    {
        EnglishPlural.Of(0, "file", "files").Should().Be("0 files");
        EnglishPlural.Of(1, "file", "files").Should().Be("1 file");
        EnglishPlural.Of(2, "file", "files").Should().Be("2 files");
    }

    [Fact]
    public void PluralRejectsNegativeCounts()
    {
        FluentActions.Invoking(() => EnglishPlural.Of(-1, "file", "files")).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    private static JsonElement Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "corpus", "responses", "v1.json");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone();
    }

    private static ResponseTemplate Template(string id)
    {
        return ResponseTemplates.All.Should().ContainSingle(template => template.Id == id).Which;
    }

    private static Dictionary<string, string> Slots(JsonElement element)
    {
        Dictionary<string, string> slots = [with(StringComparer.Ordinal)];
        foreach (JsonProperty property in element.EnumerateObject())
        {
            slots[property.Name] = property.Value.GetString()!;
        }

        return slots;
    }
}