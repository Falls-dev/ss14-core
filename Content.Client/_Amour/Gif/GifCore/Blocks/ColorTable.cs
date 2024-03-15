using System.IO;

namespace Content.Client._Amour.Gif.GifCore.Blocks
{
	internal sealed class ColorTable : Block
	{
		public byte[] Bytes;

		public ColorTable(int size, BinaryReader binaryReader)
		{
			var length = 3 * (int) Math.Pow(2, size + 1);

            Bytes = binaryReader.ReadBytes(length);
		}
	}
}
