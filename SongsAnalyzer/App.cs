using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
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
            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day) // Log to file
            .CreateLogger();

        // Log application start
        Log.Information("Application started");
        
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();


        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
        
        services.AddSingleton(loggerFactory);
        services.AddSingleton(configuration);
        services.AddSingleton<Func<SongsContext>>(_ => () => new SongsContext(loggerFactory, configuration));
        
        _serviceProvider = services.BuildServiceProvider();

        var fac = _serviceProvider.GetRequiredService<Func<SongsContext>>();
        var ctx = fac();
        var a= ctx.Contributors.ToList();
    }
}