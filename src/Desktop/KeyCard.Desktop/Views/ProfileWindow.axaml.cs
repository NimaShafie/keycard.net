// Views/ProfileWindow.axaml.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace KeyCard.Desktop.Views
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();

            // Close buttons / backdrop
            this.FindControl<Button>("CloseButton")!.Click += (_, __) => Close();
            var backdrop = this.FindControl<Border>("BackdropCloser");
            if (backdrop is not null)
            {
                backdrop.PointerPressed += (_, __) => Close();
            }

            // Drag window by title bar
            var titleBar = this.FindControl<Grid>("TitleBar");
            if (titleBar is not null)
            {
                titleBar.PointerPressed += (_, e) =>
                {
                    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                        BeginMoveDrag(e);
                };
            }

            // ESC to close
            this.AddHandler(KeyDownEvent, (_, e) =>
            {
                if (e.Key == Key.Escape) Close();
            }, RoutingStrategies.Tunnel);
        }

        public ProfileWindow(object? dataContext) : this()
        {
            DataContext = dataContext;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
