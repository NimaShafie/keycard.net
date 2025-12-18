// Services/BookingStateService.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

using Microsoft.Extensions.Logging;

namespace KeyCard.Desktop.Services
{
    public sealed class BookingStateService : IBookingStateService
    {
        private readonly IBookingService _bookingApi;
        private readonly ILogger<BookingStateService> _logger;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BookingStateService(
            IBookingService bookingApi,
            ILogger<BookingStateService> logger)
        {
            _bookingApi = bookingApi ?? throw new ArgumentNullException(nameof(bookingApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Master collection - all ViewModels should bind to this</summary>
        public ObservableCollection<Booking> AllBookings { get; } = new();

        /// <summary>Filtered view of today's arrivals</summary>
        public ObservableCollection<Booking> TodayArrivals { get; } = new();

        public async Task RefreshAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Refreshing bookings from API...");

                var bookings = await _bookingApi.ListAsync(ct);

                AllBookings.Clear();
                foreach (var booking in bookings)
                {
                    AllBookings.Add(booking);
                }

                RefreshTodayArrivals();

                _logger.LogInformation("Loaded {Count} bookings", bookings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh bookings");
                throw;
            }
        }

        public void UpdateBookingStatus(string bookingId, string newStatus)
        {
            var booking = AllBookings.FirstOrDefault(b =>
                b.Id.ToString() == bookingId ||
                b.ConfirmationCode == bookingId);

            if (booking != null)
            {
                // Parse string status to enum
                if (!Enum.TryParse<BookingStatus>(newStatus, ignoreCase: true, out var statusEnum))
                {
                    statusEnum = BookingStatus.Reserved;
                }

                // Since Booking is a record with init-only properties, create a new instance
                var updated = booking with { StatusEnum = statusEnum };

                var index = AllBookings.IndexOf(booking);
                if (index >= 0)
                {
                    // ✅ CRITICAL: Remove and Insert instead of direct assignment
                    // This triggers proper CollectionChanged notifications for DataGrid
                    AllBookings.RemoveAt(index);
                    AllBookings.Insert(index, updated);

                    _logger.LogInformation("Updated booking {BookingId} status to {Status}", bookingId, newStatus);

                    // Refresh filtered views
                    RefreshTodayArrivals();
                }
            }
        }

        public async Task<bool> CheckInAsync(string bookingCode, CancellationToken ct = default)
        {
            try
            {
                var success = await _bookingApi.CheckInAsync(bookingCode, ct);

                if (success)
                {
                    UpdateBookingStatus(bookingCode, "CheckedIn");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check in booking {Code}", bookingCode);
                return false;
            }
        }

        public async Task<bool> AssignRoomAsync(string bookingCode, int roomNumber, CancellationToken ct = default)
        {
            try
            {
                var success = await _bookingApi.AssignRoomAsync(bookingCode, roomNumber, ct);

                if (success)
                {
                    var booking = FindByCode(bookingCode);
                    if (booking != null)
                    {
                        // Since Booking is a record with init-only properties, create a new instance
                        var updated = booking with { RoomNumber = roomNumber.ToString() };

                        var index = AllBookings.IndexOf(booking);
                        if (index >= 0)
                        {
                            // ✅ CRITICAL: Remove and Insert instead of direct assignment
                            AllBookings.RemoveAt(index);
                            AllBookings.Insert(index, updated);

                            RefreshTodayArrivals();
                            _logger.LogInformation("Assigned room {Room} to booking {Code}", roomNumber, bookingCode);
                        }
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to assign room to booking {Code}", bookingCode);
                return false;
            }
        }

        public Booking? FindByCode(string code)
        {
            return AllBookings.FirstOrDefault(b =>
                b.ConfirmationCode == code ||
                b.Id.ToString() == code);
        }

        private void RefreshTodayArrivals()
        {
            var today = DateTime.Today.Date;

            TodayArrivals.Clear();
            foreach (var booking in AllBookings.Where(b => b.CheckInDate.Date == today))
            {
                TodayArrivals.Add(booking);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
