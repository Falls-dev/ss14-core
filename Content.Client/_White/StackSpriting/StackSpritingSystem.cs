using Content.Shared._White.StackSpriting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._White.StackSpriting;

public sealed class StackSpritingSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    public override void Initialize()
    {
        _overlayManager.AddOverlay(new StackSpritingOverlay());
        SubscribeLocalEvent<StackSpriteComponent,ComponentInit>(OnInit);
        SubscribeLocalEvent<WallSpriteGenerateComponent,ComponentInit>(OnGenInit);
    }

    private void OnInit(EntityUid uid, StackSpriteComponent stackSpriteComponent, ref ComponentInit args)
    {
        var texture = _resourceCache.GetResource<TextureResource>(stackSpriteComponent.Path).Texture;
        var count = texture.Width / stackSpriteComponent.Size.X * texture.Height / stackSpriteComponent.Size.Y;
        var renderer = EnsureComp<RendererStackSpriteComponent>(uid);
        renderer.Size = stackSpriteComponent.Size;
        renderer.Texture = texture;
        renderer.Height = count;
        renderer.Center = stackSpriteComponent.Center;
    }

    private void OnGenInit(EntityUid uid, WallSpriteGenerateComponent component, ComponentInit args)
    {
        var renderer = EnsureComp<RendererStackSpriteComponent>(uid);
        renderer.Size = component.Size;
        renderer.Texture = component.Texture;
        renderer.Height = component.Height;
    }
}
