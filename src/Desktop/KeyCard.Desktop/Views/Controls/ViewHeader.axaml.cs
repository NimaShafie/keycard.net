// src Views/Controls/ViewHeader.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace KeyCard.Desktop.Views.Controls;

public partial class ViewHeader : UserControl
{
    // Bindable properties that XAML can set
    public static readonly StyledProperty<string?> HeaderTextProperty =
        AvaloniaProperty.Register<ViewHeader, string?>(nameof(HeaderText));

    public static readonly StyledProperty<string?> SubHeaderTextProperty =
        AvaloniaProperty.Register<ViewHeader, string?>(nameof(SubHeaderText));

    public string? HeaderText
    {
        get => GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public string? SubHeaderText
    {
        get => GetValue(SubHeaderTextProperty);
        set => SetValue(SubHeaderTextProperty, value);
    }

    public ViewHeader() => AvaloniaXamlLoader.Load(this);
}
