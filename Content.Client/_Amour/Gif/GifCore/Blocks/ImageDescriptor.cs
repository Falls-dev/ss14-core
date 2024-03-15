using System.IO;

namespace Content.Client._Amour.Gif.GifCore.Blocks
{
	internal sealed class ImageDescriptor : Block
	{
		public ushort ImageLeftPosition;
		public ushort ImageTopPosition;
		public ushort ImageWidth;
		public ushort ImageHeight;
		public byte LocalColorTableFlag;
		public byte InterlaceFlag;
		public byte SortFlag;
		public byte Reserved;
		public byte LocalColorTableSize;

		public ImageDescriptor(byte blockType, BinaryReader binaryReader)
		{
			if (blockType != ImageDescriptorLabel) throw new Exception("Expected: " + ImageDescriptorLabel);

			ImageLeftPosition = BitHelper.ReadInt16(binaryReader);
			ImageTopPosition = BitHelper.ReadInt16(binaryReader);
			ImageWidth = BitHelper.ReadInt16(binaryReader);
			ImageHeight = BitHelper.ReadInt16(binaryReader);

            var datum = binaryReader.ReadByte();

			LocalColorTableFlag = BitHelper.ReadPackedByte(datum, 0, 1);
			InterlaceFlag = BitHelper.ReadPackedByte(datum, 1, 1);
			SortFlag = BitHelper.ReadPackedByte(datum, 2, 1);
			Reserved = BitHelper.ReadPackedByte(datum, 3, 2);
			LocalColorTableSize = BitHelper.ReadPackedByte(datum, 5, 3);
		}

		public ImageDescriptor(ushort imageLeftPosition, ushort imageTopPosition, ushort imageWidth, ushort imageHeight,
			byte localColorTableFlag, byte interlaceFlag, byte sortFlag, byte reserved, byte localColorTableSize)
		{
			ImageLeftPosition = imageLeftPosition;
			ImageTopPosition = imageTopPosition;
			ImageWidth = imageWidth;
			ImageHeight = imageHeight;
			LocalColorTableFlag = localColorTableFlag;
			InterlaceFlag = interlaceFlag;
			SortFlag = sortFlag;
			Reserved = reserved;
			LocalColorTableSize = localColorTableSize;
		}

		public List<byte> GetBytes()
		{
			var bytes = new List<byte> { ImageDescriptorLabel };

			bytes.AddRange(BitConverter.GetBytes(ImageLeftPosition));
			bytes.AddRange(BitConverter.GetBytes(ImageTopPosition));
			bytes.AddRange(BitConverter.GetBytes(ImageWidth));
			bytes.AddRange(BitConverter.GetBytes(ImageHeight));

			var packedByte = BitHelper.PackByte(
				LocalColorTableFlag == 1,
				InterlaceFlag == 1,
				SortFlag == 1,
				BitHelper.ReadByte(Reserved, 1),
				BitHelper.ReadByte(Reserved, 0),
				BitHelper.ReadByte(LocalColorTableSize, 2),
				BitHelper.ReadByte(LocalColorTableSize, 1),
				BitHelper.ReadByte(LocalColorTableSize, 0));

			bytes.Add(packedByte);

			return bytes;
		}
	}
}
