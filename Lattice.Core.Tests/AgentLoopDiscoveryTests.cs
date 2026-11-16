namespace Lattice.Core.Tests;

public sealed class AgentLoopDiscoveryTests
{
    [Fact]
    public void DiscoveredCapabilityExecutesWithoutNamingIt()
    {
        DescribableTestTool tool = new("special.operation", tags: ["do-special"]);
        AgentLoop loop = CreateLoop(tool);
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), new CapabilityRequest("do-special"));
        result.Outcome.Should().Be(TurnOutcome.ToolSucceeded);
        result.Session.ToolInvocations.Should().ContainSingle();
    }

    [Fact]
    public void NewlyRegisteredCapabilityIsDiscoverableWithoutEditingCore()
    {
        // Core contains no reference to "brand.new.thing"; discovery finds it by descriptor.
        DescribableTestTool tool = new("brand.new.thing", aliases: ["frobnicate"]);
        AgentLoop loop = CreateLoop(tool);
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), new CapabilityRequest("frobnicate"));
        result.Outcome.Should().Be(TurnOutcome.ToolSucceeded);
        result.Invocation!.ToolId.Value.Should().Be("brand.new.thing");
    }

    [Fact]
    public void NoDiscoveredCapabilityBlocksWithAQuestion()
    {
        AgentLoop loop = CreateLoop(new DescribableTestTool("something"));
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), new CapabilityRequest("nonexistent"));
        result.Outcome.Should().Be(TurnOutcome.Blocked);
        result.Session.ToolInvocations.Should().BeEmpty();
    }

    [Fact]
    public void AmbiguousDiscoveryBlocksRatherThanGuessing()
    {
        DescribableTestTool first = new("first", tags: ["shared"]);
        DescribableTestTool second = new("second", tags: ["shared"]);
        AgentLoop loop = CreateLoop(first, second);
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), new CapabilityRequest("shared"));
        result.Outcome.Should().Be(TurnOutcome.Blocked);
        result.Session.ToolInvocations.Should().BeEmpty();
    }

    [Fact]
    public void ControlledStatementPathStillWorks()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse("action calculator | left=2 | right=3");
        parsed.IsSuccess.Should().BeTrue();
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), parsed.Value);
        result.Outcome.Should().Be(TurnOutcome.ToolSucceeded);
    }

    private static AgentLoop CreateLoop(params ITool[] tools)
    {
        return new(new ToolRegistry(tools), ToolPermissionPolicy.ReadOnlyOnly);
    }
}