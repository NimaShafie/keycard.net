namespace KeyCard.Desktop;

public sealed class KeyCardOptions
{
    public string Mode { get; set; } = "Mock"; // "Mock" or "Live"
    public ApiOptions Api { get; set; } = new();
}

public sealed class ApiOptions
{
    public string HttpsBaseUrl { get; set; } = "https://localhost:7224";
    public string HttpBaseUrl { get; set; } = "http://localhost:5149";
}
