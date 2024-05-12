using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
namespace SongsAnalyzer;

public partial class App : Application
{
    private static ServiceProvider _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Configure Serilog for logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information) // Set minimum level for Microsoft.* namespaces
            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day) // Log to file
            .CreateLogger();

        // Log application start
        Log.Information("Application started");
        
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<Func<SongsContext>>(_ => () => new SongsContext(configuration));
        services.AddLogging(loggingBuilder => {
            loggingBuilder.AddSerilog(logger: Log.Logger);
        });
        
        _serviceProvider = services.BuildServiceProvider();

        var fac = _serviceProvider.GetRequiredService<Func<SongsContext>>();
        var ctx = fac();
        var a= ctx.Contributors.ToList();
    }
}