#pragma warning disable CA1416 // Validate platform compatibility

using System.Drawing.Imaging;
using System.Drawing;

static byte[] ExtractGlyphDataFromBitmap(Bitmap bmp, bool large, int glyphHeight, int glyphCount)
{
    int glyphWidth = large ? 32 : 16;
    int bytesPerRow = large ? 4 : 2;

    int glyphsPerRow = 16;
    int glyphsInRow = bmp.Width / glyphWidth;
    int glyphRows = bmp.Height / glyphHeight;

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




/*using var reader = new BinaryReader(File.OpenRead(@"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\Extro_fonts"));

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
    .Save(@"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\LargeGlyphsAgain.png", ImageFormat.Png);*/

//GlyphTool.Main(args);

static void CreateFonts(string filename, string smallGlyphImagePath, string largeGlyphImagePath)
{
    using var smallGlyphBitmap = new Bitmap(smallGlyphImagePath);
    using var largeGlyphBitmap = new Bitmap(largeGlyphImagePath);
    using var writer = new BinaryWriter(File.Create(filename));

    // For czech we use Latin-2 (central european languages), Code page 852
    // Used for: Bosnian, Croatian, Czech, Hungarian, Polish, Romanian, and Slovak.
    // We need to include up to character 253 but use 2 more. Count is 256 but minus the 32 control chars, so 224 characters.
    const byte numCharacters = 224;
    const byte numGlyphs = 108;
    const byte smallGlyphHeight = 11;
    const byte largeGlyphHeight = 22;
    const byte smallSpaceAdvance = 6;
    const byte largeSpaceAdvance = 10;

    writer.Write(numCharacters);
    writer.Write(numGlyphs);
    writer.Write(smallGlyphHeight);
    writer.Write(largeGlyphHeight);
    // Used heights
    writer.Write((byte)(smallGlyphHeight - 1));
    writer.Write((byte)(largeGlyphHeight - 1));
    // Line heights
    writer.Write((byte)(smallGlyphHeight + 1));
    writer.Write((byte)(largeGlyphHeight + 1));
    // Space advances
    writer.Write(smallSpaceAdvance);
    writer.Write(largeSpaceAdvance);

    byte[] glyphMapping =
    [
        // ASCII (original extro mappings for first 128 characters minus the 32 control chars, so 96 mapping bytes)
        0xFF, 0x42, 0xFF, 0xFF, 0xFF, 0xFF, 0x47, 0x4B, 0x44, 0x45, 0xFF, 0x46, 0x3E, 0x48, 0x3F, 0x43,
        0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x40, 0xFF, 0x49, 0xFF, 0x4A, 0x41,
        0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
        0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0x4B, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        // Czech characters
        0xFF, 0xFF, 0x53, 0xFF, 0xFF, 0x67, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0x4D, 0xFF, 0x4E, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x64, 0x65, 0xFF, 0xFF, 0x59,
        0x52, 0x54, 0x55, 0x56, 0xFF, 0xFF, 0x68, 0x69, 0xFF, 0xFF, 0xFF, 0xFF, 0x58, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x4C, 0xFF, 0x5C, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0x5A, 0xFF, 0x5B, 0x5E, 0x4E, 0xFF, 0x5D, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x66, 0xFF,
        0x4F, 0xFF, 0xFF, 0xFF, 0xFF, 0x5F, 0x62, 0x63, 0xFF, 0x50, 0xFF, 0xFF, 0x57, 0x51, 0xFF, 0x4B,
        0x48, 0xFF, 0x3E, 0xFF, 0xFF, 0xFF, 0xFF, 0x3E, 0xFF, 0xFF, 0xFF, 0xFF, 0x60, 0x61, 0x6C, 0x6D,
        // Note: The last two are custom mapings for ch and CH characters which do not appear in the code page.
    ];

    writer.Write(glyphMapping);

    if (glyphMapping.Length % 2 == 1)
        writer.Write((byte)0);

    var smallAdvances = GetGlyphAdvances(smallGlyphBitmap, 11);

    if (smallAdvances.Length != numGlyphs)
        throw new Exception("Small glyph count mismatch!");

    writer.Write(smallAdvances);

    if (smallAdvances.Length % 2 == 1)
        writer.Write((byte)0);

    var largeAdvances = GetGlyphAdvances(largeGlyphBitmap, 22);

    if (largeAdvances.Length != smallAdvances.Length)
        throw new Exception("Small and large glyph count mismatch!");

    writer.Write(largeAdvances);

    if (largeAdvances.Length % 2 == 1)
        writer.Write((byte)0);

    var smallGlyphs = ExtractGlyphDataFromBitmap(smallGlyphBitmap, false, 11, numGlyphs);

    writer.Write(smallGlyphs);

    var largeGlyphs = ExtractGlyphDataFromBitmap(largeGlyphBitmap, true, 22, numGlyphs);

    writer.Write(largeGlyphs);

    while (writer.BaseStream.Position % 4 != 0)
    {
        writer.Write((byte)0); // Pad to 4-byte boundary
    }
}

CreateFonts
(
    @"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\CzechGlyphs\Extro_fonts",
    @"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\CzechGlyphs\SmallGlyphsCzech.png",
    @"D:\Projects\AmbermoonInternalTools\AmbermoonExtroPatcher\CzechGlyphs\LargeGlyphsCzech.png"
);

static byte[] GetGlyphAdvances(Bitmap bitmap, int glyphHeight)
{
    int glyphsPerRow = 16;
    int glyphWidth = bitmap.Width / glyphsPerRow;
    int totalRows = bitmap.Height / glyphHeight;
    int totalGlyphs = glyphsPerRow * totalRows;

    var advances = new List<byte>(totalGlyphs);
    int glyphCount = 0;

    for (int index = 0; index < totalGlyphs; index++)
    {
        int gx = (index % glyphsPerRow) * glyphWidth;
        int gy = (index / glyphsPerRow) * glyphHeight;

        int width = 0;

        for (int y = 0; y < glyphHeight; y++)
        {
            for (int x = width; x < glyphWidth; x++)
            {
                Color pixel = bitmap.GetPixel(gx + x, gy + y);

                if (pixel.A > 0)
                {
                    if (x >= width)
                        width = x + 1;
                }
            }
        }

        if (width == 0)
        {
            glyphCount = index;
            break;
        }

        advances.Add((byte)(width + 1));
    }

    return [.. advances.Take(glyphCount)];
}



class GlyphTool
{
    const int GlyphsPerRow = 16;

    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: GlyphTool <imagePath>");
            return;
        }

        string imagePath = args[0];

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"File not found: {imagePath}");
            return;
        }

        Bitmap bmp;
        try
        {
            using var originalStream = File.OpenRead(imagePath);
            var memoryStream = new MemoryStream();
            originalStream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            bmp = new Bitmap(memoryStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load image: {ex.Message}");
            return;
        }

        int glyphWidth = bmp.Width / GlyphsPerRow;
        bool large = glyphWidth == 32;
        int glyphHeight = large ? 22 : 11;

        if (bmp.Width % GlyphsPerRow != 0 || bmp.Height % glyphHeight != 0)
        {
            Console.WriteLine("Invalid bitmap dimensions for glyph layout.");
            return;
        }

        int glyphsPerColumn = bmp.Height / glyphHeight;
        int totalGlyphs = GlyphsPerRow * glyphsPerColumn;

        int freeIndex = -1;
        while (freeIndex < 0 || freeIndex > totalGlyphs)
        {
            Console.Write("Enter index of first free glyph slot: ");
            string? input = Console.ReadLine();
            if (!int.TryParse(input, out freeIndex) || freeIndex < 0)
            {
                Console.WriteLine("Invalid index.");
                freeIndex = -1;
            }
        }

        if (args.Length >= 2)
        {
            var lines = File.ReadAllLines(args[1]);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                Console.WriteLine("> " + line);
                ProcessCommand(line);
            }
        }

        Console.WriteLine("Enter commands: copy <index> | save [path] | exit");

        void ProcessCommand(string line)
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string command = parts[0].ToLowerInvariant();

            switch (command)
            {
                case "exit":
                    return;

                case "copy":
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int srcIndex))
                    {
                        Console.WriteLine("Usage: copy <index>");
                        break;
                    }

                    if (srcIndex < 0 || srcIndex >= totalGlyphs)
                    {
                        Console.WriteLine("Source index out of bounds.");
                        break;
                    }

                    if (freeIndex == totalGlyphs)
                    {
                        bmp = AddGlyphRow(bmp, glyphWidth, glyphHeight);
                        totalGlyphs += GlyphsPerRow;
                        Console.WriteLine("New glyph row was added to make space.");
                    }

                    CopyGlyph(bmp, srcIndex, freeIndex, glyphWidth, glyphHeight);
                    Console.WriteLine($"Copied glyph {srcIndex} to {freeIndex}.");
                    freeIndex++;
                    break;

                case "save":
                    string outPath;
                    if (parts.Length >= 2)
                        outPath = parts[1];
                    else
                        outPath = Path.Combine(Path.GetDirectoryName(imagePath)!, Path.GetFileName(imagePath));

                    try
                    {
                        bmp.Save(outPath, ImageFormat.Png);
                        Console.WriteLine($"Saved to: {outPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Save failed: {ex.Message}");
                    }
                    break;

                default:
                    Console.WriteLine("Unknown command. Use: addrow | copy <index> | save [path] | exit");
                    break;
            }
        }

        while (true)
        {
            Console.Write("> ");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            ProcessCommand(line);
        }
    }

    static Bitmap AddGlyphRow(Bitmap bmp, int glyphWidth, int glyphHeight)
    {
        int newHeight = bmp.Height + glyphHeight;
        var newBmp = new Bitmap(bmp.Width, newHeight, PixelFormat.Format32bppArgb);

        using (Graphics g = Graphics.FromImage(newBmp))
        {
            g.Clear(Color.Transparent);

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

            g.DrawImage(bmp, 0, 0);
        }

        bmp.Dispose();

        return newBmp;
    }

    static void CopyGlyph(Bitmap bmp, int fromIndex, int toIndex, int glyphWidth, int glyphHeight)
    {
        int fromX = (fromIndex % GlyphsPerRow) * glyphWidth;
        int fromY = (fromIndex / GlyphsPerRow) * glyphHeight;

        int toX = (toIndex % GlyphsPerRow) * glyphWidth;
        int toY = (toIndex / GlyphsPerRow) * glyphHeight;

        using Graphics g = Graphics.FromImage(bmp);
        Rectangle src = new Rectangle(fromX, fromY, glyphWidth, glyphHeight);
        Rectangle dst = new Rectangle(toX, toY, glyphWidth, glyphHeight);

        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

        g.DrawImage(bmp, dst, src, GraphicsUnit.Pixel);
    }
}


#pragma warning restore CA1416 // Validate platform compatibility