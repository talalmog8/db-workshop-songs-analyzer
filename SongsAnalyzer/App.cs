using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace SongsAnalyzer;

public partial class App : Application
{
    private static ServiceProvider _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<Func<SongsContext>>(_ => () => new SongsContext(configuration));

        _serviceProvider = services.BuildServiceProvider();

        var fac = _serviceProvider.GetRequiredService<Func<SongsContext>>();
        var ctx = fac();
        var a= ctx.Contributors.ToList();
    }
}