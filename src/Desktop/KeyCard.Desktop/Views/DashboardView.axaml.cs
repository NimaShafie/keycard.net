// Views/DashboardView.axaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace KeyCard.Desktop.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
