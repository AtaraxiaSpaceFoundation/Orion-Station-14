using Content.Server._Orion.Economy.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Orion.Economy.Systems;

public sealed class PaydayRuleSystem : GameRuleSystem<PaydayRuleComponent>
{
    [Dependency] private readonly PayrollSystem _payroll = default!;

    protected override void ActiveTick(EntityUid uid, PaydayRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        _payroll.UpdatePayroll();
    }
}
