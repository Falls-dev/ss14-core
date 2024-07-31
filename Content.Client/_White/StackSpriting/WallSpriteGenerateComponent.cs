namespace Content.Client._White.StackSpriting;

[RegisterComponent]
public sealed partial class WallSpriteGenerateComponent : Component
{
    public Vector2i Size;
    public int Height;
    public Robust.Client.Graphics.Texture Texture = default!;
}
