using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._White.StackSpriting;

public sealed class StackSpritingManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = Logger.GetSawmill("StackSprite");
        InitializePrototypes();
    }

    private void InitializePrototypes()
    {
        _sawmill.Info("Starting generating spritestack...");
        var sw = new Stopwatch();
        sw.Start();
        foreach (var entityPrototype in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (   entityPrototype.TryGetComponent<WallSpriteGenerateComponent>(out var wallSpriteGenerateComponent)
                && entityPrototype.TryGetComponent<SpriteComponent>(out var spriteComponent)
                && entityPrototype.TryGetComponent<IconComponent>(out var iconComponent))
            {
                _sawmill.Info("Preparing: " + entityPrototype.Name);
                var texture = iconComponent.Icon.Frame0();
                var stackHeight = texture.Width;
                var img = new Image<Rgba32>(texture.Width, texture.Height * stackHeight);

                for (var i = 0; i < stackHeight; i++)
                {
                    for (var x = 0; x < texture.Width - 1; x++)
                    {
                        for (var y = 0; y < texture.Height - 1; y++)
                        {
                            _sawmill.Info($"SOME SHIT {x} {y} {i}");
                            img[x, y + i * texture.Height] = texture[x,y].ConvertImgSharp();
                        }
                    }
                }

                wallSpriteGenerateComponent.Texture = _clyde.LoadTextureFromImage(img);
                wallSpriteGenerateComponent.Size = texture.Size;
                wallSpriteGenerateComponent.Height = stackHeight;

                spriteComponent.Visible = false;
            }
        }

        _sawmill.Info("Done! Generated for:" + sw.Elapsed);
    }
}
