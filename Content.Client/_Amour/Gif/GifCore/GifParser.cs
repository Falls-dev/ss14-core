using System.IO;
using System.Text;
using Content.Client._Amour.Gif.GifCore.Blocks;

namespace Content.Client._Amour.Gif.GifCore
{
	/// <summary>
	/// Gif specs: https://www.w3.org/Graphics/GIF/spec-gif89a.txt
	/// </summary>
	internal sealed class GifParser
	{
		public string Header;
		public LogicalScreenDescriptor LogicalScreenDescriptor;
        public ColorTable GlobalColorTable = default!;
		public List<Block> Blocks;

        public GifParser(Stream stream)
        {
            using var binaryReader = new BinaryReader(stream);

            Header = Encoding.UTF8.GetString(binaryReader.ReadBytes(6));
            LogicalScreenDescriptor = new LogicalScreenDescriptor(binaryReader);

            if (LogicalScreenDescriptor.GlobalColorTableFlag == 1)
            {
                GlobalColorTable = new ColorTable(LogicalScreenDescriptor.GlobalColorTableSize, binaryReader);
            }

            Blocks = ReadBlocks(binaryReader);
        }

		private static List<Block> ReadBlocks(BinaryReader binaryReader)
		{
			var blocks = new List<Block>();

			while (true)
            {
                var blockType = binaryReader.ReadByte();
				switch (blockType)
				{
					case Block.ExtensionIntroducer:
					{
						Block extension;
                        var extensionType = binaryReader.ReadByte();

						switch (extensionType)
						{
							case Block.PlainTextExtensionLabel:
								extension = new PlainTextExtension(blockType, extensionType, binaryReader);
								break;
							case Block.GraphicControlExtensionLabel:
								extension = new GraphicControlExtension(blockType, extensionType, binaryReader);
								break;
							case Block.CommentExtensionLabel:
								extension = new CommentExtension(blockType, extensionType, binaryReader);
								break;
							case Block.ApplicationExtensionLabel:
								extension = new ApplicationExtension(blockType, extensionType, binaryReader);
								break;
							default:
								throw new NotSupportedException("Unknown extension!");
						}
						blocks.Add(extension);
						break;
					}
					case Block.ImageDescriptorLabel:
					{
						var descriptor = new ImageDescriptor(blockType,binaryReader);

						blocks.Add(descriptor);

						if (descriptor.LocalColorTableFlag == 1)
						{
							var localColorTable = new ColorTable(descriptor.LocalColorTableSize, binaryReader);

							blocks.Add(localColorTable);
						}

						var data = new TableBasedImageData(binaryReader);

						blocks.Add(data);

						break;
					}
					case 0x3B: // End
					{
						return blocks;
					}
					default:
						throw new NotSupportedException($"Unsupported GIF block: {blockType:X}.");
				}
			}
		}
	}
}
