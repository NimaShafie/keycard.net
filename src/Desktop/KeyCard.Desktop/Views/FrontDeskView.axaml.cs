// Views/FrontDeskView.axaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace KeyCard.Desktop.Views;

public partial class FrontDeskView : UserControl
{
    public FrontDeskView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        // Do NOT set DataContext here; ViewLocator/DI handles it.
    }
}
