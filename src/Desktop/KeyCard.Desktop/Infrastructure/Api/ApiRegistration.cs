// Infrastructure/Api/ApiRegistration.cs
using System;
using System.Net.Http;
using System.Net.Http.Json; // for GetFromJsonAsync / PutAsJsonAsync
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.Services.Live;

namespace KeyCard.Desktop.Infrastructure.Api
{
    public static class ApiRegistration
    {
        /// <summary>
        /// Overload used by Program.cs: services.AddKeyCardApi(configuration, appEnv)
        /// </summary>
        public static IServiceCollection AddKeyCardApi(
            this IServiceCollection services,
            IConfiguration configuration,
            IAppEnvironment appEnv)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(appEnv);

            var baseUrl =
                configuration["Api:BaseUrl"] ??
                Environment.GetEnvironmentVariable("KEYCARD_API_BASE") ??
                "https://localhost:5149";

            // Named client used by the Live adapter fallback paths and by lightweight REST client
            services.AddHttpClient("Api", http =>
            {
                http.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
            });

            var useLive = !appEnv.IsMock;

            if (useLive)
            {
                // Live adapter for the richer service surface
                services.AddSingleton<IHousekeepingService>(sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var logger = sp.GetRequiredService<ILogger<HousekeepingServiceAdapter>>();
                    // typed client is optional and injected via other registrations if present; adapter handles null
                    var typed = sp.GetService<KeyCard.Desktop.Generated.KeyCardApiClient>();
                    return new HousekeepingServiceAdapter(factory, logger, typed);
                });

                // For desktop UI that depends on IHousekeepingApi (simple CRUD over REST),
                // keep using the lightweight REST client against the same base URL.
                services.AddSingleton<IHousekeepingApi>(sp =>
                    new KeyCard.Desktop.Services.HousekeepingApi(baseUrl));
            }
            else
            {
                // Mock / lightweight mode
                services.AddSingleton<IHousekeepingApi>(sp =>
                    new KeyCard.Desktop.Services.HousekeepingApi(baseUrl));

                // If some parts of Desktop ask for IHousekeepingService, provide a tiny shim
                services.AddSingleton<IHousekeepingService>(sp =>
                    new HousekeepingServiceShim(
                        sp.GetRequiredService<IHousekeepingApi>(),
                        sp.GetRequiredService<IHttpClientFactory>(),
                        sp.GetRequiredService<ILogger<HousekeepingServiceShim>>()));
            }

            return services;
        }

        /// <summary>
        /// Back-compat overload (in case other code uses this old signature).
        /// </summary>
        public static IServiceCollection AddApiClients(this IServiceCollection services, string? apiBaseUrl, bool useLive)
        {
            ArgumentNullException.ThrowIfNull(services);

            var baseUrl = apiBaseUrl ??
                          Environment.GetEnvironmentVariable("KEYCARD_API_BASE") ??
                          "https://localhost:5149";

            services.AddHttpClient("Api", http =>
            {
                http.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
            });

            if (useLive)
            {
                services.AddSingleton<IHousekeepingService>(sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var logger = sp.GetRequiredService<ILogger<HousekeepingServiceAdapter>>();
                    var typed = sp.GetService<KeyCard.Desktop.Generated.KeyCardApiClient>();
                    return new HousekeepingServiceAdapter(factory, logger, typed);
                });

                services.AddSingleton<IHousekeepingApi>(sp =>
                    new KeyCard.Desktop.Services.HousekeepingApi(baseUrl));
            }
            else
            {
                services.AddSingleton<IHousekeepingApi>(sp =>
                    new KeyCard.Desktop.Services.HousekeepingApi(baseUrl));

                services.AddSingleton<IHousekeepingService>(sp =>
                    new HousekeepingServiceShim(
                        sp.GetRequiredService<IHousekeepingApi>(),
                        sp.GetRequiredService<IHttpClientFactory>(),
                        sp.GetRequiredService<ILogger<HousekeepingServiceShim>>()));
            }

            return services;
        }

        /// <summary>
        /// Minimal shim: adapts the lightweight IHousekeepingApi to IHousekeepingService for callers that expect it.
        /// Uses IHousekeepingApi for tasks; uses the named HttpClient ("Api") directly for room + status endpoints,
        /// since IHousekeepingApi does not expose those members.
        /// </summary>
        private sealed class HousekeepingServiceShim : IHousekeepingService
        {
            private readonly IHousekeepingApi _api;
            private readonly IHttpClientFactory _httpFactory;
            private readonly ILogger<HousekeepingServiceShim> _logger;

            public HousekeepingServiceShim(
                IHousekeepingApi api,
                IHttpClientFactory httpFactory,
                ILogger<HousekeepingServiceShim> logger)
            {
                _api = api ?? throw new ArgumentNullException(nameof(api));
                _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<KeyCard.Desktop.Models.HousekeepingTask>> GetTasksAsync()
            {
                var items = await _api.GetTasksAsync().ConfigureAwait(false);
                var list = new System.Collections.Generic.List<KeyCard.Desktop.Models.HousekeepingTask>(items.Count);
                foreach (var t in items)
                {
                    list.Add(new KeyCard.Desktop.Models.HousekeepingTask
                    {
                        Id = t.Id.ToString(),
                        Title = t.Title ?? string.Empty,
                        Notes = t.Notes
                    });
                }
                return list;
            }

            // ---- Rooms & Status updates via direct REST calls ----
            // Adjust endpoint paths if your Swagger differs. These defaults aim to be intuitive and
            // easy to tweak: /api/housekeeping/rooms, /api/housekeeping/rooms/{id}/status, /api/housekeeping/tasks/{id}/status

            public async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<KeyCard.Desktop.Models.Room>> GetRoomsAsync()
            {
                var client = _httpFactory.CreateClient("Api");

                // GET /api/housekeeping/rooms
                var rooms = await client.GetFromJsonAsync<System.Collections.Generic.List<KeyCard.Desktop.Models.Room>>(
                    "api/housekeeping/rooms").ConfigureAwait(false);

                // FIX: ensure same type for null-coalescing (IReadOnlyList<Room>)
                return rooms ?? new System.Collections.Generic.List<KeyCard.Desktop.Models.Room>();
            }

            public async System.Threading.Tasks.Task<bool> UpdateRoomStatusAsync(int roomId, KeyCard.Desktop.Models.RoomStatus status)
            {
                var client = _httpFactory.CreateClient("Api");

                // PUT /api/housekeeping/rooms/{roomId}/status  body: { status: "..." }
                var response = await client.PutAsJsonAsync(
                    $"api/housekeeping/rooms/{roomId}/status",
                    new { status }).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("UpdateRoomStatusAsync failed for Room {RoomId} with HTTP {Status}", roomId, (int)response.StatusCode);
                    return false;
                }

                return true;
            }

            public async System.Threading.Tasks.Task<bool> UpdateTaskStatusAsync(string taskId, KeyCard.Desktop.Models.HkTaskStatus status)
            {
                var client = _httpFactory.CreateClient("Api");

                // PUT /api/housekeeping/tasks/{taskId}/status  body: { status: "..." }
                var response = await client.PutAsJsonAsync(
                    $"api/housekeeping/tasks/{taskId}/status",
                    new { status }).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("UpdateTaskStatusAsync failed for Task {TaskId} with HTTP {Status}", taskId, (int)response.StatusCode);
                    return false;
                }

                return true;
            }
        }
    }
}
