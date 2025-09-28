//Services/RetryDelegatingHandler.cs
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Polly;

namespace KeyCard.Desktop.Services;

public sealed class RetryDelegatingHandler : DelegatingHandler
{
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public RetryDelegatingHandler(IAsyncPolicy<HttpResponseMessage> policy)
        => _policy = policy;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _policy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
}
