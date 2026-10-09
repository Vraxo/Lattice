namespace Lattice.Core;
/// <summary>An expected operational failure represented as a value, not an exception.</summary>
public sealed record Error
{
    public Error(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Error code must not be empty.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Error message must not be empty.", nameof(message));
        }

        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }

    public override string ToString()
    {
        return $"{Code}: {Message}";
    }
}