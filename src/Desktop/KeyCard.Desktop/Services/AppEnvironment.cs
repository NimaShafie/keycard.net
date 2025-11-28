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
        private readonly ApiOptions _api;             // bound from "Api"
        private readonly SignalROptions _signalR;     // bound from "SignalR"
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
            // -------- 1) Check UseMocks FIRST (highest priority from options) --------
            if (_keyCard.UseMocks) return true;

            // -------- 2) Check Mode from options --------
            if (_keyCard.Mode != null)
            {
                if (EqualsIgnoreCase(_keyCard.Mode, "Mock")) return true;
                if (EqualsIgnoreCase(_keyCard.Mode, "Live")) return false;
            }

            // -------- 3) Fallbacks from raw config (covers env vars & unbound shapes) --------
            // Nested: KeyCard:UseMocks / KeyCard:Mode
            var useNested = _config["KeyCard:UseMocks"];
            var modeNested = _config["KeyCard:Mode"];
            if (bool.TryParse(useNested, out var nestedFlag)) return nestedFlag;
            if (EqualsIgnoreCase(modeNested, "Mock")) return true;
            if (EqualsIgnoreCase(modeNested, "Live")) return false;

            // Root: UseMocks / Mode (if someone put them at the root)
            var useRoot = _config["UseMocks"];
            var modeRoot = _config["Mode"];
            if (bool.TryParse(useRoot, out var rootFlag)) return rootFlag;
            if (EqualsIgnoreCase(modeRoot, "Mock")) return true;
            if (EqualsIgnoreCase(modeRoot, "Live")) return false;

            // -------- 4) Sensible default: Development => Mock --------
            // If nothing explicitly opted in or out, default to mocks in dev.
            if (_hostEnv.IsDevelopment()) return true;

            // -------- 5) Otherwise assume Live --------
            return false;
        }

        private string ResolveApiBaseUrl()
        {
            // Preferred: typed options bound from root "Api"
            if (!string.IsNullOrWhiteSpace(_api.BaseUrl)) return _api.BaseUrl!;
            if (!string.IsNullOrWhiteSpace(_api.HttpsBaseUrl)) return _api.HttpsBaseUrl!;
            if (!string.IsNullOrWhiteSpace(_api.HttpBaseUrl)) return _api.HttpBaseUrl!;

            // Fallbacks: nested KeyCard:Api
            var baseUrlNested = _config["KeyCard:Api:BaseUrl"];
            var httpsNested = _config["KeyCard:Api:HttpsBaseUrl"];
            var httpNested = _config["KeyCard:Api:HttpBaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrlNested)) return baseUrlNested!;
            if (!string.IsNullOrWhiteSpace(httpsNested)) return httpsNested!;
            if (!string.IsNullOrWhiteSpace(httpNested)) return httpNested!;

            // Fallbacks: raw root "Api"
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

            // Build from API base as a last resort
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
