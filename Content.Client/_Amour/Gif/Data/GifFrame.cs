using Content.Client._Amour.Gif.Enums;
using Content.Client._Amour.Gif.GifCore;
using Robust.Client.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._Amour.Gif.Data
{
	/// <summary>
	/// Texture + delay + disposal method
	/// </summary>
	public sealed class GifFrame
	{
		public Texture Texture = default!;
		public float Delay;
		public DisposalMethod DisposalMethod = DisposalMethod.RestoreToBackgroundColor;

        //public void ApplyPalette(MasterPalette palette)
		//{
		//	TextureConverter.ConvertTo8Bits(ref Texture, palette);
		//}
	}
}
