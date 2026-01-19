using Content.Shared._Orion.Chat.Components;
using Content.Shared.CCVar;
using Content.Shared.Verbs;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Client._Orion.Chat.Systems.DirectionalEmote;

public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _directionalEmotesEnabled;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.DirectionalEmotesEnabled, v => _directionalEmotesEnabled = v, true);

        SubscribeLocalEvent<DirectionalEmoteTargetComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    public void ShowMessage(NetEntity source, NetEntity target, string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var maxLength = _cfg.GetCVar(CCVars.ChatMaxEmoteLength);
        if (text.Length > maxLength)
            return;

        RaiseNetworkEvent(new SendDirectionalEmoteEvent(source, target, text));
    }

    private void OnGetVerbs(EntityUid uid, DirectionalEmoteTargetComponent component, GetVerbsEvent<Verb> args)
    {
        if (!_directionalEmotesEnabled)
            return;

        if (args.Target == args.User ||
            !HasComp<DirectionalEmoteTargetComponent>(args.User))
            return;

        args.Verbs.Add(new Verb
        {
            Act = () => OpenWindow(GetNetEntity(args.User), GetNetEntity(args.Target)),
            Priority = 15,
            Text = Loc.GetString("directional-emote-verb-name"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/emotes.svg.192dpi.png")),
            ClientExclusive = true,
        });
    }

    private void OpenWindow(NetEntity source, NetEntity target)
    {
        _userInterfaceManager.GetUIController<DirectionalEmoteUIController>().ToggleWindow(source, target);
    }
}
