using Content.Client.Audio;
using Content.Shared._Orion.Radio;
using Content.Shared.CCVar;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;

namespace Content.Client._Orion.Radio;
public sealed class RadioBarkAudioSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioBarkAudioComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, RadioBarkAudioComponent _, ComponentInit args)
    {
        if (!TryComp<AudioComponent>(uid, out var audio))
            return;

        var multiplier = _cfg.GetCVar(CCVars.RadioVolume) * ContentAudioSystem.RadioMultiplier;
        _audio.SetGain(uid, audio.Gain * multiplier, audio);
    }
}
