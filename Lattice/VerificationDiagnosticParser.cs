using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Lattice.Core;

/// <summary>
/// Extracts structured diagnostics from build or test output using the MSBuild canonical format:
/// <c>path(line,column): severity CODE: message [project]</c>. Lines that do not match are ignored,
/// so summary and progress lines pass through without producing diagnostics.
/// </summary>
public static partial class VerificationDiagnosticParser
{
    public static ImmutableArray<VerificationDiagnostic> Parse(string output)
    {
        if (string.IsNullOrEmpty(output))
        {
            return [];
        }

        ImmutableArray<VerificationDiagnostic>.Builder builder = ImmutableArray.CreateBuilder<VerificationDiagnostic>();
        foreach (string rawLine in output.Split('\n'))
        {
            if (TryParseLine(rawLine.TrimEnd('\r'), out VerificationDiagnostic? diagnostic))
            {
                builder.Add(diagnostic);
            }
        }

        return builder.ToImmutable();
    }

    public static bool TryParseLine(string line, out VerificationDiagnostic diagnostic)
    {
        diagnostic = null!;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        Match match = DiagnosticPattern().Match(line);
        if (!match.Success)
        {
            return false;
        }

        VerificationSeverity severity = match.Groups["sev"].Value.Equals("error", StringComparison.OrdinalIgnoreCase)
            ? VerificationSeverity.Error
            : VerificationSeverity.Warning;
        diagnostic = new VerificationDiagnostic(
            severity,
            match.Groups["file"].Value.Trim(),
            int.Parse(match.Groups["line"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups["col"].Value, CultureInfo.InvariantCulture),
            match.Groups["code"].Value,
            StripProjectSuffix(match.Groups["msg"].Value.Trim()));
        return true;
    }

    /// <summary>
    /// MSBuild appends the originating project in trailing brackets, for example
    /// <c>... does not exist [C:\src\App.csproj]</c>. The project is not part of the message.
    /// </summary>
    private static string StripProjectSuffix(string message)
    {
        if (message.EndsWith(']'))
        {
            int index = message.LastIndexOf(" [", StringComparison.Ordinal);
            if (index >= 0)
            {
                return message[..index].TrimEnd();
            }
        }

        return message;
    }

    [GeneratedRegex(
        @"^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\):\s*(?<sev>error|warning)\s+(?<code>[A-Za-z]+\d+):\s*(?<msg>.+)$",
        RegexOptions.IgnoreCase)]
    private static partial Regex DiagnosticPattern();
}