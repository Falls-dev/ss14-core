using Content.Client._Amour.Gif.Enums;
using Content.Client._Amour.Gif.GifCore.Blocks;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client._Amour.Gif;

public sealed partial class GifManager
{
    private byte[] GetColorIndexes(Texture texture, int scale, List<Color> colorTable, byte localColorTableFlag,
        ref byte transparentColorFlag, ref byte transparentColorIndex, out byte max)
    {
        var indexes = new Dictionary<Color, int>();

        for (var i = 0; i < colorTable.Count; i++)
        {
            indexes.Add(colorTable[i], i);
        }

        var colorIndexes = new byte[texture.Width * texture.Height * scale * scale];

        max = 0;

        Action<int, int, byte> setScaledIndex = (x, y, index) =>
        {
            for (var dy = 0; dy < scale; dy++)
            {
                for (var dx = 0; dx < scale; dx++)
                {
                    colorIndexes[x * scale + dx + (y * scale + dy) * texture.Width * scale] = index;
                }
            }
        };

        for (var y = 0; y < texture.Height; y++)
        {
            for (var x = 0; x < texture.Width; x++)
            {
                var pixel = texture[x,y];

                if (pixel.A == 0)
                {
                    if (transparentColorFlag == 0)
                    {
                        transparentColorFlag = 1;

                        if (localColorTableFlag == 1)
                        {
                            transparentColorIndex = (byte) indexes[pixel];
                            colorTable[transparentColorIndex] = GetTransparentColor(colorTable);
                        }
                    }

                    if (scale == 1)
                        colorIndexes[x + y * texture.Width] = transparentColorIndex;
                    else
                        setScaledIndex(x, y, transparentColorIndex);

                    if (transparentColorIndex > max)
                        max = transparentColorIndex;
                }
                else
                {
                    var index = indexes[pixel];

                    if (index >= 0)
                    {
                        var i = (byte) index;

                        if (scale == 1)
                            colorIndexes[x + y * texture.Width] = i;
                        else
                            setScaledIndex(x, y, i);

                        if (i > max)
                            max = i;
                    }
                    else
                        throw new Exception("Color index not found: " + pixel);
                }
            }
        }

        return colorIndexes;
    }

    private static Color[] GetUnityColors(ColorTable table)
    {
        var colors = new Color[table.Bytes.Length / 3];

        for (var i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color(table.Bytes[3 * i], table.Bytes[3 * i + 1], table.Bytes[3 * i + 2]);
        }

        return colors;
    }

    private static void ReplaceTransparentColor(ref List<Color> colors)
    {
        for (var i = 0; i < colors.Count; i++)
        {
            if (colors[i].A == 0)
            {
                colors.RemoveAll(j => j.A == 0);
                colors.Insert(0, GetTransparentColor(colors));

                return;
            }
        }
    }

    private static Color GetTransparentColor(List<Color> colorTable)
    {
        for (byte r = 0; r < 0xFF; r++)
        {
            for (byte g = 0; g < 0xFF; g++)
            {
                for (byte b = 0; b < 0xFF; b++)
                {
                    var transparentColor = new Color(r, g, b, 1);

                    if (!colorTable.Contains(transparentColor))
                        return transparentColor;
                }
            }
        }

        throw new Exception("Unable to resolve transparent color!");
    }


    public SpriteSpecifier ToSpriteSpecifier(Gif gif)
    {
        throw new NotImplementedException();
    }
}
