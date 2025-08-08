using Microsoft.AspNetCore.SignalR;

namespace ea_Tracker.Hubs
{
    /// <summary>
    /// SignalR hub for broadcasting investigation lifecycle and result events to clients.
    /// </summary>
    public class InvestigationHub : Hub
    {
        // Intentionally empty; we only broadcast server-to-clients via IHubContext
    }
}


