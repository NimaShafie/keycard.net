// ViewModels/ProfileViewModel.cs
using System;

using KeyCard.Desktop.Services;
using KeyCard.Desktop.Services.Mock;

namespace KeyCard.Desktop.ViewModels
{
    public partial class ProfileViewModel : ViewModelBase
    {
        private readonly IAuthService _auth;
        private readonly IAppEnvironment _env;

        private string _name = "Unknown User";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _role = "Staff";
        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        private string _mode = "Unknown";
        public string Mode
        {
            get => _mode;
            set => SetProperty(ref _mode, value);
        }

        private string _apiEndpoint = "Not configured";
        public string ApiEndpoint
        {
            get => _apiEndpoint;
            set => SetProperty(ref _apiEndpoint, value);
        }

        public ProfileViewModel(IAuthService auth, IAppEnvironment env)
        {
            _auth = auth;
            _env = env;
            LoadProfile();
        }

        private void LoadProfile()
        {
            Name = _auth.DisplayName ?? "Unknown User";
            Role = DetermineRole();
            Mode = _env.IsMock ? "Mock Mode" : "Live Mode";
            ApiEndpoint = _env.ApiBaseUrl;
        }

        private string DetermineRole()
        {
            if (_auth is AuthService)
                return "Front Desk (Mock)";

            var name = _auth.DisplayName?.ToLowerInvariant() ?? string.Empty;

            if (name.Contains("admin")) return "Administrator";
            if (name.Contains("manager")) return "Manager";
            if (name.Contains("housekeeping")) return "Housekeeping Staff";

            return "Front Desk Staff";
        }
    }
}
