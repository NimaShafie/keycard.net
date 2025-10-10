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

            // 1) Resolve from DI
            try
            {
                vm = _sp.GetRequiredService<TVm>();
            }
            catch (Exception ex)
            {
                last = ex;
            }

            // 2) Try to create via ActivatorUtilities (uses DI for known deps)
            if (vm is null)
            {
                try
                {
                    vm = ActivatorUtilities.CreateInstance<TVm>(_sp);
                }
                catch (Exception ex)
                {
                    last = ex;
                }
            }

            // 3) Try public/non-public parameterless ctor
            if (vm is null)
            {
                try
                {
                    vm = (TVm?)Activator.CreateInstance(typeof(TVm), nonPublic: true);
                }
                catch (Exception ex)
                {
                    last = ex;
                }
            }

            // 4) Last resort: uninitialized instance (avoids ctor exceptions)
            if (vm is null)
            {
                try
                {
#pragma warning disable SYSLIB0050
                    vm = (TVm)FormatterServices.GetUninitializedObject(typeof(TVm));
#pragma warning restore SYSLIB0050
                }
                catch (Exception ex)
                {
                    last = ex;
                }
            }

            if (vm is null)
            {
                // Never crash: navigate to an error VM with details.
                var err = new NavigationErrorViewModel(
                    $"Cannot open {typeof(TVm).Name}",
                    last?.Message ?? "Unknown construction error.");

                Dispatcher.UIThread.Post(() => shell.Current = err);
                Debug.WriteLine(last);
                return;
            }

            Dispatcher.UIThread.Post(() => shell.Current = vm);
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
