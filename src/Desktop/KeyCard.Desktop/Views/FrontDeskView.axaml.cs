// Views/FrontDeskView.axaml.cs
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace KeyCard.Desktop.Views;

public partial class FrontDeskView : UserControl
{
    public FrontDeskView()
    {
        InitializeComponent();

        // Keyboard: Ctrl+K focuses the search box
        this.AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        // Do NOT set DataContext here; ViewLocator/DI handles it.
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
