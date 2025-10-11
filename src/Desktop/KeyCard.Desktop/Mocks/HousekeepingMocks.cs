// Mocks/HousekeepingMocks.cs
using System;
using System.Collections.Generic;

namespace KeyCard.Desktop.Mocks
{
    public static class HousekeepingMocks
    {
        public static List<string> GetRooms()
        {
            // A few rooms across floors with statuses
            return new List<string>
            {
                "201 • Vacant • Needs Clean",
                "202 • Occupied • DND",
                "203 • Vacant • Ready",
                "304 • Occupied • Service Requested",
                "402 • Vacant • Needs Clean",
                "405 • Vacant • Inspection",
                "506 • Occupied • Late Checkout"
            };
        }

        public static List<string> GetTasks()
        {
            return new List<string>
            {
                "Room 201 • Full Clean",
                "Room 304 • Replace Towels",
                "Room 402 • Make Bed",
                "Room 405 • Inspector Visit 2 PM",
                "Public Area • Lobby Vacuum"
            };
        }
    }
}
