namespace Lattice.Core.Tests;

public sealed class AgentLoopDiscoveryTests
{
    [Fact]
    public void DiscoveredCapabilityExecutesWithoutNamingIt()
    {
        DescribableTestTool tool = new("special.operation", tags: ["do-special"]);
        AgentLoop loop = CreateLoop(tool);
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), new CapabilityRequest("do-special"));
        Assert.Equal(TurnOutcome.ToolSucceeded, result.Outcome);
        Assert.Single(result.Session.ToolInvocations);
    }

    [Fact]
    public void NewlyRegisteredCapabilityIsDiscoverableWithoutEditingCore()
    {
        // Core contains no reference to "brand.new.thing"; discovery finds it by descriptor.
        DescribableTestTool tool = new("brand.new.thing", aliases: ["frobnicate"]);
        AgentLoop loop = CreateLoop(tool);
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), new CapabilityRequest("frobnicate"));
        Assert.Equal(TurnOutcome.ToolSucceeded, result.Outcome);
        Assert.Equal("brand.new.thing", result.Invocation!.ToolId.Value);
    }

    [Fact]
    public void NoDiscoveredCapabilityBlocksWithAQuestion()
    {
        AgentLoop loop = CreateLoop(new DescribableTestTool("something"));
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), new CapabilityRequest("nonexistent"));
        Assert.Equal(TurnOutcome.Blocked, result.Outcome);
        Assert.Empty(result.Session.ToolInvocations);
    }

    [Fact]
    public void AmbiguousDiscoveryBlocksRatherThanGuessing()
    {
        DescribableTestTool first = new("first", tags: ["shared"]);
        DescribableTestTool second = new("second", tags: ["shared"]);
        AgentLoop loop = CreateLoop(first, second);
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), new CapabilityRequest("shared"));
        Assert.Equal(TurnOutcome.Blocked, result.Outcome);
        Assert.Empty(result.Session.ToolInvocations);
    }

    [Fact]
    public void ControlledStatementPathStillWorks()
    {
        AgentLoop loop = CreateLoop(new CalculatorTool());
        Result<IControlledStatement> parsed = ControlledLanguageParser.Parse("action calculator | left=2 | right=3");
        Assert.True(parsed.IsSuccess);
        AgentTurnResult result = loop.Run(Session.Empty(SessionId.New()), parsed.Value);
        Assert.Equal(TurnOutcome.ToolSucceeded, result.Outcome);
    }

    private static AgentLoop CreateLoop(params ITool[] tools)
    {
        return new(new ToolRegistry(tools), ToolPermissionPolicy.ReadOnlyOnly);
    }
}