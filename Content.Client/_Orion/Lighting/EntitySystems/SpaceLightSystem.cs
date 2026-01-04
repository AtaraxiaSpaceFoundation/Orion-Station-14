using System.Linq;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client._Orion.Lighting.EntitySystems;

/// <summary>
///     System that handles space ambient light.
/// </summary>
public sealed class SpaceLightSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eye = default!;

    private const string SpaceTileId = "Space";
    private EntityUid? _mapEntity;
    private Color _defaultColor = Color.Blue;
    private bool _isInSpace;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MapLightComponent, ComponentInit>(OnMapLightInit);
        SubscribeLocalEvent<MapLightComponent, AfterAutoHandleStateEvent>(OnMapLightUpdate);
    }

    private void OnMapLightInit(EntityUid uid, MapLightComponent component, ComponentInit args)
    {
        _mapEntity = uid;
        _defaultColor = component.AmbientLightColor;
    }

    private void OnMapLightUpdate(EntityUid uid, MapLightComponent component, AfterAutoHandleStateEvent args)
    {
        _mapEntity = uid;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_mapEntity == null || !_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<MapLightComponent>(_mapEntity.Value, out var mapLight))
            return;

        var playerPos = _eye.CurrentEye.Position;
        var grid = GetMapGrid(playerPos.MapId);

        if (grid == null)
            return;

        var tile = grid.GetTileRef(playerPos);
        var inSpace = tile.Tile.TypeId == tile.Definition.ID && tile.Definition.ID == SpaceTileId;

        switch (inSpace)
        {
            case true when !_isInSpace:
                _isInSpace = true;
                mapLight.AmbientLightColor = new Color(0.02f, 0.02f, 0.06f);
                Dirty(_mapEntity.Value, mapLight);
                break;
            case false when _isInSpace:
                _isInSpace = false;
                mapLight.AmbientLightColor = _defaultColor;
                Dirty(_mapEntity.Value, mapLight);
                break;
        }
    }

    private IMapGrid? GetMapGrid(MapId mapId)
    {
        return EntityManager.EntityQuery<MapGridComponent>()
            .FirstOrDefault(c => c.Parent.MapID == mapId)
            ?.Grid;
    }
}
