using System.IO;

namespace Content.Client._Amour.Gif.GifCore.Blocks
{
	internal sealed class PlainTextExtension : Block
	{
		public byte BlockSize;
		public ushort TextGridLeftPosition;
		public ushort TextGridTopPosition;
		public ushort TextGridWidth;
		public ushort TextGridHeight;
		public byte CharacterCellWidth;
		public byte CharacterCellHeight;
		public byte TextForegroundColorIndex;
		public byte TextBackgroundColorIndex;
		public byte[] PlainTextData;

		public PlainTextExtension(byte blockType, byte extensionType, BinaryReader binaryReader)
		{
			if (blockType != ExtensionIntroducer) throw new Exception("Expected :" + ExtensionIntroducer);
			if (extensionType != PlainTextExtensionLabel) throw new Exception("Expected :" + PlainTextExtensionLabel);

			BlockSize = binaryReader.ReadByte();
			TextGridLeftPosition = BitHelper.ReadInt16(binaryReader);
			TextGridTopPosition = BitHelper.ReadInt16(binaryReader);
			TextGridWidth = BitHelper.ReadInt16(binaryReader);
			TextGridHeight = BitHelper.ReadInt16(binaryReader);
			CharacterCellWidth = binaryReader.ReadByte();
			CharacterCellHeight = binaryReader.ReadByte();
			TextForegroundColorIndex = binaryReader.ReadByte();
			TextBackgroundColorIndex = binaryReader.ReadByte();
			PlainTextData = ReadDataSubBlocks(binaryReader);

			//if (binaryReader.ReadByte() != BlockTerminatorLabel) throw new Exception("Expected: " + BlockTerminatorLabel);
		}
	}
}
