namespace KeyCard.BusinessLogic.ViewModels.Auth
{
    public record AuthResultViewModel(
        Guid UserId,
        string FullName,
        string Email,
        string Role
    );
}
