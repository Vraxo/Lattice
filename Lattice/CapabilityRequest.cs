namespace Lattice.Core;
/// <summary>
/// A structured operation description used to discover candidate capabilities. This is not a
/// natural-language request; it names an operation (and optionally its inputs) explicitly.
/// </summary>
public sealed record CapabilityRequest
{
    public CapabilityRequest(string operation, ArgumentBag? arguments = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        Operation = operation;
        Arguments = arguments ?? ArgumentBag.Empty;
    }

    public string Operation { get; }

    public ArgumentBag Arguments { get; }
}