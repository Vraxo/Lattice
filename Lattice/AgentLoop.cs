using System.Collections.Immutable;

namespace Lattice.Core;

/// <summary>
/// Runs a single bounded turn. It interprets the incoming statement into structured candidates,
/// asks <see cref="PolicySelector"/> to choose one, executes the chosen proposal, and records the
/// result. The loop does not decide by switching on the incoming statement type; the only switch
/// is over the already-selected proposal, which the execution layer necessarily handles.
/// </summary>
public sealed class AgentLoop
{
    private readonly ToolRegistry _registry;
    private readonly ToolPermissionPolicy _policy;
    private readonly PolicySelector _selector;
    private readonly RequestInterpretation _interpretation;

    public AgentLoop(
        ToolRegistry registry,
        ToolPermissionPolicy policy,
        PolicySelector? selector = null,
        RequestInterpretation? interpretation = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(policy);
        _registry = registry;
        _policy = policy;
        _selector = selector ?? PolicySelector.Default;
        _interpretation = interpretation ?? RequestInterpretation.Unknown(string.Empty, "No request interpretation was supplied.");
    }

    public AgentTurnResult Run(
        Session session,
        IControlledStatement statement,
        CancellationToken cancellationToken = default)
    {
        return RunStatement(session, statement, runContext: null, cancellationToken);
    }

    /// <summary>Runs one turn within a shared, bounded logical run context.</summary>
    /// <returns></returns>
    public AgentTurnResult Run(
        Session session,
        AgentRunContext runContext,
        IControlledStatement statement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runContext);
        return RunStatement(session, statement, runContext, cancellationToken);
    }

    /// <summary>
    /// Runs a turn from a structured capability request. Candidates are discovered from generic
    /// descriptor metadata and then chosen by the same policy selector the controlled path uses.
    /// When nothing is discovered, the turn is blocked with a question rather than guessed.
    /// </summary>
    /// <returns></returns>
    public AgentTurnResult Run(
        Session session,
        CapabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        return RunCapabilityRequest(session, request, runContext: null, cancellationToken);
    }

    /// <summary>Runs a capability request within a shared, bounded logical run context.</summary>
    /// <returns></returns>
    public AgentTurnResult Run(
        Session session,
        AgentRunContext runContext,
        CapabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runContext);
        return RunCapabilityRequest(session, request, runContext, cancellationToken);
    }

    private AgentTurnResult RunStatement(
        Session session,
        IControlledStatement statement,
        AgentRunContext? runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(statement);
        if (!TryConsumeStep(session, runContext, out AgentTurnResult? exhausted))
        {
            return exhausted!;
        }

        InterpretedStatement interpreted = ControlledStatementInterpreter.Interpret(session, statement);
        if (interpreted.IsResolved)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Cancelled(session);
            }

            return new AgentTurnResult(
                interpreted.Session,
                interpreted.ResolvedOutcome!.Value,
                interpreted.ResolvedResponse!);
        }

        ActionSelectionContext context = new(interpreted.Session, _interpretation);
        ActionSelection selection = _selector.Select(context, interpreted.Candidates);
        return Execute(interpreted.Session, selection, cancellationToken);
    }

    private AgentTurnResult RunCapabilityRequest(
        Session session,
        CapabilityRequest request,
        AgentRunContext? runContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(request);
        if (!TryConsumeStep(session, runContext, out AgentTurnResult? exhausted))
        {
            return exhausted!;
        }

        ImmutableArray<CapabilityCandidate> candidates = CapabilityDiscovery.Discover(_registry.ToCatalog(), request);
        if (candidates.IsEmpty)
        {
            AskUserProposal ask = new(new ClarificationRequest(
                ClarificationKind.NoCapabilityFound,
                $"No capability is registered for '{request.Operation}'."));
            return Execute(session, ActionSelection.Blocked(ask, "Discovery found no candidate."), cancellationToken);
        }

        ActionProposal[] proposals = [.. candidates.Select(candidate => (ActionProposal)candidate.Proposal)];
        ActionSelectionContext context = new(session, _interpretation);
        ActionSelection selection = _selector.Select(context, proposals);
        return Execute(session, selection, cancellationToken);
    }

    private static bool TryConsumeStep(
        Session session,
        AgentRunContext? runContext,
        out AgentTurnResult? exhausted)
    {
        exhausted = null;
        if (runContext is null || runContext.TryConsumeStep())
        {
            return true;
        }

        exhausted = new AgentTurnResult(
            session,
            TurnOutcome.StepLimitExceeded,
            $"The run exhausted its {runContext.MaxSteps}-step limit.");
        return false;
    }

    private static AgentTurnResult Cancelled(Session session)
    {
        return new AgentTurnResult(session, TurnOutcome.Cancelled, "The turn was cancelled before completion.");
    }

    private AgentTurnResult Execute(Session session, ActionSelection selection, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested && selection.Proposal is not InvokeToolProposal)
        {
            return Cancelled(session);
        }

        // A blocked selection carries a proposal the user should see (typically a question).
        if (!selection.IsSelected)
        {
            return new AgentTurnResult(
                session,
                TurnOutcome.Blocked,
                DescribeProposal(selection.Proposal));
        }

        return selection.Proposal switch
        {
            InvokeToolProposal invoke => ExecuteTool(session, invoke, cancellationToken),
            AskUserProposal ask => new AgentTurnResult(session, TurnOutcome.AskedUser, ask.Request.Question),
            RespondProposal respond => new AgentTurnResult(session, TurnOutcome.Responded, respond.Text),
            ContinueProposal or FinishProposal => new AgentTurnResult(
                session,
                TurnOutcome.Blocked,
                DescribeProposal(selection.Proposal)),
            _ => throw new InvalidOperationException(
                $"Unhandled proposal type '{selection.Proposal.GetType().Name}'."),
        };
    }

    private AgentTurnResult ExecuteTool(
        Session session,
        InvokeToolProposal proposal,
        CancellationToken cancellationToken)
    {
        if (!_registry.TryResolve(proposal.ToolId, out ITool? tool))
        {
            return new AgentTurnResult(
                session,
                TurnOutcome.ToolUnavailable,
                $"Tool '{proposal.ToolId.Value}' is not available.");
        }

        if (IsDuplicate(session, proposal.ToolId, proposal.Arguments))
        {
            return new AgentTurnResult(
                session,
                TurnOutcome.DuplicateAction,
                $"The action for '{proposal.ToolId.Value}' with the same arguments was already attempted.");
        }

        ToolInvocation invocation = ToolExecutor.Execute(tool, proposal.Arguments, _policy, cancellationToken);
        Session updated = session.AddToolInvocation(invocation);
        return new AgentTurnResult(
            updated,
            Classify(invocation),
            DescribeInvocation(proposal.ToolId.Value, invocation),
            invocation);
    }

    private static TurnOutcome Classify(ToolInvocation invocation)
    {
        if (invocation.Result.IsSuccess)
        {
            return TurnOutcome.ToolSucceeded;
        }

        return invocation.Result.Error!.Code switch
        {
            "tool.permission.denied" => TurnOutcome.PermissionDenied,
            "tool.argument.unknown" or "tool.argument.type" or "tool.argument.missing" => TurnOutcome.InvalidArguments,
            ToolErrorCodes.Cancelled => TurnOutcome.Cancelled,
            ToolErrorCodes.TimedOut => TurnOutcome.ToolTimedOut,
            _ => TurnOutcome.ToolFailed,
        };
    }

    private static string DescribeInvocation(string toolId, ToolInvocation invocation)
    {
        if (invocation.Result.IsSuccess)
        {
            return $"Tool '{toolId}' succeeded with result {invocation.Result.Output!.Type}.";
        }

        return invocation.Result.Error!.Code switch
        {
            "tool.permission.denied" => $"Tool '{toolId}' was denied by policy.",
            "tool.argument.unknown" or "tool.argument.type" or "tool.argument.missing" =>
                $"Tool '{toolId}' rejected its arguments.",
            ToolErrorCodes.Cancelled => $"Tool '{toolId}' was cancelled.",
            ToolErrorCodes.TimedOut => $"Tool '{toolId}' exceeded its execution timeout.",
            _ => $"Tool '{toolId}' failed: {invocation.Result.Error.Message}",
        };
    }

    private static string DescribeProposal(ActionProposal proposal)
    {
        return proposal switch
        {
            AskUserProposal ask => ask.Request.Question,
            ContinueProposal cont => $"Continuing: {cont.Reason}",
            FinishProposal finish => $"Finishing: {finish.Reason}",
            RespondProposal respond => respond.Text,
            InvokeToolProposal invoke => $"Invoking '{invoke.ToolId.Value}'.",
            _ => proposal.GetType().Name,
        };
    }

    private static bool IsDuplicate(Session session, ToolId toolId, ArgumentBag arguments)
    {
        foreach (ToolInvocation existing in session.ToolInvocations)
        {
            if (existing.ToolId == toolId && existing.Arguments == arguments)
            {
                return true;
            }
        }

        return false;
    }
}
