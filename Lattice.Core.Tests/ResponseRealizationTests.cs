using System.Collections.Immutable;
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
        Assert.True(realized.IsSuccess, realized.Error?.Message);
        Assert.Equal(entry.GetProperty("expect").GetString(), realized.Value);
    }
    [Theory]
    [MemberData(nameof(MissingSlotCases))]
    public void MissingSlotIsRejectedRatherThanLeftUnfilled(string id, string entryJson, string missing)
    {
        using JsonDocument document = JsonDocument.Parse(entryJson);
        JsonElement entry = document.RootElement;
        ResponseTemplate template = Template(entry.GetProperty("template").GetString()!);
        Result<string> realized = template.Realize(Slots(entry.GetProperty("slots")));
        Assert.False(realized.IsSuccess);
        Assert.Equal(ResponseErrorCodes.MissingSlot, realized.Error!.Code);
        Assert.Contains($"'{missing}'", realized.Error.Message, StringComparison.Ordinal);
    }
    [Fact]
    public void NoRealizedAnswerLeavesAnUnfilledPlaceholder()
    {
        foreach (JsonElement entry in Load().GetProperty("entries").EnumerateArray())
        {
            ResponseTemplate template = Template(entry.GetProperty("template").GetString()!);
            string text = template.Realize(Slots(entry.GetProperty("slots"))).Value;
            Assert.DoesNotContain('{', text);
            Assert.DoesNotContain('}', text);
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
        Assert.All(ResponseTemplates.All, template => Assert.Contains(template.Id, covered));
    }
    [Fact]
    public void EveryTemplateIdIsUnique()
    {
        IEnumerable<string> ids = ResponseTemplates.All.Select(template => template.Id);
        Assert.Equal(ids.Count(), ids.Distinct(StringComparer.Ordinal).Count());
    }
    [Fact]
    public void SlotNamesAreDiscoveredFromTheTemplate()
    {
        Assert.True(
            ResponseTemplates.FindSymbolAnswered.SlotNames
                .OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(["files", "lines", "symbol"]));
    }
    [Fact]
    public void ValueContainingBracesIsNotTreatedAsASlot()
    {
        Result<string> realized = ResponseTemplates.FindSymbolNotFound.Realize(
            new Dictionary<string, string>(StringComparer.Ordinal) { ["symbol"] = "a{b}" });
        Assert.True(realized.IsSuccess, realized.Error?.Message);
        Assert.Equal("No occurrences of 'a{b}' were found in the workspace.", realized.Value);
    }
    [Fact]
    public void MalformedSlotInATemplateIsRejectedAtConstruction()
    {
        Assert.Throws<ArgumentException>(() => new ResponseTemplate("bad", "missing {brace"));
    }
    [Fact]
    public void PluralUsesSingularOnlyForExactlyOne()
    {
        Assert.Equal("0 files", EnglishPlural.Of(0, "file", "files"));
        Assert.Equal("1 file", EnglishPlural.Of(1, "file", "files"));
        Assert.Equal("2 files", EnglishPlural.Of(2, "file", "files"));
    }
    [Fact]
    public void PluralRejectsNegativeCounts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EnglishPlural.Of(-1, "file", "files"));
    }
    private static JsonElement Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "corpus", "responses", "v1.json");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone();
    }
    private static ResponseTemplate Template(string id)
    {
        return Assert.Single(ResponseTemplates.All, template => template.Id == id);
    }
    private static Dictionary<string, string> Slots(JsonElement element)
    {
        Dictionary<string, string> slots = new(StringComparer.Ordinal);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            slots[property.Name] = property.Value.GetString()!;
        }
        return slots;
    }
}