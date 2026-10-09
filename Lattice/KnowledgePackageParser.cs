using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lattice.Core;

public static partial class KnowledgePackageParser
{
    public const int SupportedMajorVersion = 1;
    public const int SupportedMinorVersion = 0;
    private const int MaxIdentifierLength = 128;
    private static readonly ImmutableHashSet<string> PackageFields = ImmutableHashSet.Create(
        "schemaVersion", "id", "name", "description", "concepts", "facts", "rules");

    private static readonly ImmutableHashSet<string> ConceptFields = ImmutableHashSet.Create(
        "id", "preferred", "aliases", "definition");

    private static readonly ImmutableHashSet<string> FactFields = ImmutableHashSet.Create(
        "id", "subject", "predicate", "value", "source");

    private static readonly ImmutableHashSet<string> RuleFields = ImmutableHashSet.Create(
        "id", "description", "premises", "conclusion");

    private static readonly ImmutableHashSet<string> PatternFields = ImmutableHashSet.Create(
        "subject", "predicate", "value");

    public static Result<KnowledgePackage> Parse(string json)
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
            KnowledgePackage? package = ReadPackage(root, diagnostics);
            if (diagnostics.Count > 0)
            {
                return Fail(diagnostics);
            }

            return Result<KnowledgePackage>.Success(package!);
        }
    }

    private static KnowledgePackage? ReadPackage(JsonElement root, List<string> diagnostics)
    {
        ReportUnknownFields(root, PackageFields, "document", diagnostics);
        SchemaVersion? version = ReadSchemaVersion(root, diagnostics);
        string? id = ReadRequiredString(root, "id", "document", diagnostics);
        if (id is not null && !IsValidIdentifier(id))
        {
            diagnostics.Add($"document.id: '{id}' is not a valid identifier.");
        }

        string? name = ReadRequiredString(root, "name", "document", diagnostics);
        string? description = ReadOptionalString(root, "description", "document", diagnostics);
        HashSet<string> allIds = new(StringComparer.Ordinal);
        HashSet<string> conceptIds = new(StringComparer.Ordinal);
        ImmutableArray<KnowledgeConcept> concepts = ReadConcepts(root, allIds, conceptIds, diagnostics);
        ImmutableArray<KnowledgeFact> facts = ReadFacts(root, allIds, conceptIds, diagnostics);
        ImmutableArray<KnowledgeRule> rules = ReadRules(root, allIds, conceptIds, diagnostics);
        if (version is null || id is null || name is null)
        {
            return null;
        }

        return new KnowledgePackage(version.Value, id, name, description, concepts, facts, rules);
    }

    private static SchemaVersion? ReadSchemaVersion(JsonElement root, List<string> diagnostics)
    {
        string? text = ReadRequiredString(root, "schemaVersion", "document", diagnostics);
        if (text is null)
        {
            return null;
        }

        if (!SchemaVersion.TryParse(text, out SchemaVersion version))
        {
            diagnostics.Add($"document.schemaVersion: '{text}' is not a MAJOR.MINOR version.");
            return null;
        }

        if (version.Major != SupportedMajorVersion)
        {
            diagnostics.Add(
                $"document.schemaVersion: unsupported major version {version.Major}; expected {SupportedMajorVersion}.");
            return null;
        }

        if (version.Minor < SupportedMinorVersion)
        {
            diagnostics.Add(
                $"document.schemaVersion: minor version {version.Minor} is older than supported {SupportedMinorVersion}.");
            return null;
        }

        return version;
    }

    private static ImmutableArray<KnowledgeConcept> ReadConcepts(
        JsonElement root,
        HashSet<string> allIds,
        HashSet<string> conceptIds,
        List<string> diagnostics)
    {
        ImmutableArray<KnowledgeConcept>.Builder builder = ImmutableArray.CreateBuilder<KnowledgeConcept>();
        if (!TryReadArray(root, "concepts", "document", diagnostics, out JsonElement array))
        {
            return builder.ToImmutable();
        }

        int index = 0;
        foreach (JsonElement element in array.EnumerateArray())
        {
            string path = $"concepts[{index}]";
            index++;
            if (element.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add($"{path}: must be a JSON object.");
                continue;
            }

            ReportUnknownFields(element, ConceptFields, path, diagnostics);
            string? id = ReadRequiredString(element, "id", path, diagnostics);
            if (id is not null)
            {
                if (!IsValidIdentifier(id))
                {
                    diagnostics.Add($"{path}.id: '{id}' is not a valid identifier.");
                }
                else
                {
                    if (!allIds.Add(id))
                    {
                        diagnostics.Add($"{path}.id: duplicate entity id '{id}'.");
                    }

                    conceptIds.Add(id);
                }
            }

            string? preferred = ReadRequiredString(element, "preferred", path, diagnostics);
            ImmutableArray<string> aliases = ReadStringArray(element, "aliases", path, diagnostics);
            string? definition = ReadOptionalString(element, "definition", path, diagnostics);
            if (id is not null && preferred is not null)
            {
                builder.Add(new KnowledgeConcept(id, preferred, aliases, definition));
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<KnowledgeFact> ReadFacts(
        JsonElement root,
        HashSet<string> allIds,
        HashSet<string> conceptIds,
        List<string> diagnostics)
    {
        ImmutableArray<KnowledgeFact>.Builder builder = ImmutableArray.CreateBuilder<KnowledgeFact>();
        if (!TryReadArray(root, "facts", "document", diagnostics, out JsonElement array))
        {
            return builder.ToImmutable();
        }

        int index = 0;
        foreach (JsonElement element in array.EnumerateArray())
        {
            string path = $"facts[{index}]";
            index++;
            if (element.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add($"{path}: must be a JSON object.");
                continue;
            }

            ReportUnknownFields(element, FactFields, path, diagnostics);
            string? id = ReadEntityId(element, path, allIds, diagnostics);
            string? subject = ReadRequiredString(element, "subject", path, diagnostics);
            string? predicate = ReadRequiredString(element, "predicate", path, diagnostics);
            string? value = ReadRequiredString(element, "value", path, diagnostics);
            string? source = ReadOptionalString(element, "source", path, diagnostics);
            if (subject is not null)
            {
                ReportUnknownSubject(conceptIds, subject, $"{path}.subject", diagnostics);
            }

            if (id is not null && subject is not null && predicate is not null && value is not null)
            {
                builder.Add(new KnowledgeFact(id, subject, predicate, value, source));
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<KnowledgeRule> ReadRules(
        JsonElement root,
        HashSet<string> allIds,
        HashSet<string> conceptIds,
        List<string> diagnostics)
    {
        ImmutableArray<KnowledgeRule>.Builder builder = ImmutableArray.CreateBuilder<KnowledgeRule>();
        if (!TryReadArray(root, "rules", "document", diagnostics, out JsonElement array))
        {
            return builder.ToImmutable();
        }

        int index = 0;
        foreach (JsonElement element in array.EnumerateArray())
        {
            string path = $"rules[{index}]";
            index++;
            if (element.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add($"{path}: must be a JSON object.");
                continue;
            }

            ReportUnknownFields(element, RuleFields, path, diagnostics);
            string? id = ReadEntityId(element, path, allIds, diagnostics);
            string? description = ReadRequiredString(element, "description", path, diagnostics);
            ImmutableArray<KnowledgePattern> premises = ReadPremises(element, path, conceptIds, diagnostics);
            KnowledgePattern? conclusion = ReadConclusion(element, path, conceptIds, diagnostics);
            if (id is not null && description is not null && premises.Length > 0 && conclusion is not null)
            {
                builder.Add(new KnowledgeRule(id, description, premises, conclusion));
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<KnowledgePattern> ReadPremises(
        JsonElement rule,
        string path,
        HashSet<string> conceptIds,
        List<string> diagnostics)
    {
        ImmutableArray<KnowledgePattern>.Builder builder = ImmutableArray.CreateBuilder<KnowledgePattern>();
        if (!rule.TryGetProperty("premises", out JsonElement property))
        {
            diagnostics.Add($"{path}.premises: missing required field.");
            return builder.ToImmutable();
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add($"{path}.premises: must be an array.");
            return builder.ToImmutable();
        }

        int index = 0;
        foreach (JsonElement element in property.EnumerateArray())
        {
            string patternPath = $"{path}.premises[{index}]";
            index++;
            KnowledgePattern? pattern = ReadPattern(element, patternPath, conceptIds, diagnostics);
            if (pattern is not null)
            {
                builder.Add(pattern);
            }
        }

        if (builder.Count == 0)
        {
            diagnostics.Add($"{path}.premises: must contain at least one premise.");
        }

        return builder.ToImmutable();
    }

    private static KnowledgePattern? ReadConclusion(
        JsonElement rule,
        string path,
        HashSet<string> conceptIds,
        List<string> diagnostics)
    {
        if (!rule.TryGetProperty("conclusion", out JsonElement property))
        {
            diagnostics.Add($"{path}.conclusion: missing required field.");
            return null;
        }

        return ReadPattern(property, $"{path}.conclusion", conceptIds, diagnostics);
    }

    private static KnowledgePattern? ReadPattern(
        JsonElement element,
        string path,
        HashSet<string> conceptIds,
        List<string> diagnostics)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add($"{path}: must be a JSON object.");
            return null;
        }

        ReportUnknownFields(element, PatternFields, path, diagnostics);
        string? subject = ReadRequiredString(element, "subject", path, diagnostics);
        string? predicate = ReadRequiredString(element, "predicate", path, diagnostics);
        string? value = ReadRequiredString(element, "value", path, diagnostics);
        if (subject is not null)
        {
            ReportUnknownSubject(conceptIds, subject, $"{path}.subject", diagnostics);
        }

        if (subject is null || predicate is null || value is null)
        {
            return null;
        }

        return new KnowledgePattern(subject, predicate, value);
    }

    private static string? ReadEntityId(
        JsonElement element,
        string path,
        HashSet<string> allIds,
        List<string> diagnostics)
    {
        string? id = ReadRequiredString(element, "id", path, diagnostics);
        if (id is null)
        {
            return null;
        }

        if (!IsValidIdentifier(id))
        {
            diagnostics.Add($"{path}.id: '{id}' is not a valid identifier.");
            return null;
        }

        if (!allIds.Add(id))
        {
            diagnostics.Add($"{path}.id: duplicate entity id '{id}'.");
        }

        return id;
    }

    private static void ReportUnknownSubject(
        HashSet<string> conceptIds,
        string subject,
        string path,
        List<string> diagnostics)
    {
        if (!conceptIds.Contains(subject))
        {
            diagnostics.Add($"{path}: unknown concept '{subject}'.");
        }
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

    private static string? ReadRequiredString(
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

        return ReadString(property, $"{path}.{name}", diagnostics);
    }

    private static string? ReadOptionalString(
        JsonElement element,
        string name,
        string path,
        List<string> diagnostics)
    {
        if (!element.TryGetProperty(name, out JsonElement property) || property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return ReadString(property, $"{path}.{name}", diagnostics);
    }

    private static string? ReadString(JsonElement property, string path, List<string> diagnostics)
    {
        if (property.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add($"{path}: must be a string.");
            return null;
        }

        string? value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add($"{path}: must not be empty.");
            return null;
        }

        return value;
    }

    private static ImmutableArray<string> ReadStringArray(
        JsonElement element,
        string name,
        string path,
        List<string> diagnostics)
    {
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();
        if (!element.TryGetProperty(name, out JsonElement property))
        {
            return builder.ToImmutable();
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add($"{path}.{name}: must be an array.");
            return builder.ToImmutable();
        }

        int index = 0;
        foreach (JsonElement item in property.EnumerateArray())
        {
            string itemPath = $"{path}.{name}[{index}]";
            index++;
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
            {
                diagnostics.Add($"{itemPath}: must be a non-empty string.");
                continue;
            }

            builder.Add(item.GetString()!);
        }

        return builder.ToImmutable();
    }

    private static bool TryReadArray(
        JsonElement element,
        string name,
        string path,
        List<string> diagnostics,
        out JsonElement array)
    {
        array = default;
        if (!element.TryGetProperty(name, out JsonElement property))
        {
            return false;
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add($"{path}.{name}: must be an array.");
            return false;
        }

        array = property;
        return true;
    }

    private static bool IsValidIdentifier(string value)
    {
        if (value.Length > MaxIdentifierLength)
        {
            return false;
        }

        return IdentifierPattern().IsMatch(value);
    }

    private static Result<KnowledgePackage> Fail(IEnumerable<string> diagnostics)
    {
        return Result<KnowledgePackage>.Failure(
            new Error("knowledge.invalid", string.Join(Environment.NewLine, diagnostics)));
    }

    [GeneratedRegex("^[a-z0-9]([a-z0-9._-]*[a-z0-9])?$")]
    private static partial Regex IdentifierPattern();
}