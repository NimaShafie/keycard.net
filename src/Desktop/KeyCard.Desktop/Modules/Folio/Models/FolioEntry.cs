// /Modules/Folio/Models/FolioEntry.cs

using System;

namespace KeyCard.Desktop.Modules.Folio.Models
{
    public enum FolioEntryType
    {
        Charge = 1,
        Payment = 2
    }

    public enum PaymentMethod
    {
        None = 0,
        Cash = 1,
        Card = 2,
        External = 3
    }

    public sealed class FolioEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

        public FolioEntryType Type { get; set; }

        // Common
        public string Description { get; set; } = string.Empty;

        // For charges (+), payments (-)
        public decimal Amount { get; set; }

        // Charges: tax breakdown (computed), Payments: 0
        public decimal TaxAmount { get; set; }
        public decimal NetAmount => Type == FolioEntryType.Charge ? Amount + TaxAmount : -Amount;

        // Payment only
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.None;
        public string? PaymentRef { get; set; }
    }
}
