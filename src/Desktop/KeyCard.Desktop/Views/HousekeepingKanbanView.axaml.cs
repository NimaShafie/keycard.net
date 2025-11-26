// Views/HousekeepingKanbanView.axaml.cs
using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using KeyCard.Desktop.Models;
using KeyCard.Desktop.ViewModels;

namespace KeyCard.Desktop.Views;

public partial class HousekeepingKanbanView : UserControl
{
    private HousekeepingViewModel? _vm;

    public HousekeepingKanbanView()
    {
        InitializeComponent();

        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is HousekeepingViewModel vm)
        {
            _vm = vm;
            _vm.StatusMessage = "🔌 Kanban View loaded - setting up drag-drop...";

            // Remove any existing handlers first
            CleanupHandlers();

            // Setup drag and drop
            SetupDragDrop(PendingList, HkTaskStatus.Pending);
            SetupDragDrop(InProgressList, HkTaskStatus.InProgress);
            SetupDragDrop(CompletedList, HkTaskStatus.Completed);

            _vm.StatusMessage = "✅ Drag-drop enabled on all columns";
        }
    }

    private void CleanupHandlers()
    {
        // Remove handlers from all lists to prevent duplicates
        var lists = new[] { PendingList, InProgressList, CompletedList };
        foreach (var list in lists)
        {
            if (list != null)
            {
                list.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
                list.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
                list.RemoveHandler(DragDrop.DropEvent, OnDrop);
            }
        }
    }

    private void SetupDragDrop(ListBox list, HkTaskStatus targetStatus)
    {
        if (list == null || _vm == null) return;

        // Enable drop
        DragDrop.SetAllowDrop(list, true);

        // Tag the list with its target status for the drop handler
        list.Tag = targetStatus;

        // Add drag handler (Tunnel to catch before ListBox handles it)
        list.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);

        // Add drop handlers
        list.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        list.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_vm == null) return;
        if (sender is not ListBox list) return;

        try
        {
            // Only start drag on left button
            var properties = e.GetCurrentPoint(list).Properties;
            if (!properties.IsLeftButtonPressed)
                return;

            // Find the clicked item
            var point = e.GetPosition(list);
            var element = list.InputHitTest(point);

            if (element is Control control)
            {
                // Walk up the visual tree to find the ListBoxItem
                var container = control.FindAncestorOfType<ListBoxItem>();

                if (container?.DataContext is HousekeepingTask task)
                {
                    _vm.StatusMessage = $"🎯 Drag started: {task.Title}";

                    // Create drag data
                    var data = new DataObject();
                    data.Set("HousekeepingTask", task);

                    // Start drag operation
                    var result = await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);

                    _vm.StatusMessage = $"🏁 Drag ended: {result}";
                }
            }
        }
        catch (Exception ex)
        {
            if (_vm != null)
                _vm.StatusMessage = $"❌ Drag error: {ex.Message}";
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        if (_vm == null) return;

        // Check if we have task data
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
        if (_vm == null) return;
        if (sender is not ListBox list) return;

        try
        {
            // Get the task being dropped
            if (e.Data.Get("HousekeepingTask") is HousekeepingTask task)
            {
                // Get target status from the list's Tag
                if (list.Tag is HkTaskStatus targetStatus)
                {
                    _vm.StatusMessage = $"📥 Drop detected: {task.Title} → {targetStatus}";

                    // Move the task
                    _vm.Kanban.MoveTo(task, targetStatus);
                }
                else
                {
                    _vm.StatusMessage = "❌ Drop failed: Could not determine target column";
                }
            }
            else
            {
                _vm.StatusMessage = "❌ Drop failed: No task data in drag operation";
            }
        }
        catch (Exception ex)
        {
            _vm.StatusMessage = $"❌ Drop error: {ex.Message}";
        }

        e.Handled = true;
    }
}
