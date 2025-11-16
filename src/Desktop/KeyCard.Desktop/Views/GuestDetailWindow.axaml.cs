// Views/GuestDetailWindow.axaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using KeyCard.Desktop.Modules.Folio.ViewModels;

namespace KeyCard.Desktop.Modules.Folio.Views
{
    public partial class GuestDetailWindow : Window
    {
        public GuestDetailWindow()
        {
            InitializeComponent();
        }

        // Optional convenience constructor: pass a VM at runtime
        public GuestDetailWindow(GuestDetailViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
