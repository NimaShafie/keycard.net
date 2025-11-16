// Services/IBookingStateService.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Services
{
    /// <summary>
    /// Singleton service that maintains the shared state of all bookings.
    /// All ViewModels reference the same collections, enabling real-time updates across views.
    /// </summary>
    public interface IBookingStateService : INotifyPropertyChanged
    {
        /// <summary>Single source of truth for all bookings</summary>
        ObservableCollection<Booking> AllBookings { get; }

        /// <summary>Today's arrivals (filtered view)</summary>
        ObservableCollection<Booking> TodayArrivals { get; }

        /// <summary>Load bookings from API</summary>
        Task RefreshAsync(CancellationToken ct = default);

        /// <summary>Update a booking's status</summary>
        void UpdateBookingStatus(string bookingId, string newStatus);

        /// <summary>Check in a guest</summary>
        Task<bool> CheckInAsync(string bookingCode, CancellationToken ct = default);

        /// <summary>Assign a room to a booking</summary>
        Task<bool> AssignRoomAsync(string bookingCode, int roomNumber, CancellationToken ct = default);

        /// <summary>Find a booking by confirmation code</summary>
        Booking? FindByCode(string code);
    }
}
