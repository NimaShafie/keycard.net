// Services/IBookingService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using KeyCard.Desktop.Models;
namespace KeyCard.Desktop.Services;

public interface IBookingService
{
    Task<IReadOnlyList<Booking>> GetTodayArrivalsAsync();
    Task<Booking?> FindBookingByCodeAsync(string query);
    Task<bool> AssignRoomAsync(string bookingId, int roomNumber);
    Task<bool> CheckInAsync(string bookingId);
}
