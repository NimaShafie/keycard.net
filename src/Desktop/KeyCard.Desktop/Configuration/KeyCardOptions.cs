// Configuration/KeyCardOptions.cs
namespace KeyCard.Desktop.Configuration
{
    public sealed class KeyCardOptions
    {
        public string Mode { get; set; } = "Live";
        public bool UseMocks { get; set; } // default(false) – no explicit initializer to satisfy CA1805
    }

    public sealed class ApiOptions
    {
        public string? BaseUrl { get; set; }
        public string? HttpsBaseUrl { get; set; }
        public string? HttpBaseUrl { get; set; }
    }

    public sealed class SignalROptions
    {
        public string? BookingsHubUrl { get; set; }
    }
}
