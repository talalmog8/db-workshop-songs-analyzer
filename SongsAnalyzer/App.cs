using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
namespace SongsAnalyzer;

public partial class App : Application
{
    public static ServiceProvider Provider { get; private set; }

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
        services.AddSingleton<ISongAnalyzer, SongAnalyzer>();
        services.AddSingleton<IDatasetLoader, DatasetLoader>();

        Provider = services.BuildServiceProvider();
    }
}