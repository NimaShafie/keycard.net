// Views/GuestDetailWindow.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace KeyCard.Desktop.Modules.Folio.Views
{
    public partial class GuestDetailWindow : Window
    {
        public GuestDetailWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
