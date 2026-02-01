using Content.Goobstation.Common.Effects;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared._Orion.Recruitment;
using Content.Shared._Orion.Recruitment.Components;
using Content.Shared.Database;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Recruitment.Systems;

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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecruitedComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<RecruitmentScanningComponent, AfterInteractEvent>(OnScanAttempt);
        SubscribeLocalEvent<RecruitmentScanningComponent, RecruitmentScanningDoAfterEvent>(OnScanComplete);

        SubscribeLocalEvent<RecruitmentScanningComponent, RecruitmentAcceptMessage>(OnAccept);
        SubscribeLocalEvent<RecruitmentScanningComponent, RecruitmentDeclineMessage>(OnDecline);
    }

    private void OnMapInit(EntityUid uid, RecruitedComponent comp, MapInitEvent args)
    {
        if (comp.RecruitedAt != TimeSpan.Zero)
            return;

        comp.RecruitedAt = _timing.CurTime;
    }

    // TODO: Fix UI doesn't open for target on scan
    private void OnScanAttempt(EntityUid uid, RecruitmentScanningComponent comp, AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        var target = args.Target.Value;

        if (!TryComp<ActorComponent>(target, out var actor))
            return;

        // This will be until we fix UI, also remove this when UI is fixed
        var targetName = Identity.Name(target, EntityManager);
        _popup.PopupEntity(Loc.GetString("recruitment-start-user-temporal-popup", ("target", targetName)), target, args.User);

/* Doesn't open the UI for the target :(
// Uncomment this when UI is fixed
        var targetName = Identity.Name(target, EntityManager);
        _popup.PopupEntity(Loc.GetString("recruitment-start-user", ("target", targetName)), target, args.User);

        var userName = Identity.Name(args.User, EntityManager);
        if (args.User != target)
            _popup.PopupEntity(Loc.GetString("recruitment-start-target", ("user", userName)), args.User, target, PopupType.LargeCaution);
*/

        var confirmComp = EnsureComp<RecruitmentConfirmationComponent>(uid);
        confirmComp.Scanner = uid;
        confirmComp.Target = target;
        confirmComp.Recruiter = args.User;
        confirmComp.OrganizationName = comp.OrganizationName;

        var implantName = Loc.GetString("recruitment-member-list-unknown");
        if (comp.Implant != null && _prototypeManager.TryIndex<EntityPrototype>(comp.Implant.Value, out var implantProto))
        {
            implantName = implantProto.Name;
        }
        confirmComp.ImplantName = implantName;

        var state = new RecruitmentConfirmationBuiState
        {
            OrganizationName = confirmComp.OrganizationName,
            ImplantName = confirmComp.ImplantName,
        };

        _ui.SetUiState(uid, RecruitmentConfirmationUiKey.Key, state);
        _ui.OpenUi(uid, RecruitmentConfirmationUiKey.Key, actor.PlayerSession);

        args.Handled = true;
    }

    private void OnAccept(EntityUid uid, RecruitmentScanningComponent scanComp, RecruitmentAcceptMessage args)
    {
        if (!TryComp<RecruitmentConfirmationComponent>(uid, out var confirmComp))
            return;

        var target = confirmComp.Target;

        if (Deleted(target) || Deleted(confirmComp.Recruiter))
        {
            _ui.CloseUi(uid, RecruitmentConfirmationUiKey.Key);
            RemComp<RecruitmentConfirmationComponent>(uid);
            return;
        }

        var targetXform = Transform(target);
        var recruiterXform = Transform(confirmComp.Recruiter);
        if (!_transform.InRange(recruiterXform.Coordinates, targetXform.Coordinates, 2f))
        {
            _popup.PopupEntity(Loc.GetString("recruitment-too-far"), uid, confirmComp.Recruiter);
            _ui.CloseUi(uid, RecruitmentConfirmationUiKey.Key);
            RemComp<RecruitmentConfirmationComponent>(uid);
            return;
        }

        _ui.CloseUi(uid, RecruitmentConfirmationUiKey.Key);
        RemComp<RecruitmentConfirmationComponent>(uid);

        var doAfter = new DoAfterArgs(EntityManager, confirmComp.Recruiter, scanComp.DoAfterTime, new RecruitmentScanningDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDecline(EntityUid uid, RecruitmentScanningComponent scanComp, RecruitmentDeclineMessage args)
    {
        if (!TryComp<RecruitmentConfirmationComponent>(uid, out var confirmComp))
            return;

        if (args.Actor != confirmComp.Target)
            return;

        var targetName = Identity.Name(confirmComp.Target, EntityManager, confirmComp.Recruiter);
        _popup.PopupEntity(Loc.GetString("recruitment-decline", ("target", targetName)), uid, confirmComp.Recruiter);

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
        recruited.Organization = comp.OrganizationName;
        recruited.RecruitedBy = Identity.Name(args.User, EntityManager);
        recruited.RecruitedAt = _timing.CurTime;

        comp.ScannedEntities.Add(target);

        var success = Loc.GetString("recruitment-success", ("target", name));
        _popup.PopupEntity(success, target, args.User);

        _audio.PlayPvs(comp.SuccessSound, target, AudioParams.Default.WithVolume(-3f));
        _sparks.DoSparks(Transform(target).Coordinates, playSound: false);

        var recruiterName = Identity.Name(args.User, EntityManager);
        _adminLogger.Add(LogType.Mind, LogImpact.High, $"{recruiterName} recruited {name} to {comp.OrganizationName} with implant {comp.Implant} and faction {comp.Faction}");

        args.Handled = true;
    }
}
