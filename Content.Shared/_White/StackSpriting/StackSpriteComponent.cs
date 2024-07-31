using Robust.Shared.Utility;

namespace Content.Shared._White.StackSpriting;

[RegisterComponent]
public sealed partial class StackSpriteComponent : Component
{
    [DataField] public ResPath Path;
    [DataField] public Vector2i Size;
    [DataField] public Vector3? Center;
}
