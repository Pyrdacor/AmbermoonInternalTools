#pragma warning disable CA1416 // Validate platform compatibility

using System.Drawing.Imaging;
using System.Drawing;

static byte[] ExtractGlyphDataFromBitmap(Bitmap bmp, bool large, int glyphHeight)
{
    int glyphWidth = large ? 32 : 16;
    int bytesPerRow = large ? 4 : 2;

    int glyphsPerRow = 16;
    int glyphsInRow = bmp.Width / glyphWidth;
    int glyphRows = bmp.Height / glyphHeight;
    int glyphCount = glyphsInRow * glyphRows;

    byte[] result = new byte[glyphCount * glyphHeight * bytesPerRow];

    int resultOffset = 0;

    for (int glyphIndex = 0; glyphIndex < glyphCount; glyphIndex++)
    {
        int glyphX = (glyphIndex % glyphsPerRow) * glyphWidth;
        int glyphY = (glyphIndex / glyphsPerRow) * glyphHeight;

        for (int row = 0; row < glyphHeight; row++)
        {
            uint bits = 0;

            for (int bit = 0; bit < glyphWidth; bit++)
            {
                Color color = bmp.GetPixel(glyphX + bit, glyphY + row);
                bool isSet = color.ToArgb() == Color.White.ToArgb(); // treat white as "on"
                bits <<= 1;
                if (isSet)
                    bits |= 1;
            }

            // Store as big endian
            for (int i = bytesPerRow - 1; i >= 0; i--)
            {
                result[resultOffset + i] = (byte)(bits & 0xFF);
                bits >>= 8;
            }

            resultOffset += bytesPerRow;
        }
    }

    return result;
}

static Bitmap RenderGlyphs(byte[] glyphData, bool large, int glyphHeight)
{
    int glyphWidth = large ? 32 : 16;
    int bytesPerRow = large ? 4 : 2;
    int glyphSize = glyphHeight * bytesPerRow;
    int glyphCount = glyphData.Length / glyphSize;

    int glyphsPerRow = 16;
    int rowsOfGlyphs = (glyphCount + glyphsPerRow - 1) / glyphsPerRow;

    int imageWidth = glyphsPerRow * glyphWidth;
    int imageHeight = rowsOfGlyphs * glyphHeight;

    Bitmap bmp = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);

    for (int glyphIndex = 0; glyphIndex < glyphCount; glyphIndex++)
    {
        int glyphX = (glyphIndex % glyphsPerRow) * glyphWidth;
        int glyphY = (glyphIndex / glyphsPerRow) * glyphHeight;

        int offset = glyphIndex * glyphSize;

        for (int row = 0; row < glyphHeight; row++)
        {
            int rowOffset = offset + row * bytesPerRow;

            uint bitData = 0;

            // Read big-endian word or long
            for (int i = 0; i < bytesPerRow; i++)
            {
                bitData = (bitData << 8) | glyphData[rowOffset + i];
            }

            for (int bit = 0; bit < glyphWidth; bit++)
            {
                bool isSet = ((bitData >> (glyphWidth - 1 - bit)) & 1) != 0;
                if (isSet)
                {
                    bmp.SetPixel(glyphX + bit, glyphY + row, Color.White);
                }
            }
        }
    }

    return bmp;
}




using var reader = new BinaryReader(File.OpenRead(@"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\Extro_fonts"));

reader.BaseStream.Position += 10;
reader.BaseStream.Position += 0x60;
reader.BaseStream.Position += 0x4c;
reader.BaseStream.Position += 0x4c;

var smallGlyphData = reader.ReadBytes(22 * 0x4c);
var largeGlyphData = reader.ReadBytes(88 * 0x4c);

RenderGlyphs(smallGlyphData, false, 11).Save(@"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\SmallGlyphs.png", ImageFormat.Png);
RenderGlyphs(largeGlyphData, true, 22).Save(@"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\LargeGlyphs.png", ImageFormat.Png);

RenderGlyphs(ExtractGlyphDataFromBitmap(RenderGlyphs(smallGlyphData, false, 11), false, 11), false, 11)
    .Save(@"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\SmallGlyphsAgain.png", ImageFormat.Png);
RenderGlyphs(ExtractGlyphDataFromBitmap(RenderGlyphs(largeGlyphData, true, 22), true, 22), true, 22)
    .Save(@"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\LargeGlyphsAgain.png", ImageFormat.Png);

#pragma warning restore CA1416 // Validate platform compatibility