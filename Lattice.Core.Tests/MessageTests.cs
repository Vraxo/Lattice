namespace Lattice.Core.Tests;

public sealed class MessageTests
{
    [Fact]
    public void RejectsEmptyText()
    {
        FluentActions.Invoking(() => new Message(MessageRole.User, string.Empty)).Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void MessagesWithSameRoleAndTextAreEqual()
    {
        Message first = new(MessageRole.Assistant, "hi");
        Message second = new(MessageRole.Assistant, "hi");
        second.Should().Be(first);
    }

    [Fact]
    public void MessagesWithDifferentRoleAreNotEqual()
    {
        Message user = new(MessageRole.User, "hi");
        Message assistant = new(MessageRole.Assistant, "hi");
        assistant.Should().NotBe(user);
    }
}