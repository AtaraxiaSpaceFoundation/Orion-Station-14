using Content.Goobstation.Common.Effects;
using Content.Server.Popups;
using Content.Shared._Orion.Recruitment;
using Content.Shared._Orion.Recruitment.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Interaction;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Orion.Recruitment.Systems;

// TODO: Do this like interface with all organization members or something like that
public sealed class RecruitmentScanningSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implantSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SparksSystem _sparks = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecruitmentScanningComponent, AfterInteractEvent>(OnScanAttempt);
        SubscribeLocalEvent<RecruitmentScanningComponent, RecruitmentScanningDoAfterEvent>(OnScanComplete);
    }

    // TODO: Add something like window with "Someone invites you to join the organization" "Accept" "Decline"
    private void OnScanAttempt(EntityUid uid, RecruitmentScanningComponent comp, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        var targetName = Identity.Entity(args.Target.Value, EntityManager);
        _popup.PopupEntity(Loc.GetString("recruitment-start-user", ("target", targetName)), args.Target.Value, args.User);

        var userName = Identity.Entity(args.User, EntityManager);
        if (args.User != args.Target.Value)
            _popup.PopupEntity(Loc.GetString("recruitment-start-target", ("user", userName)), args.User, args.Target.Value, PopupType.LargeCaution);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, comp.DoAfterTime, new RecruitmentScanningDoAfterEvent(), uid, args.Target, uid) { NeedHand = true, BreakOnMove = true });
    }

    // TODO: Add effects on recruited entity
    private void OnScanComplete(EntityUid uid, RecruitmentScanningComponent comp, RecruitmentScanningDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;
        var name = Identity.Name(target, EntityManager, args.User);

        if (comp.ScannedEntities.Contains(target))
        {
            var msg = Loc.GetString("recruitment-already", ("target", name));
            _popup.PopupEntity(msg, target, args.User);

            return;
        }

        if (comp.Whitelist is not null && _whitelist.IsWhitelistFail(comp.Whitelist, target))
        {
            var msg = Loc.GetString("recruitment-failed", ("target", name));
            _popup.PopupEntity(msg, target, args.User);

            return;
        }

        if (comp.Implant is not null)
            _implantSystem.AddImplant(target, comp.Implant.Value);

        if (comp.Faction is not null)
        {
            var npcFaction = EnsureComp<NpcFactionMemberComponent>(target);
            _npcFaction.AddFaction((target, npcFaction), comp.Faction);
        }

        comp.ScannedEntities.Add(target);

        var success = Loc.GetString("recruitment-success", ("target", name));
        _popup.PopupEntity(success, target, args.User);

        _audio.PlayPvs(comp.SuccessSound, target, AudioParams.Default.WithVolume(-3f));
        _sparks.DoSparks(Transform(target).Coordinates, playSound: false);

        args.Handled = true;
    }
}
