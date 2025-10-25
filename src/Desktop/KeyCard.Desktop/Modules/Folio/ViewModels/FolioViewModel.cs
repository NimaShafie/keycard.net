// Modules/Folio/ViewModels/FolioViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Modules.Folio.Models;
using KeyCard.Desktop.Modules.Folio.Services;
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
                    (PostChargeCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (ApplyPaymentCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                    (PrintStatementCommand as UnifiedRelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private decimal _chargeAmount;
        public decimal ChargeAmount
        {
            get => _chargeAmount;
            set => SetProperty(ref _chargeAmount, value);
        }

        private string? _chargeDescription;
        public string? ChargeDescription
        {
            get => _chargeDescription;
            set => SetProperty(ref _chargeDescription, value);
        }

        private decimal _paymentAmount;
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set => SetProperty(ref _paymentAmount, value);
        }

        private string? _paymentMethod;
        public string? PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

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

        public ICommand SearchFoliosCommand { get; }
        public ICommand PostChargeCommand { get; }
        public ICommand ApplyPaymentCommand { get; }
        public ICommand PrintStatementCommand { get; }
        public ICommand RefreshCommand { get; }

        public FolioViewModel(IFolioService folio, ILogger<FolioViewModel> logger)
        {
            _folio = folio ?? throw new ArgumentNullException(nameof(folio));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SearchFoliosCommand = new UnifiedRelayCommand(SearchFoliosAsync, () => !IsBusy);
            PostChargeCommand = new UnifiedRelayCommand(PostChargeAsync, () => CanPostCharge());
            ApplyPaymentCommand = new UnifiedRelayCommand(ApplyPaymentAsync, () => CanApplyPayment());
            PrintStatementCommand = new UnifiedRelayCommand(PrintStatementAsync, () => SelectedFolio != null && !IsBusy);
            RefreshCommand = new UnifiedRelayCommand(RefreshAsync, () => !IsBusy);

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
