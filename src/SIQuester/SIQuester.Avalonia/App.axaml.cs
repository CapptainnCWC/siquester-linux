using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIStatisticsService.Client;
using SIStorage.Service.Client;
using SIQuester.ViewModel.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIQuester.Model;
using SIQuester.ViewModel;
using SIQuester.ViewModel.Configuration;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;

namespace SIQuester.Avalonia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            var platform = new LinuxPlatformManager(window);

            AppSettings.Default = AppSettings.Create();

            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
                .Build();

            services.AddLogging();
            services.AddSIStatisticsServiceClient(config);
            services.AddSIStorageServiceClient(config);
            services.AddChgkServiceClient(config);
            services.AddSingleton(AppSettings.Default);
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IPlatformService>(platform);
            services.AddSIQuester();
            var provider = services.BuildServiceProvider();

            var main = new MainViewModel(
                desktop.Args ?? Array.Empty<string>(),
                new AppOptions(),
                provider.GetRequiredService<IClipboardService>(),
                provider,
                platform,
                provider.GetRequiredService<IDocumentViewModelFactory>(),
                provider.GetRequiredService<ILoggerFactory>());

            window.DataContext = main;
            desktop.MainWindow = window;
            window.Opened += async (_, _) => await main.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
