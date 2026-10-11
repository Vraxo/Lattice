using System.Security.Cryptography;
using System.Text;

namespace Lattice.Core;

/// <summary>
/// A stable content fingerprint used to detect stale edits. Callers record the hash of the content
/// they inspected; a tool recomputes it against the current file and rejects the edit when they
/// differ.
/// </summary>
public static class ContentHash
{
    /// <summary>Lowercase hexadecimal SHA-256 of the UTF-8 encoding of <paramref name="text"/>.</summary>
    /// <returns></returns>
    public static string Of(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}