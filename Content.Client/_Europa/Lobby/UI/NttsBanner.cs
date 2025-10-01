using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Europa.Lobby.UI;

public sealed class NttsBanner : BaseButton
{
    private Texture? _textureNormal;
    private string? _texturePath;

    public Texture? TextureNormal
    {
        get => _textureNormal;
        set
        {
            _textureNormal = value;
            InvalidateMeasure();
        }
    }

    public string TexturePath
    {
        set
        {
            _texturePath = value;
            if (_texturePath != null)
                TextureNormal = Theme.ResolveTexture(_texturePath);
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var texture = TextureNormal;
        if (texture == null)
            return;

        handle.DrawTextureRectRegion(texture, PixelSizeBox);
    }
}
