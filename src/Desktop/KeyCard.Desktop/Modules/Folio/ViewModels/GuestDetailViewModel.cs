// Modules/Folio/ViewModels/GuestDetailViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public class GuestDetailViewModel : INotifyPropertyChanged
    {
        private readonly IFolioService? _folios;
        private string? _currentFolioId;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Event to notify parent when data changes
        public event EventHandler? DataChanged;

        // Header bindings - WITH PROPERTY CHANGE NOTIFICATIONS
        private string? _guestName;
        public string? GuestName
        {
            get => _guestName;
            private set
            {
                if (_guestName != value)
                {
                    _guestName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _folioNumber;
        public string? FolioNumber
        {
            get => _folioNumber;
            private set
            {
                if (_folioNumber != value)
                {
                    _folioNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _status = "Open";
        public string Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        private decimal _currentBalance;
        public decimal CurrentBalance
        {
            get => _currentBalance;
            private set
            {
                if (_currentBalance != value)
                {
                    _currentBalance = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ModeNotice { get; set; } = "";

        // Tabs bindings
        public ObservableCollection<ModuleModels.FolioLineItem> Charges { get; } = new();
        public ObservableCollection<ModuleModels.FolioLineItem> Payments { get; } = new();
        public ObservableCollection<AuditEntry> AuditLog { get; } = new();

        // Add Charge section - WITH PROPERTY CHANGE NOTIFICATIONS
        private string? _chargeDescription;
        public string? ChargeDescription
        {
            get => _chargeDescription;
            set
            {
                if (_chargeDescription != value)
                {
                    _chargeDescription = value;
                    OnPropertyChanged();
                    RaiseCanExecuteChanged();
                }
            }
        }

        private string? _chargeAmount;
        public string? ChargeAmount
        {
            get => _chargeAmount;
            set
            {
                if (_chargeAmount != value)
                {
                    _chargeAmount = value;
                    OnPropertyChanged();
                    RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand AddChargeCommand { get; }

        // Add Payment section - WITH PROPERTY CHANGE NOTIFICATIONS
        private string? _paymentAmount;
        public string? PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                if (_paymentAmount != value)
                {
                    _paymentAmount = value;
                    OnPropertyChanged();
                    RaiseCanExecuteChanged();
                }
            }
        }

        private string? _paymentMethod;
        public string? PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (_paymentMethod != value)
                {
                    _paymentMethod = value;
                    OnPropertyChanged();
                    RaiseCanExecuteChanged();
                }
            }
        }

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

        public GuestDetailViewModel()
        {
            AddChargeCommand = new RelayCommand(async _ => await AddChargeAsync(), _ => CanAddCharge());
            AddPaymentCommand = new RelayCommand(async _ => await AddPaymentAsync(), _ => CanAddPayment());

            // Hook up remove commands with proper can-execute
            RemoveChargeCommand = new RelayCommand(
                async param => await RemoveChargeAsync(param as string),
                param => CanRemoveCharge(param));

            RemovePaymentCommand = new RelayCommand(
                async param => await RemovePaymentAsync(param as string),
                param => CanRemovePayment(param));

            CloseCommand = new RelayCommand(window =>
            {
                if (window is Window w)
                {
                    w.Close();
                }
            });
        }

        public GuestDetailViewModel(IFolioService folios) : this()
        {
            _folios = folios;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaiseCanExecuteChanged()
        {
            (AddChargeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (AddPaymentCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemoveChargeCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RemovePaymentCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Load folio data and populate charges/payments.
        /// </summary>
        public async Task LoadAsync(string folioId)
        {
            if (_folios == null || string.IsNullOrWhiteSpace(folioId))
            {
                Console.WriteLine($"[GuestDetail] Cannot load: service={_folios != null}, folioId={folioId}");
                return;
            }

            _currentFolioId = folioId;

            try
            {
                Console.WriteLine($"[GuestDetail] Loading folio {folioId}...");

                var folio = await _folios.GetFolioByIdAsync(folioId);

                if (folio == null)
                {
                    Console.WriteLine($"[GuestDetail] Folio {folioId} not found");
                    return;
                }

                Console.WriteLine($"[GuestDetail] Loaded folio: {folio.GuestName}, Balance: {folio.Balance:C}");

                GuestName = folio.GuestName;
                FolioNumber = folio.FolioId;
                Status = folio.Status ?? "Open";
                CurrentBalance = folio.Balance;

                Charges.Clear();
                Payments.Clear();

                if (folio.LineItems != null)
                {
                    Console.WriteLine($"[GuestDetail] Processing {folio.LineItems.Count} line items");

                    foreach (var item in folio.LineItems)
                    {
                        var itemType = item.Type?.ToLowerInvariant();
                        if (itemType == "charge")
                        {
                            Charges.Add(item);
                            Console.WriteLine($"  Charge: {item.Description} ${item.Amount}");
                        }
                        else if (itemType == "payment")
                        {
                            Payments.Add(item);
                            Console.WriteLine($"  Payment: {item.Description} ${item.Amount}");
                        }
                    }
                }

                Console.WriteLine($"[GuestDetail] Final counts - Charges: {Charges.Count}, Payments: {Payments.Count}");

                AuditLog.Clear();
                AuditLog.Add(new AuditEntry
                {
                    Timestamp = DateTimeOffset.Now,
                    Action = "View",
                    Description = "Opened folio details",
                    User = "Staff"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuestDetail] Error loading folio: {ex.Message}");
                Console.WriteLine($"[GuestDetail] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Add charge from detail window.
        /// </summary>
        private async Task AddChargeAsync()
        {
            if (_folios == null || string.IsNullOrWhiteSpace(_currentFolioId))
            {
                Console.WriteLine("[GuestDetail] Cannot add charge: no folio service or folio ID");
                return;
            }

            try
            {
                var amount = ParseDecimal(ChargeAmount);
                var description = ChargeDescription ?? "Miscellaneous Charge";

                Console.WriteLine($"[GuestDetail] Adding charge: {description} ${amount} to folio {_currentFolioId}");

                await _folios.PostChargeAsync(_currentFolioId, amount, description);

                // Refresh the folio data
                await LoadAsync(_currentFolioId);

                // Clear inputs
                ChargeAmount = string.Empty;
                ChargeDescription = string.Empty;

                // Notify parent
                DataChanged?.Invoke(this, EventArgs.Empty);

                Console.WriteLine("[GuestDetail] Charge added successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuestDetail] Error adding charge: {ex.Message}");
            }
        }

        /// <summary>
        /// Add payment from detail window.
        /// </summary>
        private async Task AddPaymentAsync()
        {
            if (_folios == null || string.IsNullOrWhiteSpace(_currentFolioId))
            {
                Console.WriteLine("[GuestDetail] Cannot add payment: no folio service or folio ID");
                return;
            }

            try
            {
                var amount = ParseDecimal(PaymentAmount);
                var method = PaymentMethod ?? "Cash";

                Console.WriteLine($"[GuestDetail] Adding payment: {method} ${amount} to folio {_currentFolioId}");

                await _folios.ApplyPaymentAsync(_currentFolioId, amount, method);

                // Refresh the folio data
                await LoadAsync(_currentFolioId);

                // Clear inputs
                PaymentAmount = string.Empty;

                // Notify parent
                DataChanged?.Invoke(this, EventArgs.Empty);

                Console.WriteLine("[GuestDetail] Payment added successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuestDetail] Error adding payment: {ex.Message}");
            }
        }

        private bool CanAddCharge()
        {
            var amount = ParseDecimal(ChargeAmount);
            return _folios != null &&
                   !string.IsNullOrWhiteSpace(_currentFolioId) &&
                   amount > 0 &&
                   !string.IsNullOrWhiteSpace(ChargeDescription);
        }

        private bool CanAddPayment()
        {
            var amount = ParseDecimal(PaymentAmount);
            return _folios != null &&
                   !string.IsNullOrWhiteSpace(_currentFolioId) &&
                   amount > 0 &&
                   !string.IsNullOrWhiteSpace(PaymentMethod);
        }

        /// <summary>
        /// NEW: CanExecute for RemoveChargeCommand.
        /// </summary>
        private bool CanRemoveCharge(object? parameter)
        {
            var id = parameter as string;
            return !string.IsNullOrWhiteSpace(id) &&
                   Charges.Any(c => c.Id == id);
        }

        /// <summary>
        /// NEW: CanExecute for RemovePaymentCommand.
        /// </summary>
        private bool CanRemovePayment(object? parameter)
        {
            var id = parameter as string;
            return !string.IsNullOrWhiteSpace(id) &&
                   Payments.Any(p => p.Id == id);
        }

        /// <summary>
        /// NEW: Remove a charge line item by Id.
        /// </summary>
        private async Task RemoveChargeAsync(string? lineItemId)
        {
            if (string.IsNullOrWhiteSpace(lineItemId))
                return;

            Console.WriteLine($"[GuestDetail] Removing charge {lineItemId}...");

            try
            {
                // ✅ NEW: persist via IFolioService when available
                if (_folios != null && !string.IsNullOrWhiteSpace(_currentFolioId))
                {
                    try
                    {
                        await _folios.RemoveChargeAsync(_currentFolioId!, lineItemId);
                    }
                    catch (NotImplementedException)
                    {
                        Console.WriteLine("[GuestDetail] IFolioService.RemoveChargeAsync not implemented; falling back to local-only removal.");
                    }
                    catch (Exception exSvc)
                    {
                        Console.WriteLine($"[GuestDetail] Error calling IFolioService.RemoveChargeAsync: {exSvc.Message}");
                    }
                }

                // Existing local removal logic (unchanged)
                var item = Charges.FirstOrDefault(c => c.Id == lineItemId);
                if (item != null)
                {
                    Charges.Remove(item);
                    Console.WriteLine($"[GuestDetail] Charge {lineItemId} removed locally");
                }

                RecalculateBalance();

                AuditLog.Add(new AuditEntry
                {
                    Timestamp = DateTimeOffset.Now,
                    Action = "Remove Charge",
                    Description = $"Removed charge {lineItemId}",
                    User = "Staff"
                });

                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuestDetail] Error removing charge: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// NEW: Remove a payment line item by Id.
        /// </summary>
        private async Task RemovePaymentAsync(string? lineItemId)
        {
            if (string.IsNullOrWhiteSpace(lineItemId))
                return;

            Console.WriteLine($"[GuestDetail] Removing payment {lineItemId}...");

            try
            {
                // ✅ NEW: persist via IFolioService when available
                if (_folios != null && !string.IsNullOrWhiteSpace(_currentFolioId))
                {
                    try
                    {
                        await _folios.RemovePaymentAsync(_currentFolioId!, lineItemId);
                    }
                    catch (NotImplementedException)
                    {
                        Console.WriteLine("[GuestDetail] IFolioService.RemovePaymentAsync not implemented; falling back to local-only removal.");
                    }
                    catch (Exception exSvc)
                    {
                        Console.WriteLine($"[GuestDetail] Error calling IFolioService.RemovePaymentAsync: {exSvc.Message}");
                    }
                }

                // Existing local removal logic (unchanged)
                var item = Payments.FirstOrDefault(p => p.Id == lineItemId);
                if (item != null)
                {
                    Payments.Remove(item);
                    Console.WriteLine($"[GuestDetail] Payment {lineItemId} removed locally");
                }

                RecalculateBalance();

                AuditLog.Add(new AuditEntry
                {
                    Timestamp = DateTimeOffset.Now,
                    Action = "Remove Payment",
                    Description = $"Removed payment {lineItemId}",
                    User = "Staff"
                });

                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuestDetail] Error removing payment: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// NEW: Recalculate balance from current in-memory collections.
        /// </summary>
        private void RecalculateBalance()
        {
            var totalCharges = Charges.Sum(c => c.Amount);
            var totalPayments = Payments.Sum(p => p.Amount);
            CurrentBalance = totalCharges - totalPayments;
            Console.WriteLine($"[GuestDetail] Balance recalculated: {CurrentBalance:C}");
        }

        private static decimal ParseDecimal(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0m;

            // Remove any currency symbols or spaces
            input = input.Replace("$", string.Empty, StringComparison.Ordinal)
                         .Replace(" ", string.Empty, StringComparison.Ordinal)
                         .Trim();

            if (decimal.TryParse(input, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var result))
                return Math.Abs(result);

            return 0m;
        }

        // ALL ORIGINAL CONSTRUCTORS PRESERVED BELOW

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
