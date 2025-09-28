// Models/UserSession.cs
namespace KeyCard.Desktop.Models;

public record UserSession(string UserId, string DisplayName, string JwtToken);
