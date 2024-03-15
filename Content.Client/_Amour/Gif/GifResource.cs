using System.IO;
using Robust.Client.ResourceManagement;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client._Amour.Gif;

public sealed class GifResource : BaseResource
{
    private Gif _gif = default!;

    public override ResPath? Fallback { get; } = new ResPath("/Gifs/Fuck/fuck.gif");
    public Gif Gif => _gif;

    public override void Load(IDependencyCollection dependencies, ResPath path)
    {
        var resourceManager = dependencies.Resolve<IResourceManager>();

        using var stream = resourceManager.ContentFileRead(path);
        Decode(stream,dependencies.Resolve<GifManager>());
    }

    private void Decode(Stream stream, GifManager gifManager)
    {
        _gif = gifManager.Decode(stream);
    }
}
