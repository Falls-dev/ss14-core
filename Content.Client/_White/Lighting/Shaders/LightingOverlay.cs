using System.Numerics;
using Content.Shared._White.Lighting.Shaders;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._White.Lighting.Shaders;

public sealed class LightingOverlay : Overlay
{
    private readonly IPrototypeManager _prototypeManager;
    private readonly EntityManager _entityManager;
    private readonly SpriteSystem _spriteSystem;
    private readonly TransformSystem _transformSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;

    public LightingOverlay(EntityManager entityManager, IPrototypeManager prototypeManager)
    {
        _entityManager = entityManager;
        _spriteSystem = entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _prototypeManager = prototypeManager;
        _transformSystem = entityManager.EntitySysManager.GetEntitySystem<TransformSystem>();
        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index<ShaderPrototype>("LightingOverlay").InstanceUnique();
        ZIndex = (int) DrawDepth.Overdoors;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if(ScreenTexture == null)
            return;

        var lightCompQuery = _entityManager.GetEntityQuery<PointLightComponent>();
        var xformCompQuery = _entityManager.GetEntityQuery<TransformComponent>();

        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(5f);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var query = _entityManager.AllEntityQueryEnumerator<LightingOverlayComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var component, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            var worldPos = _transformSystem.GetWorldPosition(xform, xformCompQuery);

            if(!bounds.Contains(worldPos))
                continue;

            Color color = Color.White;
            SpriteSpecifier sprite = component.Sprite;

            var (_, _, worldMatrix) = xform.GetWorldPositionRotationMatrix(xformCompQuery);
            handle.SetTransform(worldMatrix);

            Texture texture = _spriteSystem.Frame0(sprite);
            float xOffset = component.Offsetx - (texture.Width / 2) / EyeManager.PixelsPerMeter;
            float yOffset = component.Offsety - (texture.Height / 2) / EyeManager.PixelsPerMeter;

            Vector2 textureVector = new Vector2(xOffset, yOffset);

            handle.DrawTexture(texture, textureVector, color);

            handle.UseShader(_shader);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
    }
}
