namespace Lattice.Core;

public sealed record Message
{
    public Message(MessageRole role, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Message text must not be empty.", nameof(text));
        }

        Role = role;
        Text = text;
    }

    public MessageRole Role { get; }

    public string Text { get; }
}