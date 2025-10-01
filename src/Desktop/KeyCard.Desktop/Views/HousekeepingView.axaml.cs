// Views/HousekeepingView.axaml.cs
using Avalonia.Controls;

using KeyCard.Desktop.Services;
using KeyCard.Desktop.ViewModels;

namespace KeyCard.Desktop.Views;

public partial class HousekeepingView : UserControl
{
    public HousekeepingView()
    {
        InitializeComponent();

        // In real app, resolve from DI container instead:
        IHousekeepingService svc = new MockHousekeepingService();
        DataContext = new HousekeepingViewModel(svc);
    }
}
