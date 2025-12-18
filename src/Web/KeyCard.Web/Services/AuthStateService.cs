namespace KeyCard.Web.Services;

public class AuthStateService
{
    public string? Token { get; private set; }
    public int UserId { get; private set; }
    public string? FullName { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public event Action? OnChange;

    public void SetAuth(string token, int userId, string fullName, string email, string role)
    {
        Token = token;
        UserId = userId;
        FullName = fullName;
        Email = email;
        Role = role;
        NotifyStateChanged();
    }

    public void Clear()
    {
        Token = null;
        UserId = 0;
        FullName = null;
        Email = null;
        Role = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

