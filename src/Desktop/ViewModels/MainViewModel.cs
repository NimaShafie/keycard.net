namespace KeyCard.Desktop.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private ViewModelBase _current = null!;
    public ViewModelBase Current { get => _current; set => Set(ref _current, value); }
}
