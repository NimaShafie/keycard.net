// Views/HousekeepingView.axaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace KeyCard.Desktop.Views;

public partial class HousekeepingView : UserControl
{
    public HousekeepingView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        // NOTE: Do NOT set DataContext here.
        // Your ViewLocator/DI wiring will provide the correct ViewModel instance.
    }
}
