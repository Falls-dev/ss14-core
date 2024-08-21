using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._White.Lighting.Shaders;

/// <summary>
/// This is used for LightOverlay
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class LightingOverlayComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier Sprite = new Texture(new ResPath("Effects/LightMasks/shadermask.png"));

    [DataField, AutoNetworkedField]
    public float Offsetx = -0.5F;

    [DataField, AutoNetworkedField]
    public float Offsety = 0.5F;
}
