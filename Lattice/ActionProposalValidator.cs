namespace Lattice.Core;
public static class ActionProposalValidator
{
    /// <summary>
    /// Checks a proposal against the registered tools. Structural validity (non-empty text,
    /// non-null payloads) is already enforced at construction; this covers the semantic
    /// checks that need context.
    /// </summary>
    public static Result Validate(ActionProposal proposal, ToolCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(catalog);
        return proposal switch
        {
            InvokeToolProposal invoke => ValidateInvoke(invoke, catalog),
            AskUserProposal => Result.Success(),
            RespondProposal => Result.Success(),
            ContinueProposal => Result.Success(),
            FinishProposal => Result.Success(),
            _ => throw new InvalidOperationException(
                $"Unhandled proposal type '{proposal.GetType().Name}'."),
        };
    }
    private static Result ValidateInvoke(InvokeToolProposal proposal, ToolCatalog catalog)
    {
        if (!catalog.TryResolve(proposal.ToolId, out var descriptor))
        {
            return Result.Failure(new Error(
                "action.tool.unknown",
                $"Tool '{proposal.ToolId.Value}' is not registered."));
        }
        return ToolArgumentValidator.Validate(descriptor, proposal.Arguments);
    }
}