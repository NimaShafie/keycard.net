// Views/FrontDeskView.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using KeyCard.Desktop.ViewModels;

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

    private void OnBookingsAreaClicked(object? sender, PointerPressedEventArgs e)
    {
        // Check if the click was directly on the background (not on a DataGrid row)
        var source = e.Source as Control;

        // If clicked on Border, Grid, or StackPanel (not DataGrid cells), deselect
        if (source is Border || source is Grid || source is StackPanel || source is TextBlock)
        {
            // Get the ViewModel and clear selection
            if (DataContext is FrontDeskViewModel vm)
            {
                vm.Selected = null;
            }
        }
    }

    private void OnDatePickerAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is CalendarDatePicker datePicker)
        {
            // Find all child controls and disable mouse wheel on them
            DisableMouseWheelRecursively(datePicker);
        }
    }

    private void DisableMouseWheelRecursively(Control control)
    {
        // Add mouse wheel handler to this control
        control.AddHandler(PointerWheelChangedEvent, (s, e) =>
        {
            e.Handled = true;
        }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);

        // Recursively apply to all children
        foreach (var child in control.GetVisualChildren())
        {
            if (child is Control childControl)
            {
                DisableMouseWheelRecursively(childControl);
            }
        }
    }
}
