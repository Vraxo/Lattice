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

        string[] parts = text.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int major))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int minor))
        {
            return false;
        }

        version = new SchemaVersion(major, minor);
        return true;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}";
    }
}