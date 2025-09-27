// Models/Booking.cs
using System;
namespace KeyCard.Desktop.Models;

public record Booking(string BookingId, string GuestName, int RoomNumber, DateTime CheckIn, DateTime CheckOut);
