// Services/Live/HousekeepingServiceAdapter.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using KeyCard.Desktop.Models;
using KeyCard.Desktop.Generated;

namespace KeyCard.Desktop.Services.Live
{
    /// <summary>
    /// Live HK adapter. Prefers typed client; falls back to raw HTTP. Never throws to UI.
    /// </summary>
    public sealed class HousekeepingServiceAdapter : IHousekeepingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HousekeepingServiceAdapter> _logger;
        private readonly KeyCardApiClient? _typed;   // nullable on purpose

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public HousekeepingServiceAdapter(
            IHttpClientFactory httpClientFactory,
            ILogger<HousekeepingServiceAdapter> logger,
            KeyCardApiClient? typedClient = null) // DI will pass this if registered
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _typed = typedClient;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<IReadOnlyList<Room>> GetRoomsAsync()
        {
            // ===== 1) Try typed client =====
            if (_typed is not null)
            {
                // look for something like GetRoomsAsync / ApiAdminRooms / Rooms*
                var m = FindMethod(_typed, contains: new[] { "Room" }, verbs: new[] { "Get", "List" });
                if (m is not null)
                {
                    try
                    {
                        var result = await InvokeAsync(m, _typed).ConfigureAwait(false);
                        var json = JsonSerializer.Serialize(result, _json);
                        var rooms = JsonSerializer.Deserialize<List<Room>>(json, _json) ?? new();
                        return rooms;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex, "Typed GetRoomsAsync failed; falling back to HTTP.");
                    }
                }
            }

            // ===== 2) Fallback to your existing HTTP path (kept intact) =====
            try
            {
                // TODO: replace with your real rooms endpoint when ready
                var list = await Client.GetFromJsonAsync<List<Room>>("api/admin/rooms");
                return (IReadOnlyList<Room>)(list ?? new List<Room>());
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Backend unreachable during GetRoomsAsync; returning empty.");
                return Array.Empty<Room>();
            }
        }

        public async Task<IReadOnlyList<HousekeepingTask>> GetTasksAsync()
        {
            if (_typed is not null)
            {
                var m = FindMethod(_typed, contains: new[] { "Housekeeping", "Task" }, verbs: new[] { "Get", "List" });
                if (m is not null)
                {
                    try
                    {
                        var result = await InvokeAsync(m, _typed).ConfigureAwait(false);
                        var json = JsonSerializer.Serialize(result, _json);
                        var tasks = JsonSerializer.Deserialize<List<HousekeepingTask>>(json, _json) ?? new();
                        return tasks;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex, "Typed GetTasksAsync failed; falling back to HTTP.");
                    }
                }
            }

            try
            {
                var list = await Client.GetFromJsonAsync<List<HousekeepingTask>>("api/admin/housekeeping");
                return (IReadOnlyList<HousekeepingTask>)(list ?? new List<HousekeepingTask>());
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Backend unreachable during GetTasksAsync; returning empty.");
                return Array.Empty<HousekeepingTask>();
            }
        }

        public async Task<bool> UpdateRoomStatusAsync(int roomNumber, RoomStatus status)
        {
            if (_typed is not null)
            {
                var m = FindMethod(_typed,
                    contains: new[] { "Room", "Status" },
                    verbs: new[] { "Post", "Update", "Set" },
                    parameterCount: 2);
                if (m is not null)
                {
                    try
                    {
                        // try (int roomNumber, string status)
                        var result = await InvokeAsync(m, _typed, roomNumber, status.ToString()).ConfigureAwait(false);
                        return result is bool b ? b : true; // assume 2xx as success
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex, "Typed UpdateRoomStatusAsync failed; falling back to HTTP.");
                    }
                }
            }

            try
            {
                using var resp = await Client.PostAsJsonAsync(
                    $"api/admin/rooms/{roomNumber}/status",
                    new { status = status.ToString() });
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Backend unreachable during UpdateRoomStatusAsync.");
                return false;
            }
        }

        public async Task<bool> UpdateTaskStatusAsync(string taskId, HkTaskStatus status)
        {
            if (_typed is not null)
            {
                var m = FindMethod(_typed,
                    contains: new[] { "Housekeeping", "Task", "Status" },
                    verbs: new[] { "Post", "Update", "Set" },
                    parameterCount: 2);
                if (m is not null)
                {
                    try
                    {
                        var result = await InvokeAsync(m, _typed, taskId, status.ToString()).ConfigureAwait(false);
                        return result is bool b ? b : true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation(ex, "Typed UpdateTaskStatusAsync failed; falling back to HTTP.");
                    }
                }
            }

            try
            {
                using var resp = await Client.PostAsJsonAsync(
                    $"api/admin/housekeeping/{Uri.EscapeDataString(taskId)}/status",
                    new { status = status.ToString() });
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Backend unreachable during UpdateTaskStatusAsync.");
                return false;
            }
        }

        // ---- Helpers for reflective call (works with current stub and future NSwag) ----

        private static MethodInfo? FindMethod(
            object instance,
            string[] contains,
            string[] verbs,
            int? parameterCount = null)
        {
            var methods = instance.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.ReturnType.Name.StartsWith("Task"))
                .ToList();

            foreach (var m in methods)
            {
                var name = m.Name;
                if (!verbs.Any(v => name.StartsWith(v, StringComparison.OrdinalIgnoreCase))) continue;
                if (!contains.All(c => name.Contains(c, StringComparison.OrdinalIgnoreCase))) continue;
                if (parameterCount.HasValue && m.GetParameters().Length != parameterCount.Value) continue;
                return m;
            }
            return null;
        }

        private static async Task<object?> InvokeAsync(MethodInfo m, object instance, params object?[] args)
        {
            var taskObj = m.Invoke(instance, args);
            if (taskObj is Task t)
            {
                await t.ConfigureAwait(false);
                var resultProp = t.GetType().GetProperty("Result");
                return resultProp?.GetValue(t);
            }
            return null;
        }
    }
}
