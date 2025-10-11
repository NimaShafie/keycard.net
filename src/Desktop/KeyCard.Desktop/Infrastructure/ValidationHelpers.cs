// Infrastructure/ValidationHelpers.cs
using System;
using System.Text.RegularExpressions;

namespace KeyCard.Desktop.Infrastructure
{
    public static class ValidationHelpers
    {
        /// <summary>Validates a room number (1-9999).</summary>
        public static bool IsValidRoomNumber(string? input, out int roomNumber)
        {
            roomNumber = 0;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (!int.TryParse(input.Trim(), out roomNumber))
                return false;

            return roomNumber is >= 1 and <= 9999;
        }

        /// <summary>Validates a confirmation code format.</summary>
        public static bool IsValidConfirmationCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            // Typical format: alphanumeric, 4-12 characters
            return Regex.IsMatch(code.Trim(), @"^[A-Z0-9]{4,12}$", RegexOptions.IgnoreCase);
        }

        /// <summary>Validates an email address.</summary>
        public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Validates a phone number (basic format check).</summary>
        public static bool IsValidPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // Remove common separators
            var cleaned = Regex.Replace(phone, @"[\s\-\(\)\.]", "");

            // Must be 10-15 digits
            return Regex.IsMatch(cleaned, @"^\d{10,15}$");
        }

        /// <summary>Sanitizes user input to prevent injection attacks.</summary>
        public static string SanitizeInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input.Trim()
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        /// <summary>Checks if a date range is valid.</summary>
        public static bool IsValidDateRange(DateOnly checkIn, DateOnly checkOut)
        {
            return checkOut > checkIn;
        }

        /// <summary>Checks if a date is in the past.</summary>
        public static bool IsInPast(DateOnly date)
        {
            return date < DateOnly.FromDateTime(DateTime.Today);
        }

        /// <summary>Checks if a date is today or in the future.</summary>
        public static bool IsTodayOrFuture(DateOnly date)
        {
            return date >= DateOnly.FromDateTime(DateTime.Today);
        }
    }
}
