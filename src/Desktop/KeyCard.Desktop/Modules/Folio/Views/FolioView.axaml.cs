// /Modules/Folio/Views/FolioView.axaml.cs
using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using KeyCard.Desktop.Modules.Folio.ViewModels;
using KeyCard.Desktop.ViewModels; // for MainViewModel

using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop.Modules.Folio.Views
{
    public partial class FolioView : UserControl
    {
        public FolioView()
        {
            InitializeComponent();

            // Resolve VM via DI; fallback to existing DataContext if DI isn't exposed yet.
            var sp = (Avalonia.Application.Current as IHaveServiceProvider)?.Services;
            if (sp != null)
            {
                var vm = ActivatorUtilities.CreateInstance<FolioViewModel>(sp);
                DataContext = vm;
                _ = vm.InitializeAsync(null);
            }
            else if (DataContext is FolioViewModel vm)
            {
                _ = vm.InitializeAsync(null);
            }
        }

        // Click handler for the Back button to avoid cross-VM compiled-binding issues.
        private void OnBackClick(object? sender, RoutedEventArgs e)
        {
            if (VisualRoot is Window w && w.DataContext is MainViewModel main &&
                main.NavigateDashboardCommand?.CanExecute(null) == true)
            {
                main.NavigateDashboardCommand.Execute(null);
            }
        }
    }

    public interface IHaveServiceProvider
    {
        IServiceProvider Services { get; }
    }
}
