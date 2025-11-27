// Views/HousekeepingView.axaml.cs
using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using KeyCard.Desktop.Models;
using KeyCard.Desktop.ViewModels;

namespace KeyCard.Desktop.Views
{
    public partial class HousekeepingView : UserControl
    {
        private HousekeepingViewModel? _vm;
        private bool _dragDropInitialized;

        public HousekeepingView()
        {
            InitializeComponent();

            this.DataContextChanged += OnDataContextChanged;

            var kanbanTab = this.FindControl<TabItem>("KanbanTab");
            if (kanbanTab != null)
            {
                kanbanTab.PropertyChanged += (s, e) =>
                {
                    if (e.Property.Name == nameof(TabItem.IsSelected) && kanbanTab.IsSelected && _vm != null)
                    {
                        if (!_dragDropInitialized)
                        {
                            InitializeDragDrop();
                        }
                    }
                };
            }
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is HousekeepingViewModel vm)
            {
                _vm = vm;

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_vm != null && !_dragDropInitialized)
                    {
                        InitializeDragDrop();
                    }
                }, Avalonia.Threading.DispatcherPriority.Loaded);
            }
        }

        private void InitializeDragDrop()
        {
            if (_vm == null) return;

            var pendingList = this.FindControl<ListBox>("PendingList");
            var inProgressList = this.FindControl<ListBox>("InProgressList");
            var completedList = this.FindControl<ListBox>("CompletedList");

            if (pendingList != null && inProgressList != null && completedList != null)
            {
                SetupListDragDrop(pendingList, HkTaskStatus.Pending);
                SetupListDragDrop(inProgressList, HkTaskStatus.InProgress);
                SetupListDragDrop(completedList, HkTaskStatus.Completed);

                _dragDropInitialized = true;
            }
        }

        private void SetupListDragDrop(ListBox list, HkTaskStatus targetStatus)
        {
            if (list == null || _vm == null) return;

            DragDrop.SetAllowDrop(list, true);
            list.Tag = targetStatus;

            list.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            list.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
            list.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
            list.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
            list.RemoveHandler(DragDrop.DropEvent, OnDrop);

            list.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            list.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
            list.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
            list.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            list.AddHandler(DragDrop.DropEvent, OnDrop);
        }

        private Point? _dragStartPoint;
        private HousekeepingTask? _dragTask;
        private const double DragThreshold = 5.0;

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_vm == null || sender is not ListBox list) return;

            try
            {
                var properties = e.GetCurrentPoint(list).Properties;
                if (!properties.IsLeftButtonPressed) return;

                var point = e.GetPosition(list);
                var element = list.InputHitTest(point) as Control;

                if (element is ComboBox || element is Button || element is TextBox)
                {
                    _dragStartPoint = null;
                    _dragTask = null;
                    return;
                }

                var parent = element;
                while (parent != null)
                {
                    if (parent is ComboBox || parent is Button || parent is TextBox)
                    {
                        _dragStartPoint = null;
                        _dragTask = null;
                        return;
                    }
                    if (parent is ListBoxItem) break;
                    parent = parent.Parent as Control;
                }

                var container = element?.FindAncestorOfType<ListBoxItem>();
                if (container?.DataContext is HousekeepingTask task)
                {
                    _dragStartPoint = point;
                    _dragTask = task;
                }
            }
            catch
            {
                // Silently handle errors
            }
        }

        private async void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_vm == null || _dragStartPoint == null || _dragTask == null) return;
            if (sender is not ListBox list) return;

            try
            {
                var properties = e.GetCurrentPoint(list).Properties;
                if (!properties.IsLeftButtonPressed)
                {
                    _dragStartPoint = null;
                    _dragTask = null;
                    return;
                }

                var currentPoint = e.GetPosition(list);
                var diff = currentPoint - _dragStartPoint.Value;
                var distance = Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);

                if (distance > DragThreshold)
                {
                    var task = _dragTask;
                    _dragStartPoint = null;
                    _dragTask = null;

                    var data = new DataObject();
                    data.Set("HousekeepingTask", task);
                    data.Set("SourceList", list.Name);

                    await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
                }
            }
            catch
            {
                // Silently handle errors
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _dragStartPoint = null;
            _dragTask = null;
        }

        private void OnDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains("HousekeepingTask"))
            {
                e.DragEffects = DragDropEffects.Move;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (_vm == null || sender is not ListBox list) return;

            try
            {
                if (e.Data.Get("HousekeepingTask") is HousekeepingTask task)
                {
                    if (list.Tag is HkTaskStatus targetStatus)
                    {
                        var sourceList = e.Data.Get("SourceList") as string;
                        var targetList = list.Name;

                        if (sourceList == targetList)
                        {
                            e.DragEffects = DragDropEffects.None;
                            e.Handled = true;
                            return;
                        }

                        if (task.Status == targetStatus)
                        {
                            e.DragEffects = DragDropEffects.None;
                            e.Handled = true;
                            return;
                        }

                        _vm.Kanban.MoveTo(task, targetStatus);
                    }
                }
            }
            catch
            {
                // Silently handle errors
            }

            e.Handled = true;
        }
    }
}
