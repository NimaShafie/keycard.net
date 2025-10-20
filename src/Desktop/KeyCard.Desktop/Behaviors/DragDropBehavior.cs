// Behaviors/DragDropBehavior.cs
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace KeyCard.Desktop.Behaviors
{
    public static class DragDropBehavior
    {
        public static readonly AttachedProperty<bool> EnableDragProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("EnableDrag", typeof(DragDropBehavior));
        public static void SetEnableDrag(AvaloniaObject o, bool v) => o.SetValue(EnableDragProperty, v);
        public static bool GetEnableDrag(AvaloniaObject o) => o.GetValue(EnableDragProperty);

        public static readonly AttachedProperty<bool> EnableDropProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("EnableDrop", typeof(DragDropBehavior));
        public static void SetEnableDrop(AvaloniaObject o, bool v) => o.SetValue(EnableDropProperty, v);
        public static bool GetEnableDrop(AvaloniaObject o) => o.GetValue(EnableDropProperty);

        public static readonly AttachedProperty<ICommand?> DropCommandProperty =
            AvaloniaProperty.RegisterAttached<Control, ICommand?>("DropCommand", typeof(DragDropBehavior));
        public static void SetDropCommand(AvaloniaObject o, ICommand? v) => o.SetValue(DropCommandProperty, v);
        public static ICommand? GetDropCommand(AvaloniaObject o) => o.GetValue(DropCommandProperty);

        static DragDropBehavior()
        {
            EnableDragProperty.Changed.AddClassHandler<Control>((c, e) =>
            {
                var enabled = e.GetNewValue<bool>();
                if (enabled)
                    c.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
                else
                    c.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            });

            EnableDropProperty.Changed.AddClassHandler<Control>((c, e) =>
            {
                var enabled = e.GetNewValue<bool>();
                DragDrop.SetAllowDrop(c, enabled);
                if (enabled)
                {
                    c.AddHandler(DragDrop.DragOverEvent, OnDragOver);
                    c.AddHandler(DragDrop.DropEvent, OnDrop);
                }
                else
                {
                    c.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
                    c.RemoveHandler(DragDrop.DropEvent, OnDrop);
                }
            });
        }

        private static async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is ListBox lb && GetEnableDrag(lb))
            {
                var point = e.GetPosition(lb);
                var hit = lb.InputHitTest(point) as Control;
                var container = hit?.FindAncestorOfType<ListBoxItem>();
                if (container?.DataContext is null) return;

                var data = new DataObject();
                data.Set("application/x-kanban-task", container.DataContext);
                await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
        }

        private static void OnDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = e.Data.Contains("application/x-kanban-task")
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        }

        private static void OnDrop(object? sender, DragEventArgs e)
        {
            if (sender is not Control target) return;
            var payload = e.Data.Get("application/x-kanban-task");
            var cmd = GetDropCommand(target);
            if (payload != null && cmd?.CanExecute(payload) == true)
                cmd.Execute(payload);
            e.Handled = true;
        }
    }
}
