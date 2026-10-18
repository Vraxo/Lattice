using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Lattice.Core;

/// <summary>
/// A fixed sentence with named slots. Slots are filled only from values the caller supplies, so a
/// template cannot introduce a claim that was not observed. A template whose slots are not all
/// supplied fails to realize rather than leaving a placeholder or inventing text.
/// </summary>
public sealed class ResponseTemplate
{
    private static readonly Regex SlotPattern =
        new(@"\{([A-Za-z][A-Za-z0-9]*)\}", RegexOptions.CultureInvariant);

    public ResponseTemplate(string id, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Id = id;
        Text = text;
        SlotNames =
        [
            .. SlotPattern.Matches(text)
                .Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.Ordinal),
        ];
        string residual = SlotPattern.Replace(text, string.Empty);
        if (residual.Contains('{') || residual.Contains('}'))
        {
            throw new ArgumentException(
                $"Template '{id}' contains a malformed slot; slots are written {{name}}.",
                nameof(text));
        }
    }

    public string Id { get; }

    public string Text { get; }

    public ImmutableArray<string> SlotNames { get; }

    /// <summary>
    /// Fills every slot from <paramref name="values"/>. Returns a typed failure when a slot has no
    /// value; no placeholder is ever emitted.
    /// </summary>
    /// <returns></returns>
    public Result<string> Realize(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        string? missing = null;
        string text = SlotPattern.Replace(Text, match =>
        {
            string name = match.Groups[1].Value;
            if (values.TryGetValue(name, out string? value) && value is not null)
            {
                return value;
            }

            missing ??= name;
            return match.Value;
        });
        if (missing is not null)
        {
            return Result<string>.Failure(new Error(
                ResponseErrorCodes.MissingSlot,
                $"Template '{Id}' is missing a value for slot '{missing}'."));
        }

        return Result<string>.Success(text);
    }
}