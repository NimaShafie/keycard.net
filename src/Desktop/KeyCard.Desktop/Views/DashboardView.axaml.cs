// Views/DashboardView.axaml.cs
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace KeyCard.Desktop.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();

        // Keyboard: Ctrl+K focuses the filter box
        this.AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.K)
        {
            var box = this.FindControl<TextBox>("SearchBox");
            box?.Focus();
            e.Handled = true;
        }
    }
}
