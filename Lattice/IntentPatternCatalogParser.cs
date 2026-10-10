using System.Collections.Immutable;
using System.Text.Json;

namespace Lattice.Core;

public static class IntentPatternCatalogParser
{
    public const int SupportedMajorVersion = 1;
    private static readonly ImmutableHashSet<string> RootFields =
        ImmutableHashSet.Create("patternVersion", "patterns");

    private static readonly ImmutableHashSet<string> PatternFields =
        ImmutableHashSet.Create("id", "intent", "template");

    public static Result<IntentPatternCatalog> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Fail(["document: JSON is empty."]);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            return Fail([$"document: malformed JSON ({exception.Message})."]);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Fail(["document: root must be a JSON object."]);
            }

            List<string> diagnostics = [];
            ReportUnknownFields(root, RootFields, "document", diagnostics);
            ReadVersion(root, diagnostics);
            ImmutableArray<IntentPattern> patterns = ReadPatterns(root, diagnostics);
            if (diagnostics.Count > 0)
            {
                return Fail(diagnostics);
            }

            return Result<IntentPatternCatalog>.Success(new IntentPatternCatalog(patterns));
        }
    }

    private static void ReadVersion(JsonElement root, List<string> diagnostics)
    {
        if (!root.TryGetProperty("patternVersion", out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add("document.patternVersion: missing or not a string.");
            return;
        }

        string? text = property.GetString();
        if (!SchemaVersion.TryParse(text, out SchemaVersion version))
        {
            diagnostics.Add($"document.patternVersion: '{text}' is not a MAJOR.MINOR version.");
            return;
        }

        if (version.Major != SupportedMajorVersion)
        {
            diagnostics.Add(
                $"document.patternVersion: unsupported major version {version.Major}; expected {SupportedMajorVersion}.");
        }
    }

    private static ImmutableArray<IntentPattern> ReadPatterns(JsonElement root, List<string> diagnostics)
    {
        ImmutableArray<IntentPattern>.Builder builder = ImmutableArray.CreateBuilder<IntentPattern>();
        if (!root.TryGetProperty("patterns", out JsonElement property))
        {
            diagnostics.Add("document.patterns: missing required field.");
            return builder.ToImmutable();
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add("document.patterns: must be an array.");
            return builder.ToImmutable();
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement element in property.EnumerateArray())
        {
            string path = $"patterns[{index}]";
            index++;
            if (element.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add($"{path}: must be a JSON object.");
                continue;
            }

            ReportUnknownFields(element, PatternFields, path, diagnostics);
            string? id = ReadString(element, "id", path, diagnostics);
            if (id is not null && !seen.Add(id))
            {
                diagnostics.Add($"{path}.id: duplicate pattern id '{id}'.");
            }

            RequestIntentKind? intent = ReadIntent(element, path, diagnostics);
            string? template = ReadString(element, "template", path, diagnostics);
            ImmutableArray<string> literals = [];
            string? slotName = null;
            if (template is not null && !IntentPattern.TryParseTemplate(template, out literals, out slotName, out string? error))
            {
                diagnostics.Add($"{path}.template: {error}");
                continue;
            }

            if (id is not null && intent is not null && template is not null)
            {
                builder.Add(new IntentPattern(id, intent.Value, literals, slotName));
            }
        }

        return builder.ToImmutable();
    }

    private static RequestIntentKind? ReadIntent(JsonElement element, string path, List<string> diagnostics)
    {
        string? text = ReadString(element, "intent", path, diagnostics);
        if (text is null)
        {
            return null;
        }

        RequestIntentKind intent = text switch
        {
            "question" => RequestIntentKind.Question,
            "explanation" => RequestIntentKind.Explanation,
            "find-symbol" => RequestIntentKind.FindSymbol,
            "inspect-path" => RequestIntentKind.InspectPath,
            _ => RequestIntentKind.Unknown,
        };
        if (intent == RequestIntentKind.Unknown)
        {
            diagnostics.Add($"{path}.intent: unknown intent '{text}'.");
            return null;
        }

        return intent;
    }

    private static string? ReadString(
        JsonElement element,
        string name,
        string path,
        List<string> diagnostics)
    {
        if (!element.TryGetProperty(name, out JsonElement property))
        {
            diagnostics.Add($"{path}.{name}: missing required field.");
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add($"{path}.{name}: must be a string.");
            return null;
        }

        string? value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add($"{path}.{name}: must not be empty.");
            return null;
        }

        return value;
    }

    private static void ReportUnknownFields(
        JsonElement element,
        ImmutableHashSet<string> allowed,
        string path,
        List<string> diagnostics)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                diagnostics.Add($"{path}.{property.Name}: unknown field.");
            }
        }
    }

    private static Result<IntentPatternCatalog> Fail(IEnumerable<string> diagnostics)
    {
        return Result<IntentPatternCatalog>.Failure(
            new Error("nlp.invalid-patterns", string.Join(Environment.NewLine, diagnostics)));
    }
}