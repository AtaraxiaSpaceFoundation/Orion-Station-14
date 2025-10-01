using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Goobstation.UIKit.UserInterface.RichText;

public sealed class RadioIconTag : IMarkupTag
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public string Name => "radicon";

    private readonly string JobIconNoId = "JobIconNoId";

    public bool TryGetControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Attributes.TryGetValue("icon", out var icon) || !icon.TryGetString(out var iconId))
            return false;

        if (!node.Attributes.TryGetValue("scale", out var scale) || !scale.TryGetLong(out var scaleValue))
        {
            scaleValue = 1;
        }

        var jobIcon = _prototypeManager.Index<JobIconPrototype>(JobIconNoId);

        if (_prototypeManager.TryIndex<JobIconPrototype>(iconId, out var proto))
            jobIcon = proto;

        var spriteSystem = _entitySystemManager.GetEntitySystem<SpriteSystem>();

        var texture = new TextureRect();
        texture.Texture = spriteSystem.GetFrame(jobIcon.Icon, _timing.CurTime);
        texture.TextureScale = new Vector2(scaleValue.Value, scaleValue.Value);

        control = texture;
        return true;
    }
}
