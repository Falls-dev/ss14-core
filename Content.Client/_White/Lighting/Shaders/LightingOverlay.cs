﻿using System.Numerics;
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
        if (ScreenTexture == null)
            return;

        var xformCompQuery = _entityManager.GetEntityQuery<TransformComponent>();

        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(5f);

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var query = _entityManager.AllEntityQueryEnumerator<LightingOverlayComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var component, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            if (!component.Enabled)
                continue;

            var worldPos = _transformSystem.GetWorldPosition(xform, xformCompQuery);

            if (!bounds.Contains(worldPos))
                continue;

            var color = component.Color;

            var (_, _, worldMatrix) = xform.GetWorldPositionRotationMatrix(xformCompQuery);
            handle.SetTransform(worldMatrix);

            var mask = _spriteSystem.Frame0(component.Sprite); // mask

            var xOffset = component.Offsetx - (mask.Width / 2) / EyeManager.PixelsPerMeter;
            var yOffset = component.Offsety - (mask.Height / 2) / EyeManager.PixelsPerMeter;

            var textureVector = new Vector2(xOffset, yOffset);

            handle.DrawTexture(mask, textureVector, color);

            handle.UseShader(_shader);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3.Identity);
    }
}
