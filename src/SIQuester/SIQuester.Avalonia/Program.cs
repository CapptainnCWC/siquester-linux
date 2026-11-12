using Avalonia;

namespace SIQuester.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // UI resources (templates, messages) are localized; ICU is not required (invariant mode), so pick the culture by name.
        try
        {
            // The Linux UI itself is Russian, so keep resources consistent with it.
            var culture = new System.Globalization.CultureInfo("ru-RU");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
        catch
        {
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
