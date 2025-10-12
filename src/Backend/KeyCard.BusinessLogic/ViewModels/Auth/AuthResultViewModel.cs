namespace KeyCard.BusinessLogic.ViewModels.Auth
{
    public record AuthResultViewModel(
        int UserId,
        string FullName,
        string Email,
        string Role
    );
}
