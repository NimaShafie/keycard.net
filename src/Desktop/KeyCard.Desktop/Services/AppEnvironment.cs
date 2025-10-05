// Services/AppEnvironment.cs
using System;

using KeyCard.Desktop.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace KeyCard.Desktop.Services
{
    public sealed class AppEnvironment : IAppEnvironment
    {
        private readonly KeyCardOptions _keyCard;     // bound from "KeyCard"
        private readonly ApiOptions _api;             // bound from "Api" (root)
        private readonly SignalROptions _signalR;     // bound from "SignalR" (root)
        private readonly IHostEnvironment _hostEnv;
        private readonly IConfiguration _config;      // raw for fallbacks

        private readonly bool _isMock;                // cached once
        private readonly string _apiBaseUrl;          // cached once
        private readonly string _bookingsHubUrl;      // cached once

        public AppEnvironment(
            IOptionsMonitor<KeyCardOptions> keyCard,
            IOptionsMonitor<ApiOptions> api,
            IOptionsMonitor<SignalROptions> signalR,
            IHostEnvironment hostEnv,
            IConfiguration config)
        {
            _keyCard = keyCard.CurrentValue;
            _api = api.CurrentValue;
            _signalR = signalR.CurrentValue;
            _hostEnv = hostEnv;
            _config = config;

            _isMock = EvaluateIsMock();
            _apiBaseUrl = ResolveApiBaseUrl();
            _bookingsHubUrl = ResolveBookingsHubUrl();
        }

        public string EnvironmentName => _hostEnv.EnvironmentName;
        public bool IsMock => _isMock;
        public bool IsLive => !_isMock;
        public string ApiBaseUrl => _apiBaseUrl;
        public string BookingsHubUrl => _bookingsHubUrl;

        private bool EvaluateIsMock()
        {
            // 1) Preferred: typed "KeyCard" options
            if (EqualsIgnoreCase(_keyCard.Mode, "Mock") || _keyCard.UseMocks)
                return true;

            // 2) Fallbacks: read raw config in BOTH shapes
            //    - Nested under KeyCard
            var modeNested = _config["KeyCard:Mode"];
            var useNested = _config["KeyCard:UseMocks"];
            if (EqualsIgnoreCase(modeNested, "Mock")) return true;
            if (bool.TryParse(useNested, out var nestedFlag) && nestedFlag) return true;

            //    - Root
            var modeRoot = _config["Mode"];
            var useRoot = _config["UseMocks"];
            if (EqualsIgnoreCase(modeRoot, "Mock")) return true;
            if (bool.TryParse(useRoot, out var rootFlag) && rootFlag) return true;

            // 3) Environment: treat "Development" as mock-friendly unless explicitly disabled
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                   ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (EqualsIgnoreCase(env, "Development"))
            {
                // If someone explicitly set UseMocks=false anywhere, honor it.
                if (bool.TryParse(useNested, out var nestedOff) && !nestedOff) return false;
                if (bool.TryParse(useRoot, out var rootOff) && !rootOff) return false;
                return true;
            }

            return false;
        }

        private string ResolveApiBaseUrl()
        {
            // Preferred: typed options bound from ROOT "Api"
            if (!string.IsNullOrWhiteSpace(_api.BaseUrl)) return _api.BaseUrl!;
            if (!string.IsNullOrWhiteSpace(_api.HttpsBaseUrl)) return _api.HttpsBaseUrl!;
            if (!string.IsNullOrWhiteSpace(_api.HttpBaseUrl)) return _api.HttpBaseUrl!;

            // Fallback: nested under KeyCard:Api
            var baseUrlNested = _config["KeyCard:Api:BaseUrl"];
            var httpsNested = _config["KeyCard:Api:HttpsBaseUrl"];
            var httpNested = _config["KeyCard:Api:HttpBaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrlNested)) return baseUrlNested!;
            if (!string.IsNullOrWhiteSpace(httpsNested)) return httpsNested!;
            if (!string.IsNullOrWhiteSpace(httpNested)) return httpNested!;

            // Fallback: root "Api" direct from raw config (covers cases where options weren't bound)
            var baseUrlRoot = _config["Api:BaseUrl"];
            var httpsRoot = _config["Api:HttpsBaseUrl"];
            var httpRoot = _config["Api:HttpBaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrlRoot)) return baseUrlRoot!;
            if (!string.IsNullOrWhiteSpace(httpsRoot)) return httpsRoot!;
            if (!string.IsNullOrWhiteSpace(httpRoot)) return httpRoot!;

            // Safe local default
            return "http://localhost:5001";
        }

        private string ResolveBookingsHubUrl()
        {
            // Preferred: typed SignalR options (root)
            if (!string.IsNullOrWhiteSpace(_signalR.BookingsHubUrl))
                return _signalR.BookingsHubUrl!;

            // Fallback: nested under KeyCard:SignalR
            var nested = _config["KeyCard:SignalR:BookingsHubUrl"];
            if (!string.IsNullOrWhiteSpace(nested)) return nested!;

            // Fallback: root via raw config
            var root = _config["SignalR:BookingsHubUrl"];
            if (!string.IsNullOrWhiteSpace(root)) return root!;

            // Build from API base
            return CombineUri(ApiBaseUrl, "hubs/bookings");
        }

        private static bool EqualsIgnoreCase(string? a, string? b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static string CombineUri(string baseUrl, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return baseUrl;
            if (!baseUrl.EndsWith('/')) baseUrl += '/';
            return baseUrl + path.TrimStart('/');
        }
    }
}
