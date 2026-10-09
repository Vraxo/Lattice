namespace Lattice.Core.Tests;

public sealed class MessageTests
{
    [Fact]
    public void RejectsEmptyText()
    {
        Assert.Throws<ArgumentException>(() => new Message(MessageRole.User, string.Empty));
    }

    [Fact]
    public void MessagesWithSameRoleAndTextAreEqual()
    {
        Message first = new(MessageRole.Assistant, "hi");
        Message second = new(MessageRole.Assistant, "hi");
        Assert.Equal(first, second);
    }

    [Fact]
    public void MessagesWithDifferentRoleAreNotEqual()
    {
        Message user = new(MessageRole.User, "hi");
        Message assistant = new(MessageRole.Assistant, "hi");
        Assert.NotEqual(user, assistant);
    }
}