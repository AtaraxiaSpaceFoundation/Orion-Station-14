using Content.Client.Audio;
using Content.Shared._Orion.Radio;
using Content.Shared.CCVar;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._Orion.Radio;
public sealed class RadioBarkAudioSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioBarkAudioComponent, ComponentHandleState>(OnHandleState);
        Subs.CVar(_cfg, CCVars.RadioVolume, _ => UpdateAllGains());
    }

    private void OnHandleState(EntityUid uid, RadioBarkAudioComponent _, ComponentHandleState args)
    {
        if (!TryComp<AudioComponent>(uid, out var audio))
            return;

        var multiplier = _cfg.GetCVar(CCVars.RadioVolume) * ContentAudioSystem.RadioMultiplier;
        _audio.SetGain(uid, multiplier, audio);
    }

    private void UpdateAllGains()
    {
        var query = EntityQueryEnumerator<RadioBarkAudioComponent, AudioComponent>();
        while (query.MoveNext(out var uid, out _, out var audio))
        {
            var multiplier = _cfg.GetCVar(CCVars.RadioVolume) * ContentAudioSystem.RadioMultiplier;
            _audio.SetGain(uid, multiplier, audio);
        }
    }
}
