using System.IO;
using System.Linq;
using Content.Client._Amour.Gif.Data;
using Content.Client._Amour.Gif.Enums;
using Content.Client._Amour.Gif.GifCore;
using Content.Client._Amour.Gif.GifCore.Assets.GifCore;
using Content.Client._Amour.Gif.GifCore.Blocks;
using Robust.Client.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Amour.Gif;

public sealed partial class GifManager
{
    [Dependency] private readonly IClyde _clyde = default!;
    /// <summary>
    ///     Decode byte array and return a new instance.
    /// </summary>
    public Gif Decode(Stream stream)
    {
        return new Gif(DecodeIterator(stream).ToList());
    }

    /// <summary>
    ///     Iterator can be used for large GIF-files in order to display progress bar.
    /// </summary>
    public IEnumerable<GifFrame> DecodeIterator(Stream stream)
    {
        var parser = new GifParser(stream);
        var blocks = parser.Blocks;
        var width = parser.LogicalScreenDescriptor.LogicalScreenWidth;
        var height = parser.LogicalScreenDescriptor.LogicalScreenHeight;
        var globalColorTable = parser.LogicalScreenDescriptor.GlobalColorTableFlag == 1
            ? GetUnityColors(parser.GlobalColorTable)
            : default!;
        //var backgroundColor = globalColorTable?[parser.LogicalScreenDescriptor.BackgroundColorIndex] ?? EmptyColor;
        GraphicControlExtension graphicControlExtension = default!;
        var state = new Color[width * height];
        var filled = false;

        for (var j = 0; j < parser.Blocks.Count; j++)
        {
            if (blocks[j] is GraphicControlExtension)
                graphicControlExtension = (GraphicControlExtension) blocks[j];
            else if (blocks[j] is ImageDescriptor)
            {
                var imageDescriptor = (ImageDescriptor) blocks[j];

                if (imageDescriptor.InterlaceFlag == 1)
                    throw new NotSupportedException("Interlacing is not supported!");

                var colorTable = imageDescriptor.LocalColorTableFlag == 1
                    ? GetUnityColors((ColorTable) blocks[j + 1])
                    : globalColorTable;
                var data = (TableBasedImageData) blocks[j + 1 + imageDescriptor.LocalColorTableFlag];
                var frame = DecodeFrame(graphicControlExtension, imageDescriptor, data, filled, width, height, state,
                    colorTable);

                yield return frame;

                switch (frame.DisposalMethod)
                {
                    case DisposalMethod.NoDisposalSpecified:
                    case DisposalMethod.DoNotDispose:
                        break;
                    case DisposalMethod.RestoreToBackgroundColor:
                        for (var i = 0; i < state.Length; i++)
                        {
                            state[i] = Gif.EmptyColor;
                        }

                        filled = true;
                        break;
                    case DisposalMethod.RestoreToPrevious: // 'state' was already copied before decoding current frame
                        filled = false;
                        break;
                    default:
                        throw new NotSupportedException($"Unknown disposal method: {frame.DisposalMethod}!");
                }
            }
        }
    }


    private GifFrame DecodeFrame(GraphicControlExtension extension, ImageDescriptor descriptor,
        TableBasedImageData data, bool filled, int width, int height, Color[] state, Color[] colorTable)
    {
        var colorIndexes = LzwDecoder.Decode(data.ImageData, data.LzwMinimumCodeSize);

        return DecodeFrame(extension, descriptor, colorIndexes, filled, width, height, state, colorTable);
    }

    private GifFrame DecodeFrame(GraphicControlExtension extension, ImageDescriptor descriptor,
        byte[] colorIndexes, bool filled, int width, int height, Color[] state, Color[] colorTable)
    {
        var frame = new GifFrame();
        var pixels = state;
        var transparentIndex = -1;

        if (extension != null)
        {
            frame.Delay = extension.DelayTime / 100f;
            frame.DisposalMethod = (DisposalMethod) extension.DisposalMethod;

            if (frame.DisposalMethod == DisposalMethod.RestoreToPrevious)
                pixels = state.ToArray();

            if (extension.TransparentColorFlag == 1)
                transparentIndex = extension.TransparentColorIndex;
        }

        for (var y = 0; y < descriptor.ImageHeight; y++)
        {
            for (var x = 0; x < descriptor.ImageWidth; x++)
            {
                var colorIndex = colorIndexes[x + y * descriptor.ImageWidth];
                var transparent = colorIndex == transparentIndex;

                if (transparent && !filled)
                    continue;

                var color = transparent ? Gif.EmptyColor : colorTable[colorIndex];
                var fx = x + descriptor.ImageLeftPosition;
                var fy = y + descriptor.ImageTopPosition;

                pixels[fx + fy * width] = pixels[fx + fy * width] = color;
            }
        }

        var rgaArray = pixels.Select(color => new Rgba32(color.R, color.G, color.B, color.A)).ToArray();

        using var image = new Image<Rgba32>(width, height);

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                image[x, y] = rgaArray[x + y * width];
            }
        }

        frame.Texture = _clyde.LoadTextureFromImage(image);

        return frame;
    }
}
