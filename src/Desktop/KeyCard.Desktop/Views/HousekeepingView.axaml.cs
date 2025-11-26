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
        private bool _dragDropInitialized = false;

        public HousekeepingView()
        {
            InitializeComponent();

            this.DataContextChanged += OnDataContextChanged;

            // Setup drag-drop when Kanban tab is selected
            var kanbanTab = this.FindControl<TabItem>("KanbanTab");
            if (kanbanTab != null)
            {
                kanbanTab.PropertyChanged += (s, e) =>
                {
                    if (e.Property.Name == nameof(TabItem.IsSelected) && kanbanTab.IsSelected && _vm != null)
                    {
                        // ✅ ALWAYS restore assignments when tab becomes visible
                        _vm.Kanban.RestoreAssignments();

                        // But only initialize drag-drop once
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
                _vm.StatusMessage = "🔌 HousekeepingView loaded";

                // ✅ ALWAYS try to restore when view loads
                // Use dispatcher to ensure UI is ready
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_vm != null)
                    {
                        _vm.Kanban.RestoreAssignments();

                        if (!_dragDropInitialized)
                        {
                            InitializeDragDrop();
                        }
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
                _vm.StatusMessage = "✅ Drag-drop initialized";
            }
            else
            {
                _vm.StatusMessage = "❌ Failed to find ListBox controls for drag-drop";
            }
        }

        private void SetupListDragDrop(ListBox list, HkTaskStatus targetStatus)
        {
            if (list == null || _vm == null) return;

            // Enable drop
            DragDrop.SetAllowDrop(list, true);
            list.Tag = targetStatus;

            // Remove any existing handlers
            list.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            list.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
            list.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
            list.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
            list.RemoveHandler(DragDrop.DropEvent, OnDrop);

            // Add new handlers
            list.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            list.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
            list.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
            list.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            list.AddHandler(DragDrop.DropEvent, OnDrop);
        }

        private Point? _dragStartPoint;
        private HousekeepingTask? _dragTask;
        private const double DragThreshold = 5.0; // pixels to move before drag starts

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_vm == null || sender is not ListBox list) return;

            try
            {
                var properties = e.GetCurrentPoint(list).Properties;
                if (!properties.IsLeftButtonPressed) return;

                var point = e.GetPosition(list);
                var element = list.InputHitTest(point) as Control;

                // ✅ CRITICAL: Don't start drag if clicking on interactive elements
                if (element is ComboBox || element is Button || element is TextBox)
                {
                    _dragStartPoint = null;
                    _dragTask = null;
                    return;
                }

                // Check if clicked element is INSIDE a ComboBox or Button
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

                // Find the task being clicked
                var container = element?.FindAncestorOfType<ListBoxItem>();
                if (container?.DataContext is HousekeepingTask task)
                {
                    // Store drag start info but don't start drag yet
                    _dragStartPoint = point;
                    _dragTask = task;
                }
            }
            catch (Exception ex)
            {
                if (_vm != null)
                    _vm.StatusMessage = $"❌ Pointer pressed error: {ex.Message}";
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
                    // Mouse button released, cancel drag
                    _dragStartPoint = null;
                    _dragTask = null;
                    return;
                }

                var currentPoint = e.GetPosition(list);
                var diff = currentPoint - _dragStartPoint.Value;
                var distance = Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);

                // ✅ Only start drag after moving threshold distance
                if (distance > DragThreshold)
                {
                    var task = _dragTask;
                    _dragStartPoint = null;
                    _dragTask = null;

                    _vm.StatusMessage = $"🎯 Drag started: {task.Title}";

                    var data = new DataObject();
                    data.Set("HousekeepingTask", task);
                    data.Set("SourceList", list.Name); // Track source column

                    var result = await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);

                    if (result == DragDropEffects.None)
                    {
                        _vm.StatusMessage = $"❌ Drag cancelled for: {task.Title}";
                    }
                }
            }
            catch (Exception ex)
            {
                if (_vm != null)
                    _vm.StatusMessage = $"❌ Drag error: {ex.Message}";
            }
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            // Clear drag state if mouse released without starting drag
            _dragStartPoint = null;
            _dragTask = null;
        }

        private void OnStaffAssignmentChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cb || cb.Tag is not HousekeepingTask task || _vm == null)
                return;

            var selected = cb.SelectedItem as string;
            if (string.IsNullOrEmpty(selected) || selected == "— Unassigned —")
                return;

            // ✅ Save directly to dictionary
            _vm.Kanban.SaveAssignment(task.Id, selected);
            _vm.StatusMessage = $"✅ AUTO-SAVED: {selected} → {task.Title}";
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
                        // ✅ Check if source and target are the same
                        var sourceList = e.Data.Get("SourceList") as string;
                        var targetList = list.Name;

                        if (sourceList == targetList)
                        {
                            _vm.StatusMessage = $"⚠️ Cannot drop in same column: {task.Title}";
                            e.DragEffects = DragDropEffects.None;
                            e.Handled = true;
                            return;
                        }

                        // Also check if task status matches target (redundant check)
                        if (task.Status == targetStatus)
                        {
                            _vm.StatusMessage = $"⚠️ Task already in {targetStatus}: {task.Title}";
                            e.DragEffects = DragDropEffects.None;
                            e.Handled = true;
                            return;
                        }

                        _vm.StatusMessage = $"📥 Dropping: {task.Title} → {targetStatus}";
                        _vm.Kanban.MoveTo(task, targetStatus);
                    }
                    else
                    {
                        _vm.StatusMessage = "❌ Drop failed: Target column not identified";
                    }
                }
                else
                {
                    _vm.StatusMessage = "❌ Drop failed: No task data";
                }
            }
            catch (Exception ex)
            {
                _vm.StatusMessage = $"❌ Drop error: {ex.Message}";
            }

            e.Handled = true;
        }
    }
}
