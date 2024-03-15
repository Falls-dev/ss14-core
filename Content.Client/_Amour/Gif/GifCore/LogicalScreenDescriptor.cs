using System.IO;

namespace Content.Client._Amour.Gif.GifCore
{
	internal sealed class LogicalScreenDescriptor
	{
		public ushort LogicalScreenWidth;
		public ushort LogicalScreenHeight;
		public byte GlobalColorTableFlag;
		public byte ColorResolution;
		public byte SortFlag;
		public byte GlobalColorTableSize;
		public byte BackgroundColorIndex;
		public byte PixelAspecRatio;

		public LogicalScreenDescriptor(BinaryReader binaryReader)
		{
			LogicalScreenWidth = BitHelper.ReadInt16(binaryReader);
			LogicalScreenHeight = BitHelper.ReadInt16(binaryReader);

            var b = binaryReader.ReadByte();

			GlobalColorTableFlag = BitHelper.ReadPackedByte(b, 0, 1);
			ColorResolution = BitHelper.ReadPackedByte(b, 1, 3);
			SortFlag = BitHelper.ReadPackedByte(b, 4, 1);
			GlobalColorTableSize = BitHelper.ReadPackedByte(b, 5, 3);

			BackgroundColorIndex = binaryReader.ReadByte();
			PixelAspecRatio = binaryReader.ReadByte();
		}
	}
}
