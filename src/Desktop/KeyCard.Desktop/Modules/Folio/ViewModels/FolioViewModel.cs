// Modules/Folio/ViewModels/FolioViewModel.cs
using System;
using System.Collections.ObjectModel;
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
    /// NOW inherits from ViewModelBase to work with INavigationService.
    /// </summary>
    public class FolioViewModel : ViewModelBase
    {
        private readonly IFolioService _folio;
        private readonly ILogger<FolioViewModel> _logger;
        private readonly IToolbarService _toolbar;

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
                    (SearchFoliosCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (PostChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (ApplyPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (PrintStatementCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();

                    // keep aliases in sync for views that use them
                    (AddChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (AddPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (GenerateInvoiceCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        // --- Charge inputs (original) ---

        private decimal _chargeAmount;
        public decimal ChargeAmount
        {
            get => _chargeAmount;
            set
            {
                if (SetProperty(ref _chargeAmount, value))
                {
                    (PostChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (AddChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
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
                    (PostChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (AddChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(CanAddCharge));
                }
            }
        }

        // --- Payment inputs (original) ---

        private decimal _paymentAmount;
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                if (SetProperty(ref _paymentAmount, value))
                {
                    (ApplyPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (AddPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
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
                    (ApplyPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (AddPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(CanAddPayment));
                }
            }
        }

        // Optional: reference/notes field used by some UIs; safe no-op for backend
        private string? _paymentReference;
        public string? PaymentReference
        {
            get => _paymentReference;
            set => SetProperty(ref _paymentReference, value);
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
                    (SearchFoliosCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (PostChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (ApplyPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (PrintStatementCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();

                    // aliases
                    (AddChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (AddPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (GenerateInvoiceCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();

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

        // --- Commands (original) ---

        public ICommand SearchFoliosCommand { get; }
        public ICommand PostChargeCommand { get; }
        public ICommand ApplyPaymentCommand { get; }
        public ICommand PrintStatementCommand { get; }
        public ICommand RefreshCommand { get; }

        // --- Aliases expected by alternative XAML bindings (added) ---

        /// <summary>
        /// Mirrors <see cref="ChargeDescription"/> for views that bind to NewChargeDescription.
        /// </summary>
        public string? NewChargeDescription
        {
            get => ChargeDescription;
            set => ChargeDescription = value;
        }

        /// <summary>
        /// Mirrors <see cref="ChargeAmount"/> for views that bind to NewChargeAmount.
        /// </summary>
        public decimal NewChargeAmount
        {
            get => ChargeAmount;
            set => ChargeAmount = value;
        }

        /// <summary>
        /// Mirrors <see cref="PaymentAmount"/> for views that bind to NewPaymentAmount.
        /// </summary>
        public decimal NewPaymentAmount
        {
            get => PaymentAmount;
            set => PaymentAmount = value;
        }

        /// <summary>
        /// Mirrors <see cref="PaymentMethod"/> for views that bind to NewPaymentMethod.
        /// </summary>
        public string? NewPaymentMethod
        {
            get => PaymentMethod;
            set => PaymentMethod = value;
        }

        /// <summary>
        /// Mirrors <see cref="PaymentReference"/> for views that bind to NewPaymentReference.
        /// (Not all backends store this yet; safe to keep local.)
        /// </summary>
        public string? NewPaymentReference
        {
            get => PaymentReference;
            set => PaymentReference = value;
        }

        /// <summary>
        /// Convenience booleans for enabling buttons in some views.
        /// </summary>
        public bool CanAddCharge => CanPostCharge();
        public bool CanAddPayment => CanApplyPayment();

        /// <summary>
        /// Command aliases so both naming schemes work without changing the rest of your app.
        /// </summary>
        public ICommand AddChargeCommand { get; }
        public ICommand AddPaymentCommand { get; }
        public ICommand GenerateInvoiceCommand { get; }

        public FolioViewModel(IFolioService folio, ILogger<FolioViewModel> logger, IToolbarService toolbar)
        {
            _folio = folio ?? throw new ArgumentNullException(nameof(folio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toolbar = toolbar;

            SearchFoliosCommand = new UnifiedRelayCommand(SearchFoliosAsync, () => !IsBusy);
            PostChargeCommand = new UnifiedRelayCommand(PostChargeAsync, () => CanPostCharge());
            ApplyPaymentCommand = new UnifiedRelayCommand(ApplyPaymentAsync, () => CanApplyPayment());
            PrintStatementCommand = new UnifiedRelayCommand(PrintStatementAsync, () => SelectedFolio != null && !IsBusy);
            RefreshCommand = new UnifiedRelayCommand(RefreshAsync, () => !IsBusy);

            _toolbar.AttachContext(
                title: "Folio Manager",
                subtitle: "Manage charges, payments, and statements",
                onRefreshAsync: RefreshAsync, // or null if none
                onSearch: q => { /* optional: set your own search/filter */ },
                initialSearchText: null
            );

            // Alias commands simply reuse the originals to keep behavior identical.
            AddChargeCommand = PostChargeCommand;
            AddPaymentCommand = ApplyPaymentCommand;
            GenerateInvoiceCommand = PrintStatementCommand;

            // Load initial data
            _ = RefreshAsync();
        }

        /// <summary>
        /// Initialize the ViewModel - called by view after construction.
        /// Loads initial folio data.
        /// </summary>
        /// <param name="bookingId">Optional booking ID to filter folios</param>
        public async Task InitializeAsync(Guid? bookingId = null)
        {
            if (bookingId.HasValue)
            {
                // If specific booking requested, search for it
                GuestSearchText = bookingId.ToString();
                await SearchFoliosAsync();
            }
            else
            {
                // Otherwise load all active folios
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
            if (SelectedFolio == null) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Posting charge...";

                await _folio.PostChargeAsync(
                    SelectedFolio.FolioId,
                    ChargeAmount,
                    ChargeDescription ?? "Miscellaneous Charge");

                // Refresh the selected folio
                var updated = await _folio.GetFolioByIdAsync(SelectedFolio.FolioId);
                if (updated != null)
                {
                    var index = Folios.IndexOf(SelectedFolio);
                    if (index >= 0)
                    {
                        Folios[index] = updated;
                        SelectedFolio = updated;
                    }
                }

                ChargeAmount = 0;
                ChargeDescription = null;
                StatusMessage = "Charge posted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting charge");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanPostCharge() =>
            !IsBusy &&
            SelectedFolio is not null &&
            ChargeAmount > 0 &&
            !string.IsNullOrWhiteSpace(ChargeDescription);

        private async Task ApplyPaymentAsync()
        {
            if (SelectedFolio is null) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Applying payment...";

                await _folio.ApplyPaymentAsync(
                    SelectedFolio.FolioId,
                    PaymentAmount,
                    PaymentMethod ?? "Cash");

                // Refresh the selected folio
                var updated = await _folio.GetFolioByIdAsync(SelectedFolio.FolioId);
                if (updated != null)
                {
                    var index = Folios.IndexOf(SelectedFolio);
                    if (index >= 0)
                    {
                        Folios[index] = updated;
                        SelectedFolio = updated;
                    }
                }

                PaymentAmount = 0;
                StatusMessage = "Payment applied successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying payment");
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanApplyPayment() =>
            !IsBusy &&
            SelectedFolio is not null &&
            PaymentAmount > 0 &&
            !string.IsNullOrWhiteSpace(PaymentMethod);

        private async Task PrintStatementAsync()
        {
            if (SelectedFolio is null) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Generating statement...";

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
    }
}
