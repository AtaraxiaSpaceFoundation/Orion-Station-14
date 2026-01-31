using Content.Server._Orion.ServerProtection.Chat;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared._Orion.Chat.Components;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Examine;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Chat.Systems;

public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ChatProtectionSystem _chatProtection = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _directionalEmotesEnabled;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.DirectionalEmotesEnabled, v => _directionalEmotesEnabled = v, true);

        SubscribeNetworkEvent<SendDirectionalEmoteEvent>(OnSendDirectionalEmoteEvent);
    }

    private void OnSendDirectionalEmoteEvent(SendDirectionalEmoteEvent args, EntitySessionEventArgs session)
    {
        if (!_directionalEmotesEnabled)
            return;

        if (string.IsNullOrWhiteSpace(args.Text))
            return;

        var source = session.SenderSession.AttachedEntity;
        if (source == null)
            return;

        var target = GetEntity(args.Target);

        if (!TryComp<ActorComponent>(source.Value, out var sourceActor) ||
            !TryComp<ActorComponent>(target, out var targetActor) ||
            !TryComp<DirectionalEmoteTargetComponent>(source.Value, out var directEmote))
            return;

        var curTime = _gameTiming.CurTime;
        if (directEmote.LastSend + directEmote.Cooldown > curTime)
            return;

        var directEmoteRange = _cfg.GetCVar(CCVars.DirectionalEmoteRange);
        var rangeError = Loc.GetString("directional-emote-range-error");
        if (!_examineSystem.InRangeUnOccluded(source.Value, target, directEmoteRange))
        {
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, rangeError, rangeError, default, false, sourceActor.PlayerSession.Channel);
            return;
        }

        var maxLength = _cfg.GetCVar(CCVars.ChatMaxEmoteLength);
        if (args.Text.Length > maxLength)
        {
            var lengthError = Loc.GetString("directional-emote-length-error", ("maxLength", maxLength));
            _chatManager.ChatMessageToOne(ChatChannel.Emotes, lengthError, lengthError, default, false, sourceActor.PlayerSession.Channel);
            return;
        }

        if (_chatProtection.CheckICMessage(args.Text, source.Value))
            return;

        var wrappedMessage = Loc.GetString("chat-manager-entity-directional-emote-wrap-message", ("entityName", MetaData(source.Value).EntityName), ("message", args.Text));

        _chatManager.ChatMessageToMany(ChatChannel.Emotes, args.Text, wrappedMessage, source.Value, false, true, [targetActor.PlayerSession.Channel, sourceActor.PlayerSession.Channel]);
        directEmote.LastSend = curTime;
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(source):source} send directional emote to {ToPrettyString(target):target}: {args.Text}");
    }
}
