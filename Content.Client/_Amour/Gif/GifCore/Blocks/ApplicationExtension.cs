using System.IO;

namespace Content.Client._Amour.Gif.GifCore.Blocks;

internal sealed class ApplicationExtension : Block
{
    public byte BlockSize;
    public byte[] ApplicationIdentifier = default!;
    public byte[] ApplicationAuthenticationCode = default!;
    public byte[] ApplicationData = default!;

    public ApplicationExtension(byte blockType, byte extensionType, BinaryReader binaryReader)
    {
        if (blockType != ExtensionIntroducer) throw new Exception("Expected: " + ExtensionIntroducer);
        if (extensionType != ApplicationExtensionLabel) throw new Exception("Expected: " + ApplicationExtensionLabel);

        BlockSize = binaryReader.ReadByte();
        ApplicationIdentifier = binaryReader.ReadBytes(8);
        ApplicationAuthenticationCode = binaryReader.ReadBytes(3);
        ApplicationData = ReadDataSubBlocks(binaryReader);
    }

    public ApplicationExtension()
    {
    }

    public byte[] GetBytes()
    {
        return new byte[] { 0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30, 0x03, 0x01, 0x00, 0x00, 0x00 };
    }
}
