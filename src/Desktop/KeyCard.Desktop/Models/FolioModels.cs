// Models/FolioModels.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KeyCard.Desktop.Models
{
    public class GuestFolio : INotifyPropertyChanged
    {
        private decimal _balance;

        public string FolioNumber { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";

        public decimal Balance
        {
            get => _balance;
            set
            {
                _balance = value;
                OnPropertyChanged();
            }
        }

        public List<FolioCharge> Charges { get; set; } = new();
        public List<FolioPayment> Payments { get; set; } = new();
        public List<FolioAuditEntry> AuditLog { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class FolioCharge
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class FolioPayment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public decimal Amount { get; set; }
        public string Method { get; set; } = "Cash";
        public string? Reference { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class FolioAuditEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string User { get; set; } = "System";
    }
}
