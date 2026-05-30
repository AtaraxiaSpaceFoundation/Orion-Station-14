using Content.Server.Emp;
using Content.Shared._Orion.Economy.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._Orion.Economy.Systems;

public sealed class CreditHolochipSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CreditHolochipComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<CreditHolochipComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CreditHolochipComponent, StackCountChangedEvent>(OnStackChanged);
    }

    private void OnStartup(Entity<CreditHolochipComponent> ent, ref ComponentStartup args)
    {
        UpdateVisualData(ent);
    }

    private void OnStackChanged(Entity<CreditHolochipComponent> ent, ref StackCountChangedEvent args)
    {
        UpdateVisualData(ent);
    }

    private void UpdateVisualData(Entity<CreditHolochipComponent> ent)
    {
        if (!TryComp<StackComponent>(ent, out var stack))
            return;

        _appearance.SetData(ent, CreditHolochipVisuals.BaseState, GetBaseColorStateForAmount(stack.Count));
        _appearance.SetData(ent, CreditHolochipVisuals.OverlayState, GetOverlayStateForAmount(stack.Count));
        _appearance.SetData(ent, CreditHolochipVisuals.BaseColor, GetColorForAmount(stack.Count));
    }

    private void OnEmpPulse(Entity<CreditHolochipComponent> ent, ref EmpPulseEvent args)
    {
        var power = MathF.Max(0.1f, (float) args.EnergyConsumption);
        var chance = Math.Clamp(0.6f / power, 0f, 1f);

        if (!_random.Prob(chance))
            return;

        _popup.PopupEntity(Loc.GetString("credit-holochip-emp-destroyed"), ent, PopupType.LargeCaution);
        QueueDel(ent);
        args.Affected = true;
    }

    private static string GetBaseColorStateForAmount(int amount)
    {
        return amount switch
        {
            >= 1_000_000_000 => "holochip_giga-color",
            >= 1_000_000 => "holochip_mega-color",
            >= 1_000 => "holochip_kilo-color",
            _ => "holochip-color",
        };
    }

    private static string GetOverlayStateForAmount(int amount)
    {
        return amount switch
        {
            >= 1_000_000_000 => "holochip_giga",
            >= 1_000_000 => "holochip_mega",
            >= 1_000 => "holochip_kilo",
            _ => "holochip",
        };
    }

    private static Color GetColorForAmount(int amount)
    {
        var rounded = GetRoundedDisplayAmount(amount);
        return rounded switch
        {
            <= 4 => Color.FromHex("#8E2E38"),
            <= 9 => Color.FromHex("#914792"),
            <= 19 => Color.FromHex("#BF5E0A"),
            <= 49 => Color.FromHex("#358F34"),
            <= 99 => Color.FromHex("#8A8A8A"),
            <= 199 => Color.FromHex("#009D9B"),
            <= 499 => Color.FromHex("#0153C1"),
            _ => Color.FromHex("#2C2C2C"),
        };
    }

    private static int GetRoundedDisplayAmount(int amount)
    {
        return amount switch
        {
            >= 1_000_000_000 => (int) MathF.Round(amount / 1_000_000_000f),
            >= 1_000_000 => (int) MathF.Round(amount / 1_000_000f),
            >= 1_000 => (int) MathF.Round(amount / 1_000f),
            _ => amount,
        };
    }
}
