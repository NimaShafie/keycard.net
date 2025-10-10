// src/Desktop/KeyCard.Desktop/ViewModels/SettingsViewModel.cs
namespace KeyCard.Desktop.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public bool UseMocks { get; set; } = true;
        public bool EnableSounds { get; set; }
    }
}
