// Modules/Folio/ViewModels/FolioViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Modules.Folio.Models;
using KeyCard.Desktop.Modules.Folio.Services;
using KeyCard.Desktop.Services;
using KeyCard.Desktop.ViewModels;

using Microsoft.Extensions.Logging;

namespace KeyCard.Desktop.Modules.Folio.ViewModels
{
    /// <summary>
    /// Main view model for the Folio Manager module.
    /// </summary>
    public class FolioViewModel : ViewModelBase
    {
        private readonly IFolioService _folio;
        private readonly ILogger<FolioViewModel> _logger;
        private readonly IToolbarService _toolbar;

        // Event for opening guest detail window
        public event EventHandler<string>? OpenGuestDetailRequested;

        // --- Search / selection ---

        private string? _guestSearchText;
        public string? GuestSearchText
        {
            get => _guestSearchText;
            set => SetProperty(ref _guestSearchText, value);
        }

        private GuestFolio? _selectedFolio;
        public GuestFolio? SelectedFolio
        {
            get => _selectedFolio;
            set
            {
                if (SetProperty(ref _selectedFolio, value))
                {
                    RaiseAllCanExecuteChanged();
                }
            }
        }

        // --- Charge inputs (using string for TextBox binding) ---

        private string? _chargeAmountText;
        public string? ChargeAmountText
        {
            get => _chargeAmountText;
            set
            {
                if (SetProperty(ref _chargeAmountText, value))
                {
                    RaiseAllCanExecuteChanged();
                    OnPropertyChanged(nameof(CanAddCharge));
                }
            }
        }

        private string? _chargeDescription;
        public string? ChargeDescription
        {
            get => _chargeDescription;
            set
            {
                if (SetProperty(ref _chargeDescription, value))
                {
                    RaiseAllCanExecuteChanged();
                    OnPropertyChanged(nameof(CanAddCharge));
                }
            }
        }

        // Alias for XAML binding compatibility
        public string? NewChargeDescription
        {
            get => ChargeDescription;
            set => ChargeDescription = value;
        }

        // --- Payment inputs (using string for TextBox binding) ---

        private string? _paymentAmountText;
        public string? PaymentAmountText
        {
            get => _paymentAmountText;
            set
            {
                if (SetProperty(ref _paymentAmountText, value))
                {
                    RaiseAllCanExecuteChanged();
                    OnPropertyChanged(nameof(CanAddPayment));
                }
            }
        }

        private string? _paymentMethod;
        public string? PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (SetProperty(ref _paymentMethod, value))
                {
                    RaiseAllCanExecuteChanged();
                    OnPropertyChanged(nameof(CanAddPayment));
                }
            }
        }

        // Alias for XAML binding compatibility
        public string? NewPaymentMethod
        {
            get => PaymentMethod;
            set => PaymentMethod = value;
        }

        // --- Busy / status ---

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseAllCanExecuteChanged();
                    OnPropertyChanged(nameof(CanAddCharge));
                    OnPropertyChanged(nameof(CanAddPayment));
                }
            }
        }

        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<GuestFolio> Folios { get; } = new();

        public ObservableCollection<string> PaymentMethods { get; } = new()
        {
            "Cash",
            "Credit Card",
            "Debit Card",
            "Room Charge",
            "Check"
        };

        // --- Commands ---

        public ICommand SearchFoliosCommand { get; }
        public ICommand PostChargeCommand { get; }
        public ICommand ApplyPaymentCommand { get; }
        public ICommand PrintStatementCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenGuestDetailCommand { get; }

        // --- Command aliases ---
        public ICommand AddChargeCommand { get; }
        public ICommand AddPaymentCommand { get; }
        public ICommand GenerateInvoiceCommand { get; }

        // --- Convenience booleans for UI ---
        public bool CanAddCharge => CanPostCharge();
        public bool CanAddPayment => CanApplyPayment();

        public FolioViewModel(IFolioService folio, ILogger<FolioViewModel> logger, IToolbarService toolbar)
        {
            _folio = folio ?? throw new ArgumentNullException(nameof(folio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toolbar = toolbar;

            SearchFoliosCommand = new UnifiedRelayCommand(SearchFoliosAsync, () => !IsBusy);
            PostChargeCommand = new UnifiedRelayCommand(PostChargeAsync, CanPostCharge);
            ApplyPaymentCommand = new UnifiedRelayCommand(ApplyPaymentAsync, CanApplyPayment);
            PrintStatementCommand = new UnifiedRelayCommand(PrintStatementAsync, () => SelectedFolio != null && !IsBusy);
            RefreshCommand = new UnifiedRelayCommand(RefreshAsync, () => !IsBusy);

            // Use non-generic UnifiedRelayCommand with object parameter
            OpenGuestDetailCommand = new UnifiedRelayCommand(
                async (param) => await OpenGuestDetailAsync(param as string),
                (param) => !IsBusy
            );

            _toolbar.AttachContext(
                title: "Folio Manager",
                subtitle: "Manage charges, payments, and statements",
                onRefreshAsync: RefreshAsync,
                onSearch: q => { /* optional: set your own search/filter */ },
                initialSearchText: null
            );

            // Alias commands
            AddChargeCommand = PostChargeCommand;
            AddPaymentCommand = ApplyPaymentCommand;
            GenerateInvoiceCommand = PrintStatementCommand;

            // Load initial data
            _ = RefreshAsync();
        }

        /// <summary>
        /// Initialize the ViewModel - called by view after construction.
        /// </summary>
        public async Task InitializeAsync(Guid? bookingId = null)
        {
            if (bookingId.HasValue)
            {
                GuestSearchText = bookingId.ToString();
                await SearchFoliosAsync();
            }
            else
            {
                await RefreshAsync();
            }
        }

        private async Task SearchFoliosAsync()
        {
            if (string.IsNullOrWhiteSpace(GuestSearchText))
            {
                await RefreshAsync();
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = $"Searching for '{GuestSearchText}'...";

                var results = await _folio.SearchFoliosAsync(GuestSearchText);

                Folios.Clear();
                foreach (var folio in results)
                {
                    Folios.Add(folio);
                }

                StatusMessage = $"Found {Folios.Count} folio(s)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching folios");
                StatusMessage = $"Search error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading folios...";

                var folios = await _folio.GetActiveFoliosAsync();

                Folios.Clear();
                foreach (var folio in folios)
                {
                    Folios.Add(folio);
                }

                StatusMessage = $"Loaded {Folios.Count} active folio(s)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading folios");
                StatusMessage = $"Load error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PostChargeAsync()
        {
            if (SelectedFolio == null)
            {
                StatusMessage = "Please select a folio first";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Posting charge...";

                var amount = ParseDecimal(ChargeAmountText);
                var description = ChargeDescription ?? "Miscellaneous Charge";

                _logger.LogInformation("Posting charge: {Amount} - {Description} to folio {FolioId}",
                    amount, description, SelectedFolio.FolioId);

                await _folio.PostChargeAsync(
                    SelectedFolio.FolioId,
                    amount,
                    description);

                // Refresh ALL folios to get updated balances
                await RefreshAllFolios();

                StatusMessage = $"Charge of {amount:C} posted successfully";
                _logger.LogInformation("Charge posted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting charge");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;

                // ✅ FIX: Clear inputs AFTER IsBusy is set to false
                // This ensures CanPostCharge() can return true again
                ChargeAmountText = string.Empty;
                ChargeDescription = string.Empty;
                OnPropertyChanged(nameof(ChargeAmountText));
                OnPropertyChanged(nameof(ChargeDescription));
                OnPropertyChanged(nameof(NewChargeDescription));
                RaiseAllCanExecuteChanged();
            }
        }

        private bool CanPostCharge()
        {
            var amount = ParseDecimal(ChargeAmountText);
            return !IsBusy &&
                   SelectedFolio is not null &&
                   amount > 0 &&
                   !string.IsNullOrWhiteSpace(ChargeDescription);
        }

        private async Task ApplyPaymentAsync()
        {
            if (SelectedFolio is null)
            {
                StatusMessage = "Please select a folio first";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Applying payment...";

                var amount = ParseDecimal(PaymentAmountText);
                var method = PaymentMethod ?? "Cash";

                _logger.LogInformation("Applying payment: {Amount} via {Method} to folio {FolioId}",
                    amount, method, SelectedFolio.FolioId);

                await _folio.ApplyPaymentAsync(
                    SelectedFolio.FolioId,
                    amount,
                    method);

                // Refresh ALL folios to get updated balances
                await RefreshAllFolios();

                StatusMessage = $"Payment of {amount:C} applied successfully";
                _logger.LogInformation("Payment applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying payment");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;

                // ✅ FIX: Clear inputs AFTER IsBusy is set to false
                // This ensures CanApplyPayment() can return true again
                PaymentAmountText = string.Empty;
                OnPropertyChanged(nameof(PaymentAmountText));
                RaiseAllCanExecuteChanged();
            }
        }

        private bool CanApplyPayment()
        {
            var amount = ParseDecimal(PaymentAmountText);
            return !IsBusy &&
                   SelectedFolio is not null &&
                   amount > 0 &&
                   !string.IsNullOrWhiteSpace(PaymentMethod);
        }

        private async Task PrintStatementAsync()
        {
            if (SelectedFolio is null)
            {
                StatusMessage = "Please select a folio first";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Generating statement...";

                _logger.LogInformation("Generating statement for folio {FolioId}", SelectedFolio.FolioId);

                await _folio.PrintStatementAsync(SelectedFolio.FolioId);

                StatusMessage = "Statement generated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing statement");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OpenGuestDetailAsync(string? folioId)
        {
            Console.WriteLine($"[FolioViewModel] OpenGuestDetailAsync called with folioId: {folioId}");

            if (string.IsNullOrWhiteSpace(folioId))
            {
                folioId = SelectedFolio?.FolioId;
                Console.WriteLine($"[FolioViewModel] FolioId was null, using SelectedFolio: {folioId}");
            }

            if (!string.IsNullOrWhiteSpace(folioId))
            {
                Console.WriteLine($"[FolioViewModel] About to invoke OpenGuestDetailRequested event for folio {folioId}");
                Console.WriteLine($"[FolioViewModel] Event has {(OpenGuestDetailRequested == null ? "NO" : OpenGuestDetailRequested.GetInvocationList().Length.ToString())} subscriber(s)");

                _logger.LogInformation("Opening guest detail for folio {FolioId}", folioId);
                OpenGuestDetailRequested?.Invoke(this, folioId);

                Console.WriteLine($"[FolioViewModel] Event invoked");
            }
            else
            {
                StatusMessage = "Please select a folio first";
                Console.WriteLine($"[FolioViewModel] Cannot open detail: no folio selected");
                _logger.LogWarning("OpenGuestDetail called with no folio selected");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Refresh all folios and maintain the current selection
        /// </summary>
        private async Task RefreshAllFolios()
        {
            var selectedFolioId = SelectedFolio?.FolioId;

            // Get fresh data from service
            var folios = await _folio.GetActiveFoliosAsync();

            // Update collection
            Folios.Clear();
            foreach (var folio in folios)
            {
                Folios.Add(folio);
            }

            // Restore selection if it still exists
            if (!string.IsNullOrWhiteSpace(selectedFolioId))
            {
                SelectedFolio = Folios.FirstOrDefault(f => f.FolioId == selectedFolioId);
            }

            _logger.LogInformation("Refreshed {Count} folios, balance updated", Folios.Count);
        }

        /// <summary>
        /// Safely parse decimal from string input, handling null/empty/invalid cases
        /// </summary>
        private static decimal ParseDecimal(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0m;

            // Remove any currency symbols or spaces
            input = input.Replace("$", "").Replace(" ", "").Trim();

            if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
                return Math.Abs(result);

            return 0m;
        }

        /// <summary>
        /// Helper to raise CanExecuteChanged on all commands
        /// </summary>
        private void RaiseAllCanExecuteChanged()
        {
            (SearchFoliosCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            (PostChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            (ApplyPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            (PrintStatementCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            (OpenGuestDetailCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
            (RefreshCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
