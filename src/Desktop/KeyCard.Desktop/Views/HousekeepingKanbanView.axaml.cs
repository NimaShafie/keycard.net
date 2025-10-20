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
    public HousekeepingKanbanView()
    {
        InitializeComponent();

        // Wire drag sources
        MakeListDraggable(PendingList);
        MakeListDraggable(InProgressList);
        MakeListDraggable(CompletedList);

        // Wire drop targets
        MakeListDroppable(PendingList, HkTaskStatus.Pending);
        MakeListDroppable(InProgressList, HkTaskStatus.InProgress);
        MakeListDroppable(CompletedList, HkTaskStatus.Completed);
    }

    private void MakeListDraggable(ListBox list)
    {
        list.AddHandler(InputElement.PointerPressedEvent, async (_, e) =>
        {
            if (list.SelectedItem is not HousekeepingTask task) return;

            var data = new DataObject();
            data.Set("keycard/hk-task-id", task.Id);

            await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
        }, RoutingStrategies.Tunnel);
    }

    private void MakeListDroppable(ListBox list, HkTaskStatus target)
    {
        list.AddHandler(DragDrop.DragOverEvent, (s, e) =>
        {
            if (!e.Data.Contains("keycard/hk-task-id"))
                e.DragEffects = DragDropEffects.None;
            else
                e.DragEffects = DragDropEffects.Move;

            e.Handled = true;
        });

        list.AddHandler(DragDrop.DropEvent, (s, e) =>
        {
            if (DataContext is not HousekeepingViewModel vm) return;
            var adapter = vm.Kanban;

            if (e.Data.Get("keycard/hk-task-id") is string id)
            {
                var task = adapter.FindById(id);
                if (task is null) return;

                switch (target)
                {
                    case HkTaskStatus.Pending:
                        adapter.DropOnPendingCommand.Execute(task);
                        break;
                    case HkTaskStatus.InProgress:
                        adapter.DropOnInProgressCommand.Execute(task);
                        break;
                    case HkTaskStatus.Completed:
                        adapter.DropOnCompletedCommand.Execute(task);
                        break;
                }
                e.Handled = true;
            }
        });
    }
}
