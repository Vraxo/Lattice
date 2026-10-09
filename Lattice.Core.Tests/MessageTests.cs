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
        var first = new Message(MessageRole.Assistant, "hi");
        var second = new Message(MessageRole.Assistant, "hi");
        Assert.Equal(first, second);
    }
    [Fact]
    public void MessagesWithDifferentRoleAreNotEqual()
    {
        var user = new Message(MessageRole.User, "hi");
        var assistant = new Message(MessageRole.Assistant, "hi");
        Assert.NotEqual(user, assistant);
    }
}