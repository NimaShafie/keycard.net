// ViewModels/MainViewModel.Folio.cs
using System;

using KeyCard.Desktop.Modules.Folio.Services;
using KeyCard.Desktop.Modules.Folio.ViewModels;
using KeyCard.Desktop.Services;

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
                    var toolbar = _serviceProvider.GetService<IToolbarService>();   // required now

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

                    if (toolbar == null)
                    {
                        // Be defensive: construct a ToolbarService if DI is missing it
                        var nav = _serviceProvider.GetService<INavigationService>();
                        if (nav != null)
                        {
                            toolbar = new ToolbarService(nav);
                        }
                        else
                        {
                            // Absolute fallback: preserve previous behavior clearly
                            throw new InvalidOperationException("IToolbarService is not available and cannot be constructed.");
                        }
                    }

                    // Pass toolbar to the FolioViewModel (new required parameter)
                    folioVm = new FolioViewModel(folioService, logger!, toolbar);
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
