// ViewMoels/HousekeepingKanbanViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Models;
using KeyCard.Desktop.Services;
// alias not required here, we don’t reference System.Threading.Tasks.TaskStatus
using TaskStatus = KeyCard.Desktop.Models.TaskStatus;

namespace KeyCard.Desktop.ViewModels
{
    public sealed class HousekeepingKanbanViewModel : INotifyPropertyChanged
    {
        private readonly IHousekeepingApi _api;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<HousekeepingTaskDto> Tasks { get; } = new();
        public ObservableCollection<HousekeepingTaskDto> Pending { get; } = new();
        public ObservableCollection<HousekeepingTaskDto> InProgress { get; } = new();
        public ObservableCollection<HousekeepingTaskDto> Completed { get; } = new();

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set { _isBusy = value; OnChanged(nameof(IsBusy)); } }

        public string? NewRoom { get; set; }
        public string? NewTitle { get; set; }
        public string? NewNotes { get; set; }

        public bool IsMock { get; }

        // kept for your inline prompt
        public Func<string, string, string?, string?>? PromptProvider { get; set; }

        public ICommand RefreshCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand AssignAttendantCommand { get; }
        public ICommand DropOnPendingCommand { get; }
        public ICommand DropOnInProgressCommand { get; }
        public ICommand DropOnCompletedCommand { get; }

        public HousekeepingKanbanViewModel()
        {
            var mode = (Environment.GetEnvironmentVariable("KEYCARD_MODE") ?? "Mock").Trim();
            IsMock = mode.Equals("Mock", StringComparison.OrdinalIgnoreCase);

            _api = IsMock
                ? new HousekeepingApiMock()
                : new HousekeepingApi(Environment.GetEnvironmentVariable("KEYCARD_API_BASE"));

            RefreshCommand = new Relay(async _ => await LoadAsync());
            AddTaskCommand = new Relay(async _ => await AddTaskAsync());
            DeleteTaskCommand = new Relay(async idObj =>
            {
                if (idObj is Guid id) await DeleteAsync(id);
            });

            AssignAttendantCommand = new Relay(async taskObj =>
            {
                if (taskObj is not HousekeepingTaskDto t) return;
                await SaveAsync(t);
            });

            DropOnPendingCommand = new Relay(async payload => await OnDropChangeStatusAsync(payload, TaskStatus.Pending));
            DropOnInProgressCommand = new Relay(async payload => await OnDropChangeStatusAsync(payload, TaskStatus.InProgress));
            DropOnCompletedCommand = new Relay(async payload => await OnDropChangeStatusAsync(payload, TaskStatus.Completed));
        }

        // used by your view
        public Task InitializeAsync() => LoadAsync();

        private async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                Tasks.Clear();
                Pending.Clear(); InProgress.Clear(); Completed.Clear();

                var items = await _api.GetTasksAsync();
                foreach (var t in items.OrderBy(t => t.CreatedUtc))
                    Tasks.Add(t);

                RebuildBuckets();
            }
            finally { IsBusy = false; }
        }

        private void RebuildBuckets()
        {
            Pending.Clear();
            InProgress.Clear();
            Completed.Clear();

            foreach (var t in Tasks)
            {
                switch (t.Status)
                {
                    case TaskStatus.Pending: Pending.Add(t); break;
                    case TaskStatus.InProgress: InProgress.Add(t); break;
                    case TaskStatus.Completed: Completed.Add(t); break;
                }
            }

            OnChanged(nameof(Pending));
            OnChanged(nameof(InProgress));
            OnChanged(nameof(Completed));
        }

        private async Task AddTaskAsync()
        {
            if (string.IsNullOrWhiteSpace(NewRoom) || string.IsNullOrWhiteSpace(NewTitle))
                return;

            var newTask = new HousekeepingTaskDto
            {
                RoomNumber = NewRoom!.Trim(),
                Title = NewTitle!.Trim(),
                Notes = string.IsNullOrWhiteSpace(NewNotes) ? null : NewNotes!.Trim(),
                Status = TaskStatus.Pending
            };

            var saved = await _api.CreateTaskAsync(newTask);
            Tasks.Add(saved);
            RebuildBuckets();

            NewRoom = null; NewTitle = null; NewNotes = null;
        }

        private async Task SaveAsync(HousekeepingTaskDto task)
        {
            var updated = await _api.UpdateTaskAsync(task.Id, task);
            var idx = Tasks.IndexOf(Tasks.First(x => x.Id == task.Id));
            Tasks[idx] = updated;
            RebuildBuckets();
        }

        private async Task DeleteAsync(Guid id)
        {
            await _api.DeleteTaskAsync(id);
            var existing = Tasks.FirstOrDefault(t => t.Id == id);
            if (existing != null) Tasks.Remove(existing);
            RebuildBuckets();
        }

        private async Task OnDropChangeStatusAsync(object? payload, TaskStatus newStatus)
        {
            if (payload is not HousekeepingTaskDto t) return;
            if (t.Status == newStatus) return;

            if (newStatus == TaskStatus.Completed)
            {
                await _api.CompleteTaskAsync(t.Id);
                t.Status = TaskStatus.Completed;
            }
            else
            {
                t.Status = newStatus;
                await SaveAsync(t);
                return;
            }

            var idx = Tasks.IndexOf(Tasks.First(x => x.Id == t.Id));
            Tasks[idx] = t;
            RebuildBuckets();
        }

        private void OnChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class Relay : ICommand
    {
        private readonly Func<object?, Task> _async;
        private readonly Predicate<object?>? _can;

        public Relay(Func<object?, Task> executeAsync, Predicate<object?>? can = null)
        {
            _async = executeAsync;
            _can = can;
        }

        public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
        public event EventHandler? CanExecuteChanged;
        public async void Execute(object? parameter) => await _async(parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
