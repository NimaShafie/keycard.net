// Services/Live/HousekeepingServiceAdapter.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
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

        private static readonly string[] RoomsNameContains = { "Room" };
        private static readonly string[] TasksNameContains = { "Housekeeping", "Task" };
        private static readonly string[] StatusNameContains = { "Status" };
        private static readonly string[] VerbsGetOrList = { "Get", "List" };
        private static readonly string[] VerbsPostUpdateOrSet = { "Post", "Update", "Set" };

        public HousekeepingServiceAdapter(
            IHttpClientFactory httpClientFactory,
            ILogger<HousekeepingServiceAdapter> logger,
            KeyCardApiClient? typedClient = null)
        {
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            ArgumentNullException.ThrowIfNull(logger);

            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _typed = typedClient;
        }

        private HttpClient Client => _httpClientFactory.CreateClient("Api");

        public async Task<IReadOnlyList<Room>> GetRoomsAsync()
        {
            if (_typed is not null)
            {
                var m = FindMethod(_typed, contains: RoomsNameContains, verbs: VerbsGetOrList);
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

            try
            {
                var list = await Client.GetFromJsonAsync<List<Room>>("api/admin/rooms").ConfigureAwait(false);
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
                var m = FindMethod(_typed, contains: TasksNameContains, verbs: VerbsGetOrList);
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
                var list = await Client.GetFromJsonAsync<List<HousekeepingTask>>("api/admin/housekeeping").ConfigureAwait(false);
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
                var m = FindMethod(
                    _typed,
                    contains: RoomsNameContains.Concat(StatusNameContains).ToArray(),
                    verbs: VerbsPostUpdateOrSet,
                    parameterCount: 2);

                if (m is not null)
                {
                    try
                    {
                        var result = await InvokeAsync(m, _typed, roomNumber, status.ToString()).ConfigureAwait(false);
                        return result is bool b ? b : true;
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
                    new { status = status.ToString() }).ConfigureAwait(false);

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
                var m = FindMethod(
                    _typed,
                    contains: TasksNameContains.Concat(StatusNameContains).ToArray(),
                    verbs: VerbsPostUpdateOrSet,
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
                    new { status = status.ToString() }).ConfigureAwait(false);

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
            ArgumentNullException.ThrowIfNull(instance);
            contains ??= Array.Empty<string>();
            verbs ??= Array.Empty<string>();

            var methods = instance.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.ReturnType.Name.StartsWith("Task", StringComparison.Ordinal))
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
            ArgumentNullException.ThrowIfNull(m);
            ArgumentNullException.ThrowIfNull(instance);

            var taskObj = m.Invoke(instance, args);
            if (taskObj is Task t)
            {
                await t.ConfigureAwait(false);
                var resultProp = t.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
                return resultProp?.GetValue(t);
            }
            return null;
        }
    }
}
