// Modules/Folio/Views/FolioView.axaml.cs
using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

using KeyCard.Desktop.Modules.Folio.ViewModels;
using KeyCard.Desktop.ViewModels; // MainViewModel

using Microsoft.Extensions.DependencyInjection;

namespace KeyCard.Desktop.Modules.Folio.Views
{
    public partial class FolioView : UserControl
    {
        public FolioView()
        {
            InitializeComponent();

            if (Design.IsDesignMode)
                return;

            // Only set DataContext if not already provided
            if (DataContext is not FolioViewModel vm)
            {
                var sp = (Application.Current as IHaveServiceProvider)?.Services;
                if (sp is not null)
                {
                    try
                    {
                        vm = sp.GetRequiredService<FolioViewModel>();
                    }
                    catch
                    {
                        vm = ActivatorUtilities.CreateInstance<FolioViewModel>(sp);
                    }

                    DataContext = vm;
                }
            }

            if (DataContext is FolioViewModel folioVm)
            {
                folioVm.OpenGuestDetailRequested -= OnOpenGuestDetailRequested;
                folioVm.OpenGuestDetailRequested += OnOpenGuestDetailRequested;

                Dispatcher.UIThread.Post(async () =>
                {
                    try { await folioVm.InitializeAsync(null); } catch { /* ignore */ }
                });
            }
        }

        private async void OnOpenGuestDetailRequested(object? sender, string folioId)
        {
            var sp = (Application.Current as IHaveServiceProvider)?.Services;
            var owner = this.VisualRoot as Window;

            var detailWindow = new KeyCard.Desktop.Modules.Folio.Views.GuestDetailWindow();

            GuestDetailViewModel detailVm;
            try
            {
                detailVm = sp?.GetRequiredService<GuestDetailViewModel>()
                           ?? ActivatorUtilities.CreateInstance<GuestDetailViewModel>(sp!);
            }
            catch
            {
                detailVm = new GuestDetailViewModel();
            }

            try { await detailVm.LoadAsync(folioId); } catch { /* ignore */ }
            detailWindow.DataContext = detailVm;

            if (owner is not null)
                await detailWindow.ShowDialog(owner);
            else
                detailWindow.Show();
        }

        private void OnBackClick(object? sender, RoutedEventArgs e)
        {
            if (VisualRoot is Window w &&
                w.DataContext is MainViewModel main &&
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
