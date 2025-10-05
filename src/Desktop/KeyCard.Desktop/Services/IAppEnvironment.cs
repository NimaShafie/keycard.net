// Services/IAppEnvironment.cs
namespace KeyCard.Desktop.Services
{
    public interface IAppEnvironment
    {
        string EnvironmentName { get; }
        bool IsMock { get; }
        bool IsLive { get; }
        string ApiBaseUrl { get; }
        string BookingsHubUrl { get; }
    }
}
