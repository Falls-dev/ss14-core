using System.Threading.Tasks;
using Content.Client._Amour.Gif.Data;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Amour.Gif.UI;

public sealed class GifRect : TextureRect
{
    public static string PleaseWaitTexturePath = "/Textures/Gif/loading.png";
    public Texture PleaseWaitTexture = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(PleaseWaitTexturePath).Texture;

    public string GifPath
    {
        get => _gifPath;
        set
        {
            _gifPath = value;
            UpdateControl();
            SetShit(value);
        }
    }

    private async Task SetShit(string value)
    {
        _gif = IoCManager.Resolve<IResourceCache>().GetResource<GifResource>(value).Gif;
        Logger.Debug("LOADED YAY");
    }

    private string _gifPath = "";

    public Gif? Gif
    {
        get => _gif;
        set
        {
            UpdateControl();
            _gif = value;
        }
    }

    private Gif? _gif;

    public int Frame { get; private set; }
    private float _delay;

    private void UpdateControl()
    {
        Texture = PleaseWaitTexture;
        Frame = 0;
        _delay = 0;
        _gif = null;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if(_gif is null || !VisibleInTree)
            return;

        var current = _gif.Frames[Frame];
        _delay += args.DeltaSeconds;
        if(_delay < current.Delay)
            return;

        Texture = current.Texture;
        _delay = 0;
        Frame = (Frame + 1) % _gif.Frames.Count;
    }
}

