// SPDX-FileCopyrightText: 2025 Coenx-flex
// SPDX-FileCopyrightText: 2025 Cojoke
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Orion.CorticalBorer.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CorticalBorerComponent : Component
{
    /// <summary>
    ///     Host of this Borer
    /// </summary>
    [ViewVariables]
    public EntityUid? Host = null;

    [DataField]
    public HashSet<EntityUid> WillingHosts = new();

    /// <summary>
    ///     How many eggs this borer has laid during the round.
    /// </summary>
    [DataField]
    public int EggsLaid;

    /// <summary>
    ///     Objectives assigned to the cortical borer role.
    /// </summary>
    [DataField]
    public List<EntProtoId> Objectives = new()
    {
        "CorticalBorerSurviveObjective",
        "CorticalBorerWillingHostsObjective",
        "CorticalBorerEggsObjective",
    };

    /// <summary>
    ///     Current number of chemical points
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ChemicalPoints = 50;

    /// <summary>
    ///     Chemicals added every second WHILE IN A HOST
    /// </summary>
    [DataField]
    public int ChemicalGenerationRate = 1;

    /// <summary>
    ///     Multiplier for chemical generation when the current host is a willing host.
    /// </summary>
    [DataField]
    public float WillingHostChemicalGenerationMultiplier = 2f;

    /// <summary>
    ///     Max Chemicals that can be held
    /// </summary>
    [DataField]
    public int ChemicalPointCap = 250;

    /// <summary>
    ///     Reagent injection amount
    /// </summary>
    public int InjectAmount = 10;

    /// <summary>
    ///     At what interval does the chem ui update
    /// </summary>
    public int UiUpdateInterval = 5; // Prevent constant update on cap

    /// <summary>
    ///     Healing applied to the borer every update tick while it is inside a host.
    /// </summary>
    [DataField]
    public DamageSpecifier HealingDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Blunt", -0.1 },
            { "Slash", -0.1 },
            { "Piercing", -0.1 },
            { "Heat", -0.1 },
            { "Shock", -0.1 },
            { "Cold", -0.1 },
            { "Poison", -0.1 },
            { "Radiation", -0.1 },
            { "Asphyxiation", -0.1 },
        },
    };

    /// <summary>
    ///     The max duration you can take control of your host
    /// </summary>
    [DataField]
    public TimeSpan ControlDuration = TimeSpan.FromSeconds(40);

    /// <summary>
    ///     Cooldown between chem regen events
    /// </summary>
    public TimeSpan UpdateTimer = TimeSpan.Zero;
    public float UpdateCooldown = 1f;

    /// <summary>
    ///     Can this borer be PREGNANT
    /// </summary>
    [DataField]
    public bool CanReproduce = true;

    /// <summary>
    ///     What does it vomit out of its mouth when it lays an egg
    /// </summary>
    [DataField]
    public string EggProto = "CorticalBorerEgg";

    /// <summary>
    ///     Cost to lay an egg... will not update ability desc if changed
    /// </summary>
    [DataField]
    public int EggCost = 200;

    /// <summary>
    ///     Duration of host paralysis from borer ability
    /// </summary>
    [DataField]
    public TimeSpan ParalyzeHostDuration = TimeSpan.FromSeconds(6);

    /// <summary>
    ///     Max forced host speech message length
    /// </summary>
    [DataField]
    public int MaxForceSpeakLength = 360;

    [DataField]
    public bool ControllingHost;

    [DataField]
    public ComponentRegistry? AddOnInfest;

    [DataField]
    public ComponentRegistry? RemoveOnInfest;

    [DataField]
    public ProtoId<AlertPrototype> ChemicalAlert = "Chemicals";

    [DataField]
    public ProtoId<AlertPrototype> SugarAlert = "BorerSugar";

    public readonly List<EntProtoId> InitialCorticalBorerActions = new()
    {
        "ActionBorerInfest",
        "ActionBorerEject",
        "ActionBorerInject",
        "ActionBorerCheckBlood",
        "ActionBorerLayEggHost",
        "ActionBorerControlHost",
        "ActionBorerForceSpeakHost",
        "ActionBorerParalyzeHost",
        "ActionBorerWillingHost",
    };
}
