using KeyCard.Desktop.ViewModels;

namespace KeyCard.Desktop.Services;

public interface INavigationService
{
    ViewModelBase Current { get; }
    void NavigateTo<T>() where T : ViewModelBase;
}
