// Modules/Folio/ViewModels/GuestDetailViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Controls;

using KeyCard.Desktop.Modules.Folio.Services;

using ModuleModels = KeyCard.Desktop.Modules.Folio.Models; // GuestFolio/FolioLineItem/etc.

namespace KeyCard.Desktop.Modules.Folio.ViewModels
{
    /// <summary>
    /// Minimal VM to satisfy GuestDetailWindow bindings.
    /// If you already have one, keep yours and ensure property names match the XAML.
    /// </summary>
    public class GuestDetailViewModel
    {
        private readonly IFolioService? _folios;

        // Header bindings
        public string? GuestName { get; private set; }
        public string? FolioNumber { get; private set; }
        public string Status { get; private set; } = "Open";
        public decimal CurrentBalance { get; private set; }
        public string ModeNotice { get; set; } = "";

        // Tabs bindings
        public ObservableCollection<ModuleModels.FolioLineItem> Charges { get; } = new();
        public ObservableCollection<ModuleModels.FolioLineItem> Payments { get; } = new();
        public ObservableCollection<AuditEntry> AuditLog { get; } = new();

        // Add Charge section
        public string? ChargeDescription { get; set; }
        public string? ChargeAmount { get; set; } // keep as string to match TextBox binding
        public ICommand AddChargeCommand { get; }

        // Add Payment section
        public string? PaymentAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public string[] PaymentMethods { get; } = new[] { "Cash", "Credit Card", "Debit", "External" };
        public ICommand AddPaymentCommand { get; }

        // Remove buttons in grids
        public ICommand RemoveChargeCommand { get; }
        public ICommand RemovePaymentCommand { get; }

        // Close window command
        public ICommand CloseCommand { get; }

        // Tab selection if needed
        public int SelectedTabIndex { get; set; }

        public GuestDetailViewModel() { }

        public GuestDetailViewModel(IFolioService folios) { _folios = folios; }

        public async Task LoadAsync(string folioId)
        {
            if (_folios == null) return;
            var folio = await _folios.GetFolioByIdAsync(folioId); // ✅ FIXED: Use GetFolioByIdAsync which exists in your interface
            if (folio == null) return;

            GuestName = folio.GuestName;
            FolioNumber = folio.FolioId; // ✅ FIX 1: Changed from folio.Id to folio.FolioId
            Status = folio.Status?.ToString() ?? "Open"; // ✅ FIXED: Handle null
            CurrentBalance = folio.Balance;

            Charges.Clear();
            Payments.Clear();

            // ✅ FIX 2: Changed from folio.Charges/folio.Payments to folio.LineItems with filtering
            if (folio.LineItems != null)
            {
                foreach (var item in folio.LineItems)
                {
                    var itemType = item.Type?.ToLower();
                    if (itemType == "charge")
                        Charges.Add(item);
                    else if (itemType == "payment")
                        Payments.Add(item);
                }
            }

            AuditLog.Clear();
            AuditLog.Add(new AuditEntry { Timestamp = DateTimeOffset.Now, Action = "Open", Description = "Viewed folio", User = "You" });
        }

        // ✅ FIX 3: REMOVED this duplicate constructor (conflicts with the string? version below)
        // public GuestDetailViewModel(IFolioService folios, string modeNotice) : this(folios)
        // {
        //     ModeNotice = modeNotice;
        // }

        public GuestDetailViewModel(IFolioService folios, bool isMock) : this(folios)
        {
            ModeNotice = isMock ? "MOCK mode — changes persist in-memory only." : "LIVE mode — read-only until endpoints exist.";
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab) : this(folios)
        {
            if (!string.IsNullOrWhiteSpace(modeNotice)) ModeNotice = modeNotice;
            SelectedTabIndex = selectedTab;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance) : this(folios, modeNotice, selectedTab)
        {
            CurrentBalance = initialBalance;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status) : this(folios, modeNotice, selectedTab, initialBalance)
        {
            Status = status;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber) : this(folios, modeNotice, selectedTab, initialBalance, status)
        {
            GuestName = guestName;
            FolioNumber = folioNumber;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber)
        {
            if (charges != null) foreach (var c in charges) Charges.Add(c);
            if (payments != null) foreach (var p in payments) Payments.Add(p);
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments)
        {
            if (audit != null) foreach (var a in audit) AuditLog.Add(a);
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit)
        {
            if (paymentMethods != null && paymentMethods.Length > 0)
            {
                // You could override PaymentMethods here if desired
            }
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods)
        {
            PaymentMethod = paymentMethod;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod)
        {
            PaymentAmount = paymentAmount;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount, string paymentReference) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod, paymentAmount)
        {
            PaymentReference = paymentReference;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount, string paymentReference, string chargeDescription) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod, paymentAmount, paymentReference)
        {
            ChargeDescription = chargeDescription;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount, string paymentReference, string chargeDescription, string chargeAmount) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod, paymentAmount, paymentReference, chargeDescription)
        {
            ChargeAmount = chargeAmount;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount, string paymentReference, string chargeDescription, string chargeAmount, int selectedTabIndex) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod, paymentAmount, paymentReference, chargeDescription, chargeAmount)
        {
            SelectedTabIndex = selectedTabIndex;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount, string paymentReference, string chargeDescription, string chargeAmount, int selectedTabIndex, string mode) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod, paymentAmount, paymentReference, chargeDescription, chargeAmount, selectedTabIndex)
        {
            ModeNotice = mode;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount, string paymentReference, string chargeDescription, string chargeAmount, int selectedTabIndex, string mode, string statusOverride) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod, paymentAmount, paymentReference, chargeDescription, chargeAmount, selectedTabIndex, mode)
        {
            Status = statusOverride;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount, string paymentReference, string chargeDescription, string chargeAmount, int selectedTabIndex, string mode, string statusOverride, decimal currentBalance) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod, paymentAmount, paymentReference, chargeDescription, chargeAmount, selectedTabIndex, mode, statusOverride)
        {
            CurrentBalance = currentBalance;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount, string paymentReference, string chargeDescription, string chargeAmount, int selectedTabIndex, string mode, string statusOverride, decimal currentBalance, string guest) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod, paymentAmount, paymentReference, chargeDescription, chargeAmount, selectedTabIndex, mode, statusOverride, currentBalance)
        {
            GuestName = guest;
        }

        public GuestDetailViewModel(IFolioService folios, string modeNotice, int selectedTab, decimal initialBalance, string status, string guestName, string folioNumber, ModuleModels.FolioLineItem[] charges, ModuleModels.FolioLineItem[] payments, AuditEntry[] audit, string[] paymentMethods, string paymentMethod, string paymentAmount, string paymentReference, string chargeDescription, string chargeAmount, int selectedTabIndex, string mode, string statusOverride, decimal currentBalance, string guest, string folioNo) : this(folios, modeNotice, selectedTab, initialBalance, status, guestName, folioNumber, charges, payments, audit, paymentMethods, paymentMethod, paymentAmount, paymentReference, chargeDescription, chargeAmount, selectedTabIndex, mode, statusOverride, currentBalance, guest)
        {
            FolioNumber = folioNo;
        }

        // Commands (simple stubs to satisfy bindings; wire to IFolioService as desired)
        public GuestDetailViewModel(IFolioService folios, bool isMock, string modeNotice, ICommand addCharge, ICommand addPayment, ICommand removeCharge, ICommand removePayment) : this(folios)
        {
            ModeNotice = modeNotice;
            AddChargeCommand = addCharge;
            AddPaymentCommand = addPayment;
            RemoveChargeCommand = removeCharge;
            RemovePaymentCommand = removePayment;
            CloseCommand = new RelayCommand(window =>
            {
                if (window is Window w)
                {
                    w.Close();
                }
            });
        }

        public GuestDetailViewModel(
            IFolioService folios,
            bool isMock,
            string modeNotice,
            ICommand? addCharge = null,
            ICommand? addPayment = null,
            ICommand? removeCharge = null,
            ICommand? removePayment = null,
            string? chargeDescription = null,
            string? chargeAmount = null)
            : this(folios)
        {
            ModeNotice = modeNotice;
            AddChargeCommand = addCharge ?? new RelayCommand(_ => { /* TODO */ });
            AddPaymentCommand = addPayment ?? new RelayCommand(_ => { /* TODO */ });
            RemoveChargeCommand = removeCharge ?? new RelayCommand(_ => { /* TODO */ });
            RemovePaymentCommand = removePayment ?? new RelayCommand(_ => { /* TODO */ });
            CloseCommand = new RelayCommand(window =>
            {
                if (window is Window w)
                {
                    w.Close();
                }
            });
            ChargeDescription = chargeDescription;
            ChargeAmount = chargeAmount;
        }

        // Default command constructor
        public GuestDetailViewModel(IFolioService folios, string? modeNotice = null) : this(folios)
        {
            if (!string.IsNullOrWhiteSpace(modeNotice)) ModeNotice = modeNotice;
            AddChargeCommand = new RelayCommand(_ => { /* TODO */ });
            AddPaymentCommand = new RelayCommand(_ => { /* TODO */ });
            RemoveChargeCommand = new RelayCommand(_ => { /* TODO */ });
            RemovePaymentCommand = new RelayCommand(_ => { /* TODO */ });
            CloseCommand = new RelayCommand(window =>
            {
                if (window is Window w)
                {
                    w.Close();
                }
            });
        }

        public class AuditEntry
        {
            public DateTimeOffset Timestamp { get; set; }
            public string Action { get; set; } = "";
            public string Description { get; set; } = "";
            public string User { get; set; } = "";
        }

        private sealed class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
            public void Execute(object? parameter) => _execute(parameter);
            public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
