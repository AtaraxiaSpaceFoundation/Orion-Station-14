using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._Orion.NPC.HTN.Operators.Specific;

/// <summary>
/// Picks a point away from a target entity and stores it as movement coordinates.
/// </summary>
public sealed partial class PickFleeCoordinatesOperator : HTNOperator
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private SharedTransformSystem _transform = default!;

    [DataField(required: true)]
    public string TargetKey = default!;

    [DataField]
    public string TargetCoordinatesKey = "TargetCoordinates";

    [DataField]
    public float FleeDistance = 6f;

    [DataField]
    public float RandomOffset = 1.5f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);

        _transform = sysManager.GetEntitySystem<SharedTransformSystem>();
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entManager) ||
            !_entManager.EntityExists(target) ||
            !_entManager.TryGetComponent<TransformComponent>(owner, out var ownerXform) ||
            !_entManager.TryGetComponent<TransformComponent>(target, out var targetXform))
            return HTNOperatorStatus.Failed;

        var ownerMap = _transform.ToMapCoordinates(ownerXform.Coordinates);
        var targetMap = _transform.ToMapCoordinates(targetXform.Coordinates);

        var direction = ownerMap.Position - targetMap.Position;

        direction = direction.LengthSquared() <= 0.001f
            ? _random.NextVector2()
            : direction.Normalized();

        var offset = direction * FleeDistance + _random.NextVector2(RandomOffset);
        var destination = ownerMap.Position + offset;

        if (ownerXform.MapUid is not { } mapUid)
            return HTNOperatorStatus.Failed;

        var destinationCoords = _transform.ToCoordinates(mapUid, new MapCoordinates(destination, ownerMap.MapId));

        blackboard.SetValue(TargetCoordinatesKey, destinationCoords);
        return HTNOperatorStatus.Finished;
    }
}
