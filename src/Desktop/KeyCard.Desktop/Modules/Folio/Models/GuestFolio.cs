// Modules/Folio/Models/GuestFolio.cs
using System;
using System.Collections.Generic;

namespace KeyCard.Desktop.Modules.Folio.Models
{
    /// <summary>
    /// Represents a guest's folio (account) with charges and payments.
    /// </summary>
    public class GuestFolio
    {
        public string FolioId { get; set; } = string.Empty;
        public string BookingId { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public int RoomNumber { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }

        public decimal TotalCharges { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal Balance => TotalCharges - TotalPayments;

        public List<FolioLineItem> LineItems { get; set; } = new();

        public string Status { get; set; } = "Open"; // Open, Closed, Settled
    }

    /// <summary>
    /// Represents a single line item (charge or payment) on a folio.
    /// </summary>
    public class FolioLineItem
    {
        public string Id { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; } = string.Empty; // Charge, Payment, Tax, Fee
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }
}
