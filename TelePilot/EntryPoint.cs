using System.Configuration;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace TelePilot;

public class EntryPoint
{
    [STAThread]
    static void Main()
    {
        var rightDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName);
        if (rightDir != null)
            Directory.SetCurrentDirectory(rightDir);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var provider = BuildServiceProvider();

        Application.Run(provider.GetRequiredService<TrayApp>());
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureLogging(services);
        services.AddSingleton(ConfigurationManager.LoadConfig());
        services.AddSingleton<TelegramManagerService>();
        services.AddSingleton<TrayApp>();
        services.AddSingleton<Func<string, string?>>(SecretPrompt.ShowDialog);
        return services.BuildServiceProvider();
    }

    private static void ConfigureLogging(ServiceCollection services)
    {

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("log.txt")
            .CreateLogger();
        services.AddLogging(builder => { builder.AddSerilog(); });
    }
}