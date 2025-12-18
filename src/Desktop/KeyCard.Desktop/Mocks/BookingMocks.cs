// Mocks/BookingMocks.cs
using System;
using System.Collections.Generic;

using KeyCard.Desktop.Models;

namespace KeyCard.Desktop.Mocks
{
    public static class BookingMocks
    {
        private static int _idCounter = 1000;

        public static List<Booking> GetArrivalsToday(int count = 8) => Generate(count, false);
        public static List<Booking> GetDeparturesToday(int count = 4) => Generate(count, true);

        private static List<Booking> Generate(int count, bool isDeparture)
        {
            var list = new List<Booking>(count);
            var today = DateTime.Today;

            for (int i = 1; i <= count; i++)
            {
                var id = _idCounter++;
                var guest = isDeparture ? $"Departing Guest {i}" : $"Guest Name {i}";
                var room = (200 + i).ToString();
                var checkIn = isDeparture ? today.AddDays(-3) : today;
                var checkOut = isDeparture ? today : today.AddDays(2);
                var code = $"MOCK{id:D4}";

                list.Add(new Booking
                {
                    Id = id,
                    ConfirmationCode = code,
                    GuestName = guest,
                    RoomNumber = room,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    StatusEnum = BookingStatus.Reserved
                });
            }
            return list;
        }
    }
}
