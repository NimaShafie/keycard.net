// ProfileViewModel.cs
using System.Diagnostics.CodeAnalysis;

namespace KeyCard.Desktop.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        [SuppressMessage("Performance", "CA1822")]
        public string Name => "Mock Staff";

        [SuppressMessage("Performance", "CA1822")]
        public string Role => "Front Desk (Mock)";
    }
}
