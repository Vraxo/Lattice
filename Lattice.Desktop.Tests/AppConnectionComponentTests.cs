using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Lattice.Desktop.Tests;

public sealed class AppConnectionComponentTests
{
    [Fact]
    public async Task InitialWindowShowsIdentityDisconnectedIndicatorAndConnectControls()
    {
        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();

        IRenderedComponent<App> component = context.Render<App>();

        Assert.Equal("Lattice", component.Find(".brand").TextContent);
        Assert.Equal("false", component.Find("#active-workspace").GetAttribute("data-connected"));
        Assert.Contains("Not connected", component.Find("#active-workspace").TextContent, StringComparison.Ordinal);
        Assert.Single(component.FindAll("#workspace-path"));
        Assert.Single(component.FindAll("#connect-workspace"));
        Assert.Empty(component.FindAll("[role='alert']"));
    }

    [Fact]
    public async Task SubmittingExistingWorkspaceFolderUpdatesDisplayedActiveWorkspace()
    {
        using DesktopTempWorkspace workspace = new();
        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();

        IRenderedComponent<App> component = context.Render<App>();
        await component.Find("#workspace-path").InputAsync(workspace.Path);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        Assert.Equal("true", component.Find("#active-workspace").GetAttribute("data-connected"));
        Assert.Contains(
            Path.GetFullPath(workspace.Path),
            component.Find("#active-workspace").TextContent,
            StringComparison.Ordinal);
        Assert.Empty(component.FindAll("[role='alert']"));
    }

    [Fact]
    public async Task SubmittingInvalidWorkspacePathRendersAlertAndStaysDisconnected()
    {
        string missing = Path.Combine(Path.GetTempPath(), "lattice-missing-ui-" + Guid.NewGuid().ToString("N"));
        await using BunitContext context = new();
        context.Services.AddSingleton<DesktopSession>();

        IRenderedComponent<App> component = context.Render<App>();
        await component.Find("#workspace-path").InputAsync(missing);
        await component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);

        Assert.Equal("false", component.Find("#active-workspace").GetAttribute("data-connected"));
        Assert.Contains("does not exist", component.Find("[role='alert']").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PendingConnectionDisablesWorkspaceFieldsetUntilResolved()
    {
        using DesktopTempWorkspace workspace = new();
        TaskCompletionSource<WorkspaceConnectionResult> pending =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BunitContext context = new();
        context.Services.AddSingleton(new DesktopSession((_, _) => pending.Task));

        IRenderedComponent<App> component = context.Render<App>();
        await component.Find("#workspace-path").InputAsync(workspace.Path);
        Task submit = component.Find("#workspace-form").TriggerEventAsync("onsubmit", EventArgs.Empty);
        try
        {
            component.WaitForAssertion(() =>
            {
                Assert.True(component.Find("#workspace-form fieldset").HasAttribute("disabled"));
                Assert.Contains("Connecting", component.Find("#connect-workspace").TextContent, StringComparison.Ordinal);
            });
        }
        finally
        {
            pending.TrySetResult(WorkspaceConnector.Connect(workspace.Path));
        }

        await submit;
        Assert.False(component.Find("#workspace-form fieldset").HasAttribute("disabled"));
        Assert.Equal("true", component.Find("#active-workspace").GetAttribute("data-connected"));
    }
}
