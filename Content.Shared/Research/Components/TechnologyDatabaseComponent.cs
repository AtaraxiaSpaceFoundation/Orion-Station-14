// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 BombasterDS <115770678+BombasterDS@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 SX_7 <sn1.test.preria.2002@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Orion.Research.Prototypes;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Research.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedResearchSystem), typeof(SharedLatheSystem)), AutoGenerateComponentState]
public sealed partial class TechnologyDatabaseComponent : Component
{
    /// <summary>
    /// A main discipline that locks out other discipline technology past a certain tier.
    /// </summary>
    [AutoNetworkedField]
    [DataField("mainDiscipline", customTypeSerializer: typeof(PrototypeIdSerializer<TechDisciplinePrototype>))]
    public string? MainDiscipline;

    [AutoNetworkedField]
    [DataField("currentTechnologyCards")]
    public List<string> CurrentTechnologyCards = new();

    /// <summary>
    /// Which research disciplines are able to be unlocked
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<ProtoId<TechDisciplinePrototype>> SupportedDisciplines = new();

    /// <summary>
    /// Technologies that are currently known (including researched ones).
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<ProtoId<TechnologyPrototype>> VisibleTechnologies = new(); // Orion-Edit: Was UnlockedTechnologies

    // Orion-Start
    /// <summary>
    /// Technologies that can be researched right now.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<ProtoId<TechnologyPrototype>> AvailableTechnologies = new();

    /// <summary>
    /// Technologies that were researched by this database.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<ProtoId<TechnologyPrototype>> ResearchedTechnologies = new();

    /// <summary>
    /// Experiments currently available for this database.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<string> AvailableExperiments = new();

    /// <summary>
    /// Experiments forcibly unlocked by reward effects.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<string> UnlockedExperiments = new();

    /// <summary>
    /// Experiments currently active and progressable.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<string> ActiveExperiments = new();

    /// <summary>
    /// Experiments completed by this database.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<string> CompletedExperiments = new();

    /// <summary>
    /// Progress state by experiment id.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<ResearchExperimentProgress> ExperimentProgress = new();

    /// <summary>
    /// Experiments that were explicitly skipped.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<string> SkippedExperiments = new();
    // Orion-End

    /// <summary>
    /// The ids of all the lathe recipes which have been unlocked.
    /// This is maintained alongside researched technologies.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<ProtoId<LatheRecipePrototype>> UnlockedRecipes = new();

    // Orion-Start
    /// <summary>
    /// Technologies revealed by non-standard effects (e.g. experiments).
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<ProtoId<TechnologyPrototype>> RevealedTechnologies = new();

    /// <summary>
    /// Discovery requirement progress for technology reveal conditions.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<TechnologyDiscoveryProgress> DiscoveryProgress = new();

    /// <summary>
    /// Infrastructure categories currently unlocked for this network.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<string> UnlockedInfrastructure = new();
    // Orion-End
}

/// <summary>
/// Event raised on the database whenever its
/// technologies or recipes are modified.
/// </summary>
/// <remarks>
/// This event is forwarded from the
/// server to all of it's clients.
/// </remarks>
[ByRefEvent]
public readonly record struct TechnologyDatabaseModifiedEvent // Goobstation - Lathe message on recipes update
{
    public readonly List<ProtoId<LatheRecipePrototype>> UnlockedRecipes;

    public TechnologyDatabaseModifiedEvent(List<ProtoId<LatheRecipePrototype>>? unlockedRecipes = null)
    {
        UnlockedRecipes = unlockedRecipes ?? new();
    }
};

/// <summary>
/// Event raised on a database after being synchronized
/// with the values from another database.
/// </summary>
[ByRefEvent]
public readonly record struct TechnologyDatabaseSynchronizedEvent;

// Orion-Start
[DataDefinition, Serializable, NetSerializable]
public partial record struct ResearchExperimentProgress
{
    [DataField]
    public ProtoId<ResearchExperimentPrototype> ExperimentId;

    [DataField]
    public int Progress;

    [DataField]
    public int Target;

    [DataField]
    public HashSet<string> UniqueProgressKeys = new();

    [DataField]
    public HashSet<NetEntity> ScannedEntities = new();

    [DataField]
    public TimeSpan? CompletedAt;
}

[DataDefinition, Serializable, NetSerializable]
public partial record struct TechnologyDiscoveryProgress
{
    [DataField]
    public ProtoId<TechnologyPrototype> TechnologyId;

    [DataField]
    public string RequirementId = string.Empty;

    [DataField]
    public int Progress;

    [DataField]
    public int Target;

    [DataField]
    public TimeSpan? CompletedAt;
}
// Orion-End
