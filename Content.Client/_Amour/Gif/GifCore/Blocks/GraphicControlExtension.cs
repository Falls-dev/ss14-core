using System.IO;

namespace Content.Client._Amour.Gif.GifCore.Blocks
{
	internal sealed class GraphicControlExtension : Block
	{
		public byte BlockSize;
		public byte Reserved;
		public byte DisposalMethod;
		public byte UserInputFlag;
		public byte TransparentColorFlag;
		public ushort DelayTime;
		public byte TransparentColorIndex;

		public GraphicControlExtension(byte blockType, byte extensionType, BinaryReader binaryReader)
		{
			if (blockType != ExtensionIntroducer) throw new Exception("Expected: " + ExtensionIntroducer);
			if (extensionType != GraphicControlExtensionLabel) throw new Exception("Expected: " + GraphicControlExtensionLabel);

			BlockSize = binaryReader.ReadByte();

            var datum = binaryReader.ReadByte();

			Reserved = BitHelper.ReadPackedByte(datum, 0, 3);
			DisposalMethod = BitHelper.ReadPackedByte(datum, 3, 3);
			UserInputFlag = BitHelper.ReadPackedByte(datum, 6, 1);
			TransparentColorFlag = BitHelper.ReadPackedByte(datum, 7, 1);

			DelayTime = BitHelper.ReadInt16(binaryReader);
			TransparentColorIndex = binaryReader.ReadByte();

			if (binaryReader.ReadByte() != BlockTerminatorLabel) throw new Exception("Expected: " + BlockTerminatorLabel);
		}

		public GraphicControlExtension(byte blockSize, byte reserved, byte disposalMethod, byte userInputFlag, byte transparentColorFlag, ushort delayTime, byte transparentColorIndex)
		{
			BlockSize = blockSize;
			Reserved = reserved;
			DisposalMethod = disposalMethod;
			UserInputFlag = userInputFlag;
			TransparentColorFlag = transparentColorFlag;
			DelayTime = delayTime;
			TransparentColorIndex = transparentColorIndex;
		}

		public List<byte> GetBytes()
		{
			var bytes = new List<byte> { ExtensionIntroducer, GraphicControlExtensionLabel, BlockSize };
			var packedByte = BitHelper.PackByte(
				BitHelper.ReadByte(Reserved, 2),
				BitHelper.ReadByte(Reserved, 1),
				BitHelper.ReadByte(Reserved, 0),
				BitHelper.ReadByte(DisposalMethod, 2),
				BitHelper.ReadByte(DisposalMethod, 1),
				BitHelper.ReadByte(DisposalMethod, 0),
				UserInputFlag == 1,
				TransparentColorFlag == 1);

			bytes.Add(packedByte);
			bytes.AddRange(BitConverter.GetBytes(DelayTime));
			bytes.Add(TransparentColorIndex);
			bytes.Add(BlockTerminatorLabel);

			return bytes;
		}
	}
}
