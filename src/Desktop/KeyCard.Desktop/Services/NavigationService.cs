// Services/NavigationService.cs
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

using Avalonia.Threading;

using KeyCard.Desktop.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop.Services
{
    /// <summary>
    /// Interface for ViewModels that need to know when they're navigated to/from
    /// </summary>
    public interface INavigationAware
    {
        void OnNavigatedTo();
        void OnNavigatedFrom();
    }

    /// <summary>
    /// Robust, UI-thread safe navigation that avoids app crashes when a VM cannot be created.
    /// </summary>
    public sealed class NavigationService : INavigationService
    {
        private readonly IServiceProvider _sp;

        public NavigationService(IServiceProvider sp) => _sp = sp;

        // The interface in your project expects a non-nullable ViewModelBase.
        public ViewModelBase Current => _sp.GetRequiredService<MainViewModel>().Current!;

        public void NavigateTo<TVm>() where TVm : ViewModelBase
        {
            var shell = _sp.GetRequiredService<MainViewModel>();
            TVm? vm = null;
            Exception? last = null;

            // Track which method succeeded
            string resolutionMethod = "UNKNOWN";
            int instanceHash = 0;

            // Call OnNavigatedFrom on the current VM
            if (shell.Current is INavigationAware currentNav)
            {
                currentNav.OnNavigatedFrom();
            }

            // DIAGNOSTIC: Log navigation attempt
            Debug.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.WriteLine($"[NavigationService] Navigating to {typeof(TVm).Name}");

            // 1) Resolve from DI (should get Singleton)
            try
            {
                vm = _sp.GetRequiredService<TVm>();
                resolutionMethod = "DI Singleton";
                instanceHash = vm.GetHashCode();
                Debug.WriteLine($"[NavigationService] ✅ SUCCESS: Got {typeof(TVm).Name} from DI (Singleton)");
                Debug.WriteLine($"[NavigationService] Instance HashCode: {instanceHash}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NavigationService] ❌ FAILED to get {typeof(TVm).Name} from DI: {ex.Message}");
                last = ex;
            }

            // 2) Try to create via ActivatorUtilities (uses DI for known deps)
            // ⚠️ THIS CREATES A NEW INSTANCE - NOT A SINGLETON!
            if (vm is null)
            {
                try
                {
                    Debug.WriteLine($"[NavigationService] ⚠️ FALLBACK: Creating NEW instance of {typeof(TVm).Name} via ActivatorUtilities");
                    vm = ActivatorUtilities.CreateInstance<TVm>(_sp);
                    resolutionMethod = "ActivatorUtilities (NEW)";
                    instanceHash = vm.GetHashCode();
                    Debug.WriteLine($"[NavigationService] Created new instance - HashCode: {instanceHash}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NavigationService] ❌ ActivatorUtilities failed: {ex.Message}");
                    last = ex;
                }
            }

            // 3) Try public/non-public parameterless ctor
            if (vm is null)
            {
                try
                {
                    Debug.WriteLine($"[NavigationService] ⚠️ FALLBACK: Creating NEW instance via Activator.CreateInstance");
                    vm = (TVm?)Activator.CreateInstance(typeof(TVm), nonPublic: true);
                    resolutionMethod = "Activator (NEW)";
                    instanceHash = vm?.GetHashCode() ?? 0;
                    Debug.WriteLine($"[NavigationService] Created new instance - HashCode: {instanceHash}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NavigationService] ❌ Activator.CreateInstance failed: {ex.Message}");
                    last = ex;
                }
            }

            // 4) Last resort: uninitialized instance (avoids ctor exceptions)
            if (vm is null)
            {
                try
                {
                    Debug.WriteLine($"[NavigationService] ⚠️ FALLBACK: Creating uninitialized instance");
#pragma warning disable SYSLIB0050
                    vm = (TVm)FormatterServices.GetUninitializedObject(typeof(TVm));
#pragma warning restore SYSLIB0050
                    resolutionMethod = "Uninitialized (NEW)";
                    instanceHash = vm.GetHashCode();
                    Debug.WriteLine($"[NavigationService] Created uninitialized instance - HashCode: {instanceHash}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NavigationService] ❌ GetUninitializedObject failed: {ex.Message}");
                    last = ex;
                }
            }

            if (vm is null)
            {
                // Never crash: navigate to an error VM with details.
                Debug.WriteLine($"[NavigationService] ❌ ALL METHODS FAILED - Showing error screen");
                var err = new NavigationErrorViewModel(
                    $"Cannot open {typeof(TVm).Name}",
                    last?.Message ?? "Unknown construction error.");
                Dispatcher.UIThread.Post(() => shell.Current = err);
                Debug.WriteLine(last);
                return;
            }

            Debug.WriteLine($"[NavigationService] Setting MainViewModel.Current to {typeof(TVm).Name}");
            Debug.WriteLine($"[NavigationService] Resolution: {resolutionMethod}, Hash: {instanceHash}");
            Debug.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // Set status message if it's HousekeepingViewModel
            if (vm is HousekeepingViewModel hkVm)
            {
                var msg = $"Nav: {resolutionMethod} | Hash: {instanceHash}";
                Dispatcher.UIThread.Post(() => hkVm.StatusMessage = msg);
            }

            Dispatcher.UIThread.Post(() =>
            {
                shell.Current = vm;

                // Call OnNavigatedTo on the new VM
                if (vm is INavigationAware newNav)
                {
                    newNav.OnNavigatedTo();
                }
            });
        }
    }

    /// <summary>
    /// Simple error VM used when a target view-model cannot be constructed.
    /// Your ViewLocator will either resolve a view for this type or show its default "not found" UI.
    /// </summary>
    public sealed class NavigationErrorViewModel : ViewModelBase
    {
        public NavigationErrorViewModel(string title, string details)
        {
            Title = title;
            Details = details;
        }

        public string Title { get; }
        public string Details { get; }
    }
}
