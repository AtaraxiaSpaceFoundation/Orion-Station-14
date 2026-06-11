using Content.Client.Audio;
using Content.Shared._Orion.Radio;
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client._Orion.Radio;

public sealed class RadioBarkAudioSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PlayRadioBarkEvent>(OnPlayRadioBark);
    }

    private void OnPlayRadioBark(PlayRadioBarkEvent ev)
    {
        var multiplier = _cfg.GetCVar(CCVars.RadioVolume) * ContentAudioSystem.RadioMultiplier;
        var volume = SharedAudioSystem.GainToVolume(multiplier);
        var audioParams = ev.Params.WithVolume(volume);
        _audio.PlayEntity(new SoundPathSpecifier(ev.Path), Filter.Local(), GetEntity(ev.Source), false, audioParams);
    }
}
