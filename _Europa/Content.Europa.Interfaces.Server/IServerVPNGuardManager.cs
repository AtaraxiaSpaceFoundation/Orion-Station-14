using System.Net;

namespace Content.Europa.Interfaces.Server;

public interface IServerVPNGuardManager
{
    public void Initialize();
    public Task<bool> IsConnectionVpn(IPAddress ip);
}
