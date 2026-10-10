namespace Lattice.Core;

public sealed record RespondProposal : ActionProposal
{
    public RespondProposal(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    public string Text { get; }
}