// /Modules/Folio/Views/FolioView.axaml.cs
using System;

using Avalonia;
using Avalonia.Controls;

using KeyCard.Desktop.Modules.Folio.ViewModels;

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
    }

    public interface IHaveServiceProvider
    {
        IServiceProvider Services { get; }
    }
}
