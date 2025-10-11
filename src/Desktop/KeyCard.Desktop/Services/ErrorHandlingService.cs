// Services/ErrorHandlingService.cs
using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace KeyCard.Desktop.Services
{
    public interface IErrorHandlingService
    {
        void HandleError(Exception ex, string context = "");
        Task HandleErrorAsync(Exception ex, string context = "");
        string GetUserFriendlyMessage(Exception ex);
    }

    public sealed partial class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger<ErrorHandlingService> _logger;

        public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void HandleError(Exception ex, string context = "")
        {
            LogError(_logger, ex, context);

            // In a real app, you might show a notification or dialog
            System.Diagnostics.Debug.WriteLine($"[ERROR] {context}: {ex.Message}");
        }

        public Task HandleErrorAsync(Exception ex, string context = "")
        {
            HandleError(ex, context);
            return Task.CompletedTask;
        }

        public string GetUserFriendlyMessage(Exception ex) => ex switch
        {
            ArgumentNullException => "A required value was missing.",
            ArgumentException => "Invalid input provided.",
            InvalidOperationException => "This operation cannot be performed right now.",
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            System.Net.Http.HttpRequestException => "Unable to connect to the server. Please check your connection.",
            TimeoutException => "The operation took too long to complete.",
            OperationCanceledException => "Operation was cancelled.",
            _ => "An unexpected error occurred. Please try again."
        };

        // Use LoggerMessage source generator for better performance
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Error,
            Message = "Error in {Context}")]
        private static partial void LogError(
            ILogger logger,
            Exception ex,
            string context);
    }
}
