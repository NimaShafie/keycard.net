// src Views/Controls/ViewHeader.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace KeyCard.Desktop.Views.Controls;

public partial class ViewHeader : UserControl
{
    public static readonly StyledProperty<string> HeaderTextProperty =
        AvaloniaProperty.Register<ViewHeader, string>(nameof(HeaderText), "");

    public string HeaderText
    {
        get => GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public ViewHeader()
    {
        // Build the UI in code so we don't depend on a compiled .axaml resource.
        var border = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderBrush = Brush.Parse("#5C46A3"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var text = new TextBlock
        {
            FontSize = 16,
            FontWeight = FontWeight.SemiBold
        };

        // Bind the text to our styled property
        text.Bind(TextBlock.TextProperty, new Binding(nameof(HeaderText)) { Source = this });

        border.Child = text;
        Content = border;
    }
}
