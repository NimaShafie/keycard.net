// Options/ApiOptions.cs
namespace KeyCard.Desktop.Options
{
    public sealed class ApiOptions
    {
        public string BaseUrl { get; set; } = "https://localhost:5149"; // update if your port differs
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(20);
    }
}
