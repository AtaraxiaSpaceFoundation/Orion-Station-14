using Content.Europa.Interfaces.Shared;

namespace Content.Europa.Interfaces.Client;

public interface IClientDiscordAuthManager : ISharedDiscordAuthManager
{
    public string AuthUrl { get; }
}
