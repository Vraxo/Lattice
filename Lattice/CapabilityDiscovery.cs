using System.Collections.Immutable;

namespace Lattice.Core;

/// <summary>
/// Discovers candidate capabilities by matching a structured operation against generic
/// descriptor metadata: identifiers, aliases, tags, and descriptions. It never branches on a
/// specific tool name, so a newly registered capability becomes discoverable through its
/// descriptor alone.
/// </summary>
public static class CapabilityDiscovery
{
    private const int ExactNameScore = 3;
    private const int AliasOrTagScore = 2;
    private const int DescriptionScore = 1;

    public static ImmutableArray<CapabilityCandidate> Discover(
        ToolCatalog catalog,
        CapabilityRequest request)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(request);
        ImmutableArray<CapabilityCandidate>.Builder builder = ImmutableArray.CreateBuilder<CapabilityCandidate>();
        foreach (ToolDescriptor descriptor in catalog.Descriptors)
        {
            ImmutableArray<string>.Builder matchedOn = ImmutableArray.CreateBuilder<string>();
            int score = 0;
            if (Matches(descriptor.Id.Value, request.Operation))
            {
                score += ExactNameScore;
                matchedOn.Add("id");
            }

            if (AnyMatches(descriptor.Aliases, request.Operation))
            {
                score += AliasOrTagScore;
                matchedOn.Add("alias");
            }

            if (AnyMatches(descriptor.Tags, request.Operation))
            {
                score += AliasOrTagScore;
                matchedOn.Add("tag");
            }

            if (ContainsWord(descriptor.Description, request.Operation))
            {
                score += DescriptionScore;
                matchedOn.Add("description");
            }

            if (score > 0)
            {
                builder.Add(new CapabilityCandidate(
                    new InvokeToolProposal(descriptor.Id, request.Arguments),
                    score,
                    matchedOn.ToImmutable()));
            }
        }

        int best = builder.Count == 0 ? 0 : builder.Max(candidate => candidate.Score);
        return
        [
            .. builder
                .Where(candidate => candidate.Score == best)
                .OrderBy(candidate => candidate.Proposal.ToolId.Value, StringComparer.Ordinal),
        ];
    }

    private static bool AnyMatches(ImmutableArray<string> values, string operation)
    {
        foreach (string value in values)
        {
            if (Matches(value, operation))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Matches(string value, string operation)
    {
        return string.Equals(value, operation, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsWord(string description, string operation)
    {
        foreach (string word in description.Split(
            [' ', '\t', '\n', '\r', '.', ',', ';', ':', '(', ')', '/', '-'],
            StringSplitOptions.RemoveEmptyEntries))
        {
            if (Matches(word, operation))
            {
                return true;
            }
        }

        return false;
    }
}