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

            // DIAGNOSTIC LOGGING - helps debug mode issues
#if DEBUG
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("╔══════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine("║       AppEnvironment Initialized             ║");
            System.Diagnostics.Debug.WriteLine("╚══════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"Environment: {_hostEnv.EnvironmentName}");
            System.Diagnostics.Debug.WriteLine($"IsMock: {_isMock}");
            System.Diagnostics.Debug.WriteLine($"ApiBaseUrl: {_apiBaseUrl}");
            System.Diagnostics.Debug.WriteLine($"BookingsHubUrl: {_bookingsHubUrl}");
            System.Diagnostics.Debug.WriteLine("");
#endif
        }

        public string EnvironmentName => _hostEnv.EnvironmentName;
        public bool IsMock => _isMock;
        public bool IsLive => !_isMock;
        public string ApiBaseUrl => _apiBaseUrl;
        public string BookingsHubUrl => _bookingsHubUrl;

        private bool EvaluateIsMock()
        {
            // Check KEYCARD_MODE environment variable FIRST
            // This is set by launchSettings.json and takes highest priority
            var keycardModeEnv = Environment.GetEnvironmentVariable("KEYCARD_MODE");
            if (!string.IsNullOrWhiteSpace(keycardModeEnv))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"KEYCARD_MODE env var: {keycardModeEnv}");
#endif

                if (EqualsIgnoreCase(keycardModeEnv, "Mock")) return true;
                if (EqualsIgnoreCase(keycardModeEnv, "Live")) return false;
            }

            // -------- 1) Check UseMocks from options (second priority) --------
            if (_keyCard.UseMocks)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("UseMocks=true from KeyCardOptions");
#endif
                return true;
            }

            // -------- 2) Check Mode from options --------
            if (_keyCard.Mode != null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Mode={_keyCard.Mode} from KeyCardOptions");
#endif

                if (EqualsIgnoreCase(_keyCard.Mode, "Mock")) return true;
                if (EqualsIgnoreCase(_keyCard.Mode, "Live")) return false;
            }

            // -------- 3) Fallbacks from raw config (covers env vars & unbound shapes) --------
            // Nested: KeyCard:UseMocks / KeyCard:Mode
            var useNested = _config["KeyCard:UseMocks"];
            var modeNested = _config["KeyCard:Mode"];
            if (bool.TryParse(useNested, out var nestedFlag))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"KeyCard:UseMocks={nestedFlag} from config");
#endif
                return nestedFlag;
            }
            if (EqualsIgnoreCase(modeNested, "Mock"))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("KeyCard:Mode=Mock from config");
#endif
                return true;
            }
            if (EqualsIgnoreCase(modeNested, "Live"))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("KeyCard:Mode=Live from config");
#endif
                return false;
            }

            // Root: UseMocks / Mode (if someone put them at the root)
            var useRoot = _config["UseMocks"];
            var modeRoot = _config["Mode"];
            if (bool.TryParse(useRoot, out var rootFlag))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"UseMocks={rootFlag} from root config");
#endif
                return rootFlag;
            }
            if (EqualsIgnoreCase(modeRoot, "Mock"))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Mode=Mock from root config");
#endif
                return true;
            }
            if (EqualsIgnoreCase(modeRoot, "Live"))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Mode=Live from root config");
#endif
                return false;
            }

            // -------- 4) ⚠️ CHANGED: Default to LIVE instead of Mock --------
            // This prevents the "stuck in mock mode" issue
            // If you're in Development but didn't explicitly set Mock, assume Live
#if DEBUG
            System.Diagnostics.Debug.WriteLine("No mode specified - defaulting to LIVE");
#endif

            return false;  // Changed from: if (_hostEnv.IsDevelopment()) return true;
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
