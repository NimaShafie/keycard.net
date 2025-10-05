using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace KeyCard.Desktop.Views;

public partial class LoginView : UserControl
{
    private readonly DispatcherTimer _animTimer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private double _phase;

    public LoginView()
    {
        InitializeComponent();

        // Conditionally load logo image (prevents XAML parse exception when the resource is absent)
        try
        {
            var uri = new Uri("avares://KeyCard.Desktop/Assets/logo.png");
            var exists = AssetLoader.Exists(uri);
            var img = this.FindControl<Image>("BrandLogo");
            if (img is not null)
            {
                if (exists)
                {
                    using var stream = AssetLoader.Open(uri);
                    img.Source = new Bitmap(stream);
                    img.IsVisible = true;
                }
                else
                {
                    img.IsVisible = false;
                }
            }
        }
        catch
        {
            if (this.FindControl<Image>("BrandLogo") is { } img) img.IsVisible = false;
        }

        // Fade + slight scale-in
        this.AttachedToVisualTree += (_, __) =>
        {
            if (this.FindControl<Border>("LoginCard") is { } card)
            {
                card.Opacity = 0;
                card.RenderTransform = new ScaleTransform(0.98, 0.98);

                var start = DateTime.UtcNow;
                DispatcherTimer.Run(() =>
                {
                    var p = Math.Clamp((DateTime.UtcNow - start).TotalMilliseconds / 350.0, 0, 1);
                    card.Opacity = p;
                    var s = 0.98 + 0.02 * p;
                    card.RenderTransform = new ScaleTransform(s, s);
                    return p < 1;
                }, TimeSpan.FromMilliseconds(16));
            }
        };

        // Animated gradient drift
        _animTimer.Tick += (_, __) =>
        {
            if (this.FindControl<Border>("AnimatedBackground")?.Background is LinearGradientBrush b)
            {
                _phase += 0.0025;
                var x = 0.5 + 0.5 * Math.Sin(_phase);
                var y = 0.5 + 0.5 * Math.Cos(_phase * 0.8);

                b.StartPoint = new RelativePoint(1 - x, 1 - y, RelativeUnit.Relative);
                b.EndPoint = new RelativePoint(x, y, RelativeUnit.Relative);
            }
        };
        _animTimer.Start();
    }
}
