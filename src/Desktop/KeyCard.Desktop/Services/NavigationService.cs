// Services/NavigationService.cs
using System;

using Avalonia.Threading;

using KeyCard.Desktop.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop.Services
{
    public sealed class NavigationService : INavigationService
    {
        private readonly IServiceProvider _sp;

        public NavigationService(IServiceProvider sp) => _sp = sp;

        // The interface in your project expects a non-nullable ViewModelBase.
        // We initialized shell.Current in App, so we can assert non-null here.
        public ViewModelBase Current => _sp.GetRequiredService<MainViewModel>().Current!;

        public void NavigateTo<TVm>() where TVm : ViewModelBase
        {
            var shell = _sp.GetRequiredService<MainViewModel>();
            var vm = _sp.GetRequiredService<TVm>();

            // Never block the UI thread:
            Dispatcher.UIThread.Post(() => shell.Current = vm);
        }
    }
}
