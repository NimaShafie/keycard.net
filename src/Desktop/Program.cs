using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace KeyCard.Desktop;

/// <summary>Staff desktop shell (Avalonia). Cross-platform Win/macOS/Linux.</summary>
public sealed class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
            d.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}

internal static class Program
{
    public static void Main(string[] args) => App.BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
