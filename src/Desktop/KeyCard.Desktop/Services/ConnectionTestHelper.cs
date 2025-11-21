// Services/ConnectionTestHelper.cs
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace KeyCard.Desktop.Services
{
    /// <summary>
    /// Helper to test backend connectivity and diagnose connection issues.
    /// </summary>
    public static class ConnectionTestHelper
    {
        /// <summary>
        /// Test if the backend is reachable.
        /// </summary>
        public static async Task<(bool success, string message)> TestConnectionAsync(
            string baseUrl,
            ILogger? logger = null,
            CancellationToken ct = default)
        {
            try
            {
                logger?.LogInformation("Testing connection to {BaseUrl}", baseUrl);

                using var handler = new HttpClientHandler
                {
                    // Accept self-signed certificates for development
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                client.DefaultRequestHeaders.Add("User-Agent", "KeyCard-ConnectionTest/1.0");

                // Try to hit a health endpoint or any simple endpoint
                var testUrls = new[]
                {
                    "api/v1/Health",
                    "api/Health",
                    "health",
                    "" // Root endpoint
                };

                foreach (var url in testUrls)
                {
                    try
                    {
                        logger?.LogDebug("Trying endpoint: {Url}", url);
                        var response = await client.GetAsync(url, ct);

                        logger?.LogInformation(
                            "Response from {Url}: {StatusCode}",
                            url,
                            (int)response.StatusCode);

                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync(ct);
                            return (true, $"Connected successfully to {baseUrl}/{url}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogDebug(ex, "Failed to connect to {Url}", url);
                    }
                }

                return (false, $"Backend is reachable at {baseUrl} but no valid endpoints found");
            }
            catch (HttpRequestException ex)
            {
                logger?.LogError(ex, "HTTP request failed");
                return (false, $"Connection failed: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                logger?.LogError(ex, "Request timed out");
                return (false, "Connection timed out. Is the backend running?");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Unexpected error testing connection");
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Get detailed diagnostic information.
        /// </summary>
        public static string GetDiagnosticInfo(string baseUrl)
        {
            return $@"
=== Connection Diagnostics ===
Target URL: {baseUrl}
Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

Checklist:
□ Backend is running (check console for 'Now listening on:')
□ Backend is listening on correct port
□ No firewall blocking localhost connections
□ SSL certificate trusted (for HTTPS)
□ Desktop app has correct base URL in config

Expected backend URLs:
  - https://localhost:7224 (HTTPS - preferred)
  - http://localhost:5149 (HTTP - fallback)

Common Issues:
1. Backend not started → Start backend project
2. Port mismatch → Check appsettings.json matches backend
3. SSL error → Trust dev certificate: dotnet dev-certs https --trust
4. Firewall → Allow localhost traffic
";
        }
    }
}
