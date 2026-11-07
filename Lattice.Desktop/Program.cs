using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using PhotinoX.App;

namespace Lattice.Desktop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var builder = PhotinoBlazorApp.CreateBuilder(new PhotinoAppOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
            WebRootPath = "wwwroot",
        });
        builder.Services.AddSingleton<DesktopSession>();
        builder.RootComponents.Add<App>("#app");
        builder.ConfigureMainWindow(window =>
        {
            window.SetTitle("Lattice").SetSize(1200, 820);
        });

        using var app = builder.Build();
        return app.Run();
    }
}
