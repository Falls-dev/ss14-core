using Content.Client._Amour.Gif.Data;

// Original shit from: https://github.com/hippogamesunity/SimpleGif/
// Some rewrite shit for RobustShitBox
namespace Content.Client._Amour.Gif
{
	/// <summary>
	/// Simple class for working with GIF format
	/// </summary>
	public sealed class Gif
	{
		/// <summary>
		/// List of GIF frames
		/// </summary>
		public List<GifFrame> Frames;

		/// <summary>
		/// Create a new instance from GIF frames.
		/// </summary>
		public Gif(List<GifFrame> frames)
		{
			Frames = frames;
		}

		public static readonly Color EmptyColor = new Color();
    }
}
