// ViewModels/MainViewModel.Folio.cs
using System;

using KeyCard.Desktop.Modules.Folio.Services;
using KeyCard.Desktop.Modules.Folio.ViewModels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KeyCard.Desktop.ViewModels
{
    public partial class MainViewModel
    {
        /// <summary>
        /// Navigate to the Folio Manager, creating the VM on demand.
        /// Implementation of the partial method declared in MainViewModel.cs
        /// </summary>
        partial void NavigateFolioImpl()
        {
            try
            {
                // Try to get FolioViewModel from DI first
                var folioVm = _serviceProvider.GetService<FolioViewModel>();

                if (folioVm == null)
                {
                    // If not registered, create manually with required dependencies
                    var folioService = _serviceProvider.GetService<IFolioService>();
                    var logger = _serviceProvider.GetService<ILogger<FolioViewModel>>();

                    if (folioService == null)
                    {
                        // Fallback to mock service if not registered
                        folioService = new MockFolioService();
                    }

                    if (logger == null)
                    {
                        // Create a simple logger if not available
                        var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
                        logger = loggerFactory?.CreateLogger<FolioViewModel>()
                            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<FolioViewModel>.Instance;
                    }

                    folioVm = new FolioViewModel(folioService, logger);
                }

                // FolioViewModel now inherits from ViewModelBase, so this works
                Current = folioVm;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to navigate to Folio: {ex.Message}");
                // Stay on current page if navigation fails
            }
        }
    }
}
