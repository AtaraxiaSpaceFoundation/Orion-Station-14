using Content.Server.Database;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._Chaos.Discord;

public sealed class DiscordAuthManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    private readonly HttpClient _httpClient = new();
    private ISawmill _sawmill = default!;
    private int _encryptionKey;
    private string _botIp = "";
    private int _botPort;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("discord_auth");

        _encryptionKey = _cfg.GetCVar(CCVars.DiscordAuthEncryptionKey);
        if (_encryptionKey == 0)
            throw new Exception("DiscordAuthEncryptionKey is not configured.");

        _botIp = _cfg.GetCVar(CCVars.DiscordAuthBotIp);
        _botPort = _cfg.GetCVar(CCVars.DiscordAuthBotPort);
    }

    public Task<string?> GetDiscordId(Guid userId)
    {
        var testValue = GenerateUserCode(userId);
        _sawmill.Warning($"Generated code for user {userId}: {testValue}");
        return _dbManager.GetDiscordIdAsync(userId);
    }

    public int GenerateUserCode(Guid userId)
    {
        var input = userId.ToString() + _encryptionKey;
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        var value = BitConverter.ToInt32(hash, 0);
        value = Math.Abs(value);
        return 10000000 + (value % 90000000);
    }

    public async Task SendAuthCodeToBot(Guid userId, int code, string token)
    {
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new {
                userId,
                code,
                token }),
            Encoding.UTF8,
            "application/json");
        await _httpClient.PostAsync($"http://{_botIp}:{_botPort}/auth", content); // Простите за хардкод ^_^, но я не хочу городить отдельный конфиг для одного эндпоинта бота
    }
}
