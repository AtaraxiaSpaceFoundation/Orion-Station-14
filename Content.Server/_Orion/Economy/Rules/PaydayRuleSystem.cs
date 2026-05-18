using Content.Server._Orion.Economy.Rules.Components;
using Content.Server._Orion.Economy.Systems;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Timing;

namespace Content.Server._Orion.Economy.Rules;

public sealed class PaydayRuleSystem : GameRuleSystem<PaydayRuleComponent>
{
    [Dependency] private readonly PayrollSystem _payroll = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    protected override void Started(EntityUid uid, PaydayRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        component.NextPayday = _timing.CurTime + component.Interval;
    }

    protected override void ActiveTick(EntityUid uid, PaydayRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (_timing.CurTime < component.NextPayday)
            return;

        component.NextPayday = _timing.CurTime + component.Interval;
        _payroll.ProcessPayroll();
    }
}
