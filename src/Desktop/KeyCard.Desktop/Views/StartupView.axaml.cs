// Views/StartupView.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;

using KeyCard.Desktop.ViewModels;

using System.Threading;

namespace KeyCard.Desktop.Views
{
    public partial class StartupView : UserControl
    {
        private CancellationTokenSource? _cts;

        public StartupView()
        {
            InitializeComponent();
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            if (DataContext is StartupViewModel viewModel)
            {
                _cts = new CancellationTokenSource();
                await viewModel.InitializeAsync(_cts.Token);
            }
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            base.OnUnloaded(e);
        }
    }
}
