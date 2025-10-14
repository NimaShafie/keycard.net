// Services/AppGlobals.cs
namespace KeyCard.Desktop.Services
{
    /// <summary>Static access for simple XAML bindings (e.g., IsMock).</summary>
    public static class AppGlobals
    {
        public static IAppEnvironment? Env { get; set; }
    }
}
