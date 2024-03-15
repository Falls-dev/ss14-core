using System.IO;
using System.Linq;

namespace Content.Client._Amour.Gif.GifCore.Blocks
{
	internal abstract class Block
	{
		public const byte ExtensionIntroducer = 0x21;
		public const byte PlainTextExtensionLabel = 0x1;
		public const byte GraphicControlExtensionLabel = 0xF9;
		public const byte CommentExtensionLabel = 0xFE;
		public const byte ImageDescriptorLabel = 0x2C;
		public const byte ApplicationExtensionLabel = 0xFF;
		public const byte BlockTerminatorLabel = 0x00;

		protected byte[] ReadDataSubBlocks(BinaryReader binaryReader)
		{
			var data = new List<byte>();

            var len = binaryReader.ReadByte();

            while (len > 0)
            {
                var subBlock = binaryReader.ReadBytes(len);

                if (data.Count == 0)
                {
                    data = subBlock.ToList();
                }
                else
                {
                    data.AddRange(subBlock);
                }

                len = binaryReader.ReadByte();
            }

			return data.ToArray();
		}
	}
}
