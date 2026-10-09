using System.Globalization;
namespace Lattice.Core;
public readonly record struct SchemaVersion(int Major, int Minor)
{
    public static bool TryParse(string? text, out SchemaVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }
        var parts = text.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }
        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major))
        {
            return false;
        }
        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
        {
            return false;
        }
        version = new SchemaVersion(major, minor);
        return true;
    }
    public override string ToString() => $"{Major}.{Minor}";
}