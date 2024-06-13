using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Model;
using Model.Entities;
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
        
        Provider = services.BuildServiceProvider();
        
        /*
        analyzer.Path = "C:\\Users\\talal\\OneDrive\\Documents\\University\\Current\\databases workshop\\songs\\All Along the Watchtower.txt";
        analyzer.SongName = "All Along the Watchtower.txt";
        analyzer.Performer = new Contributor(firstName: "aal", lastName: "almog");
        analyzer.MusicComposer = new Contributor(firstName: "bal", lastName: "blmog");
        analyzer.Writer = new Contributor(firstName: "cal", lastName: "clmog");

        try
        {
            analyzer.ProcessSong();
        }
        catch (Exception exception)
        {
            Log.Logger.Error(exception, "Failed ProcessSong");
        }
        */
    }
}