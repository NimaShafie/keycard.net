using System;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace KeyCard.Desktop.Infrastructure
{
    /// <summary>
    /// Resolves Views for ViewModels by convention:
    ///   KeyCard.Desktop.ViewModels.FooViewModel -> KeyCard.Desktop.Views.FooView
    /// Works without depending on a specific base VM type.
    /// </summary>
    public sealed class ViewLocator : IDataTemplate
    {
        public Control Build(object? data)
        {
            if (data is null)
                return new TextBlock { Text = "null" };

            var vmType = data.GetType();
            var name = vmType.FullName?
                .Replace(".ViewModels.", ".Views.", StringComparison.Ordinal)
                .Replace("ViewModel", "View", StringComparison.Ordinal);

            var viewType = name is null ? null : Type.GetType(name);

            if (viewType is not null)
                return (Control)Activator.CreateInstance(viewType)!;

            // Fallback: show the name we attempted to resolve
            return new TextBlock { Text = name ?? vmType.Name };
        }

        public bool Match(object? data) =>
            data is not null &&
            data.GetType().Name.EndsWith("ViewModel", StringComparison.Ordinal);
    }
}
