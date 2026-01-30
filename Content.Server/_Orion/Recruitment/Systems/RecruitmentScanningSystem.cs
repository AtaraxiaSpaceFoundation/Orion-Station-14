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
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

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
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecruitmentScanningComponent, AfterInteractEvent>(OnScanAttempt);
        SubscribeLocalEvent<RecruitmentScanningComponent, RecruitmentScanningDoAfterEvent>(OnScanComplete);

        SubscribeLocalEvent<RecruitmentConfirmationComponent, RecruitmentAcceptMessage>(OnAccept);
        SubscribeLocalEvent<RecruitmentConfirmationComponent, RecruitmentDeclineMessage>(OnDecline);
    }

    private void OnScanAttempt(EntityUid uid, RecruitmentScanningComponent comp, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        var target = args.Target.Value;
        var targetName = Identity.Entity(target, EntityManager);
        _popup.PopupEntity(Loc.GetString("recruitment-start-user", ("target", targetName)), target, args.User);

        var userName = Identity.Entity(args.User, EntityManager);
        if (args.User != target)
            _popup.PopupEntity(Loc.GetString("recruitment-start-target", ("user", userName)), args.User, args.Target.Value, PopupType.LargeCaution);

        if (TryComp<ActorComponent>(target, out _))
        {
            var confirmComp = EnsureComp<RecruitmentConfirmationComponent>(target);
            confirmComp.Scanner = uid;
            confirmComp.Recruiter = args.User;
            confirmComp.OrganizationName = "InteQ";
            confirmComp.ImplantName = comp.Implant?.ToString() ?? "Unknown Implant";

            var state = new RecruitmentConfirmationBuiState
            {
                OrganizationName = confirmComp.OrganizationName,
                ImplantName = confirmComp.ImplantName,
            };

            _ui.SetUiState(uid, RecruitmentConfirmationUiKey.Key, state);
            _ui.TryOpenUi(uid, RecruitmentConfirmationUiKey.Key, target);
        }

        args.Handled = true;
    }

    private void OnAccept(EntityUid uid, RecruitmentConfirmationComponent comp, RecruitmentAcceptMessage args)
    {
        if (Deleted(comp.Scanner) || Deleted(comp.Recruiter) || !TryComp<RecruitmentScanningComponent>(comp.Scanner, out var scanComp))
        {
            _ui.CloseUi(uid, RecruitmentConfirmationUiKey.Key);
            RemComp<RecruitmentConfirmationComponent>(uid);

            return;
        }

        var targetXform = Transform(uid);
        var recruiterXform = Transform(comp.Recruiter);
        if (!targetXform.Coordinates.InRange(EntityManager, _transform, recruiterXform.Coordinates, 2f))
        {
            _popup.PopupEntity(Loc.GetString("recruitment-too-far"), comp.Scanner, comp.Recruiter);
            _ui.CloseUi(uid, RecruitmentConfirmationUiKey.Key);
            RemComp<RecruitmentConfirmationComponent>(uid);
            return;
        }

        _ui.CloseUi(uid, RecruitmentConfirmationUiKey.Key);
        RemComp<RecruitmentConfirmationComponent>(uid);

        var doAfter = new DoAfterArgs(EntityManager, comp.Recruiter, scanComp.DoAfterTime, new RecruitmentScanningDoAfterEvent(), comp.Scanner, uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDecline(EntityUid uid, RecruitmentConfirmationComponent comp, RecruitmentDeclineMessage args)
    {
        var targetName = Identity.Name(uid, EntityManager, comp.Recruiter);
        _popup.PopupEntity(Loc.GetString("recruitment-decline", ("target", targetName)), uid, comp.Recruiter);

        _ui.CloseUi(uid, RecruitmentConfirmationUiKey.Key);
        RemComp<RecruitmentConfirmationComponent>(uid);
    }

    private void OnScanComplete(EntityUid uid, RecruitmentScanningComponent comp, RecruitmentScanningDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;
        var name = Identity.Name(target, EntityManager, args.User);

        if (HasComp<RecruitedComponent>(target) || comp.ScannedEntities.Contains(target))
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

        var recruited = EnsureComp<RecruitedComponent>(target);
        recruited.Organization = "InteQ";
        recruited.RecruitedBy = args.User;
        recruited.RecruitedAt = _timing.CurTime;

        comp.ScannedEntities.Add(target);

        var success = Loc.GetString("recruitment-success", ("target", name));
        _popup.PopupEntity(success, target, args.User);

        _audio.PlayPvs(comp.SuccessSound, target, AudioParams.Default.WithVolume(-3f));
        _sparks.DoSparks(Transform(target).Coordinates, playSound: false);

        args.Handled = true;
    }
}
