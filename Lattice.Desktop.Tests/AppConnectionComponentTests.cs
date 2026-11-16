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

        component.Find(".brand").TextContent.Should().Be("Lattice");
        component.Find("#active-workspace").GetAttribute("data-connected").Should().Be("false");
        component.Find("#active-workspace").TextContent.Should().Contain("Not connected");
        component.FindAll("#workspace-path").Should().ContainSingle();
        component.FindAll("#connect-workspace").Should().ContainSingle();
        component.FindAll("[role='alert']").Should().BeEmpty();
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

        component.Find("#active-workspace").GetAttribute("data-connected").Should().Be("true");
        component.Find("#active-workspace").TextContent.Should().Contain(Path.GetFullPath(workspace.Path));
        component.FindAll("[role='alert']").Should().BeEmpty();
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

        component.Find("#active-workspace").GetAttribute("data-connected").Should().Be("false");
        component.Find("[role='alert']").TextContent.Should().ContainEquivalentOf("does not exist");
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
                component.Find("#workspace-form fieldset").HasAttribute("disabled").Should().BeTrue();
                component.Find("#connect-workspace").TextContent.Should().Contain("Connecting");
            });
        }
        finally
        {
            pending.TrySetResult(WorkspaceConnector.Connect(workspace.Path));
        }

        await submit;
        component.Find("#workspace-form fieldset").HasAttribute("disabled").Should().BeFalse();
        component.Find("#active-workspace").GetAttribute("data-connected").Should().Be("true");
    }
}
