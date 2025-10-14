using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KeyCard.Infrastructure.Hubs.RealTime
{


    public sealed class ConnectionRegistry
    {
        private readonly Dictionary<string, HashSet<string>> _userToConns = new();
        private readonly object _lock = new();
        public void Add(string userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_userToConns.TryGetValue(userId, out var set)) _userToConns[userId] = set = new();
                set.Add(connectionId);
            }
        }
        public void Remove(string userId, string connectionId)
        {
            lock (_lock)
            {
                if (_userToConns.TryGetValue(userId, out var set))
                {
                    set.Remove(connectionId);
                    if (set.Count == 0) _userToConns.Remove(userId);
                }
            }
        }
        public IReadOnlyCollection<string> GetConnections(string userId)
            => _userToConns.TryGetValue(userId, out var set) ? set : Array.Empty<string>();
    }

    public sealed class NameIdUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext ctx) =>
            ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User?.FindFirstValue("sub");
    }

    [Authorize]
    public abstract class KeyCardHubBase<TClient> : Hub<TClient> where TClient : class
    {
        private readonly ConnectionRegistry _registry;
        protected KeyCardHubBase(ConnectionRegistry registry) => _registry = registry;

        protected static string? Claim(ClaimsPrincipal u, string type) => u.FindFirst(type)?.Value;

        public override async Task OnConnectedAsync()
        {
            var uid = Context.UserIdentifier;
            if (!string.IsNullOrWhiteSpace(uid))
            {
                _registry.Add(uid, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{uid}");

                foreach (var role in Context.User!.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");

                var hotelId = Claim(Context.User!, "hotelId");
                if (!string.IsNullOrEmpty(hotelId))
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"hotel:{hotelId}");

                var bookingId = Claim(Context.User!, "bookingId");
                if (!string.IsNullOrEmpty(bookingId))
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"booking:{bookingId}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            var uid = Context.UserIdentifier;
            if (!string.IsNullOrWhiteSpace(uid))
                _registry.Remove(uid, Context.ConnectionId);
            await base.OnDisconnectedAsync(ex);
        }
    }

}
