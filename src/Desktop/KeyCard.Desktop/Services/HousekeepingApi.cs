// /Services/HousekeepingApi.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services
{
    public interface IHousekeepingApi
    {
        Task<IReadOnlyList<HousekeepingTaskDto>> GetTasksAsync(CancellationToken ct = default);
        Task<HousekeepingTaskDto> CreateTaskAsync(HousekeepingTaskDto task, CancellationToken ct = default);
        Task<HousekeepingTaskDto> UpdateTaskAsync(Guid id, HousekeepingTaskDto task, CancellationToken ct = default);
        Task CompleteTaskAsync(Guid id, CancellationToken ct = default);
        Task DeleteTaskAsync(Guid id, CancellationToken ct = default);
    }

    /// <summary>
    /// Lightweight REST client used in non-Live mode.
    /// </summary>
    public sealed class HousekeepingApi : IHousekeepingApi, IDisposable
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _json;

        public HousekeepingApi(string? baseUrl = null, HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
            baseUrl ??= Environment.GetEnvironmentVariable("KEYCARD_API_BASE")?.TrimEnd('/')
                       ?? "https://localhost:5149";
            _http.BaseAddress = new Uri(baseUrl);
            _json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public async Task<IReadOnlyList<HousekeepingTaskDto>> GetTasksAsync(CancellationToken ct = default)
        {
            var res = await _http.GetAsync("/api/admin/Housekeeping", ct);
            res.EnsureSuccessStatusCode();
            var data = await res.Content.ReadFromJsonAsync<List<HousekeepingTaskDto>>(_json, ct);
            return data ?? new List<HousekeepingTaskDto>();
        }

        public async Task<HousekeepingTaskDto> CreateTaskAsync(HousekeepingTaskDto task, CancellationToken ct = default)
        {
            var res = await _http.PostAsJsonAsync("/api/admin/Housekeeping", task, _json, ct);
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<HousekeepingTaskDto>(_json, ct))!;
        }

        public async Task<HousekeepingTaskDto> UpdateTaskAsync(Guid id, HousekeepingTaskDto task, CancellationToken ct = default)
        {
            var res = await _http.PutAsJsonAsync($"/api/admin/Housekeeping/{id}", task, _json, ct);
            res.EnsureSuccessStatusCode();
            return (await res.Content.ReadFromJsonAsync<HousekeepingTaskDto>(_json, ct))!;
        }

        public async Task CompleteTaskAsync(Guid id, CancellationToken ct = default)
        {
            var res = await _http.PostAsync($"/api/admin/Housekeeping/{id}/complete", content: null, ct);
            res.EnsureSuccessStatusCode();
        }

        public async Task DeleteTaskAsync(Guid id, CancellationToken ct = default)
        {
            var res = await _http.DeleteAsync($"/api/admin/Housekeeping/{id}", ct);
            res.EnsureSuccessStatusCode();
        }

        public void Dispose() => _http.Dispose();
    }

    /// <summary>
    /// In-memory mock used when running in Mock mode.
    /// </summary>
    public sealed class HousekeepingApiMock : IHousekeepingApi
    {
        private readonly List<HousekeepingTaskDto> _tasks = new();

        public HousekeepingApiMock()
        {
            _tasks.Add(new HousekeepingTaskDto
            {
                Id = Guid.NewGuid(),
                RoomNumber = "101",
                Title = "Turnover",
                Notes = "Guest checkout 11am",
                Attendant = null,
                Status = KeyCard.Desktop.Models.TaskStatus.Pending,
                CreatedUtc = DateTime.UtcNow
            });
            _tasks.Add(new HousekeepingTaskDto
            {
                Id = Guid.NewGuid(),
                RoomNumber = "203",
                Title = "Deep clean",
                Notes = "Coffee stain on carpet",
                Attendant = "Marta",
                Status = KeyCard.Desktop.Models.TaskStatus.InProgress,
                CreatedUtc = DateTime.UtcNow.AddHours(-3)
            });
            _tasks.Add(new HousekeepingTaskDto
            {
                Id = Guid.NewGuid(),
                RoomNumber = "315",
                Title = "Amenity restock",
                Notes = "Towels + minibar",
                Attendant = "Ken",
                Status = KeyCard.Desktop.Models.TaskStatus.Completed,
                CreatedUtc = DateTime.UtcNow.AddDays(-1)
            });
        }

        public Task<IReadOnlyList<HousekeepingTaskDto>> GetTasksAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<HousekeepingTaskDto>>(_tasks.ToArray());

        public Task<HousekeepingTaskDto> CreateTaskAsync(HousekeepingTaskDto task, CancellationToken ct = default)
        {
            task.Id = task.Id == Guid.Empty ? Guid.NewGuid() : task.Id;
            task.CreatedUtc = DateTime.UtcNow;
            _tasks.Add(task);
            return Task.FromResult(task);
        }

        public Task<HousekeepingTaskDto> UpdateTaskAsync(Guid id, HousekeepingTaskDto task, CancellationToken ct = default)
        {
            var idx = _tasks.FindIndex(t => t.Id == id);
            if (idx >= 0)
            {
                task.Id = id;
                task.UpdatedUtc = DateTime.UtcNow;
                _tasks[idx] = task;
                return Task.FromResult(task);
            }
            throw new KeyNotFoundException("Task not found");
        }

        public Task CompleteTaskAsync(Guid id, CancellationToken ct = default)
        {
            var t = _tasks.Find(x => x.Id == id) ?? throw new KeyNotFoundException("Task not found");
            t.Status = KeyCard.Desktop.Models.TaskStatus.Completed;
            t.UpdatedUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task DeleteTaskAsync(Guid id, CancellationToken ct = default)
        {
            _tasks.RemoveAll(x => x.Id == id);
            return Task.CompletedTask;
        }
    }
}
