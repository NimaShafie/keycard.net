// ViewModels/LoginViewModel.cs
using System.Threading.Tasks;
using System.Windows.Input;
using KeyCard.Desktop.Infrastructure;
using KeyCard.Desktop.Services;

namespace KeyCard.Desktop.ViewModels;

public sealed class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;

    public LoginViewModel(IAuthService auth, INavigationService nav)
    {
        _auth = auth; _nav = nav;
        LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => CanLogin);
    }

    private string _username = "";
    public string Username { get => _username; set { Set(ref _username, value); Update(); } }
    private string _password = "";
    public string Password { get => _password; set { Set(ref _password, value); Update(); } }
    private bool _busy;
    public bool IsBusy { get => _busy; set { Set(ref _busy, value); Update(); } }
    public string? Error { get; set; }
    public ICommand LoginCommand { get; }
    public bool CanLogin => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    private void Update() => (LoginCommand as RelayCommand)!.RaiseCanExecuteChanged();

    private async Task LoginAsync()
    {
        Error = null; Notify(nameof(Error));
        IsBusy = true;
        try
        {
            var session = await _auth.LoginAsync(Username, Password);
            if (session is null) { Error = "Invalid credentials."; Notify(nameof(Error)); return; }
            _nav.NavigateTo<DashboardViewModel>();
        }
        finally { IsBusy = false; }
    }
}
