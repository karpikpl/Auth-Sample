using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace desktopClient;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Add services to the container
                services.AddSingleton<Form1>();
            })
            .ConfigureHostConfiguration(configHost =>
            {
                configHost.AddConfiguration(configuration);
            })
            .Build();

        // Get the service and call the Run method
        using (var serviceScope = host.Services.CreateScope())
        {
            var services = serviceScope.ServiceProvider;
            var form1 = services.GetRequiredService<Form1>();
            Application.Run(form1);
        }
    }
}