using System.IO;

namespace Content.Client._Amour.Gif.GifCore.Blocks
{
	internal sealed class CommentExtension : Block
	{
		public byte[] CommentData;

		public CommentExtension(byte blockType, byte extensionType, BinaryReader binaryReader)
		{
			if (blockType != ExtensionIntroducer) throw new Exception("Expected: " + ExtensionIntroducer);
			if (extensionType != CommentExtensionLabel) throw new Exception("Expected: " + CommentExtensionLabel);

			CommentData = ReadDataSubBlocks(binaryReader);

			//if (binaryReader.ReadByte() != BlockTerminatorLabel) throw new Exception("Expected: " + BlockTerminatorLabel);
		}
	}
}
