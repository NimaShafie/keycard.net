// Infrastructure/ViewLocator.cs
using System;
using System.Collections.Concurrent;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace KeyCard.Desktop.Infrastructure
{
    /// <summary>
    /// Lightweight, non-DI view locator:
    ///   ViewModel: KeyCard.Desktop.ViewModels.LoginViewModel
    ///   View:      KeyCard.Desktop.Views.LoginView
    /// </summary>
    public sealed class ViewLocator : IDataTemplate
    {
        private static readonly ConcurrentDictionary<Type, Type?> Cache = new();

        public Control? Build(object? data)
        {
            if (data is null) return null;

            var vmType = data.GetType();
            var viewType = Cache.GetOrAdd(vmType, ResolveViewType);

            if (viewType is null)
            {
                // Fallback so you SEE something instead of hanging
                return new TextBlock
                {
                    Text = $"No view found for {vmType.FullName}",
                    Margin = new Thickness(12)
                };
            }

            try
            {
                // Create the view without DI to avoid startup re-entrancy / deadlocks.
                return (Control)Activator.CreateInstance(viewType)!;
            }
            catch (Exception ex)
            {
                return new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = $"Error creating view for {vmType.FullName}:\n\n{ex}",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    Margin = new Thickness(12)
                };
            }
        }

        public bool Match(object? data) => data is not null && data.GetType().Name.EndsWith("ViewModel", StringComparison.Ordinal);

        private static Type? ResolveViewType(Type vmType)
        {
            var vmNamespace = vmType.Namespace ?? string.Empty;
            var vmName = vmType.Name;

            if (!vmName.EndsWith("ViewModel", StringComparison.Ordinal))
                return null;

            var viewNamespace = vmNamespace.Replace(".ViewModels", ".Views", StringComparison.Ordinal);

            ReadOnlySpan<char> nameSpan = vmName.AsSpan();
            var baseName = nameSpan[..^"ViewModel".Length];
            var viewName = string.Concat(baseName, "View");

            var fullName = $"{viewNamespace}.{viewName}";

            return vmType.Assembly.GetType(fullName);
        }

    }
}
