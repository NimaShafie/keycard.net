// /Infrastructure/UI/ViewChrome.cs
using Avalonia;
using Avalonia.Controls;

namespace KeyCard.Desktop.Infrastructure.UI
{
    /// <summary>
    /// Per-view chrome hints. Views can set ViewChrome.TopBarVisible="False"
    /// to hide the shared top bar when they are active.
    /// </summary>
    public static class ViewChrome
    {
        public static readonly AttachedProperty<bool> TopBarVisibleProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>(
                "TopBarVisible",
                typeof(ViewChrome),
                defaultValue: true);

        public static void SetTopBarVisible(AvaloniaObject element, bool value)
            => element.SetValue(TopBarVisibleProperty, value);

        public static bool GetTopBarVisible(AvaloniaObject element)
            => element.GetValue(TopBarVisibleProperty);
    }
}
