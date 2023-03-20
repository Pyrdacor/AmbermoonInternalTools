using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Runtime.InteropServices;

namespace FontCreator
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine
            (
                "FontCreator <font_file> <png_file> <out_file> [advance]" + Environment.NewLine +
                "FontCreator <png_file> <out_file>" + Environment.NewLine + Environment.NewLine +
                "The first version was used to create fonts" + Environment.NewLine +
                "for intro/extro from font definitions." + Environment.NewLine + Environment.NewLine +
                "Use the IntroFont.* files in the root dir." + Environment.NewLine + Environment.NewLine +
                "The second version produces the extended ingame font" + Environment.NewLine +
                "which includes french letters." + Environment.NewLine + Environment.NewLine + Environment.NewLine +
                "Use the PNG IngameFont.png as an input." + Environment.NewLine
            );
        }

        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                CreateIngameFont(args[0], args[1]);
            }
            else if (args.Length == 3 || args.Length == 4)
            {
                CreateFont(args[0], args[1], args[2], args.Length == 3 ? 0 : int.Parse(args[3]));
            }
            else
            {
                Usage();
                Console.WriteLine("ERROR: Wrong number of arguments");
            }
        }

        static void CreateIngameFont(string pngFile, string outFile)
        {
            using var image = (Bitmap)Image.FromFile(pngFile);
            var foo = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var data = new byte[image.Width * image.Height * 4];
            int numGlyphs = image.Width / 12;
            var gdata = new byte[numGlyphs * 2 * image.Height];
            Marshal.Copy(foo.Scan0, data, 0, data.Length);
            image.UnlockBits(foo);
            int start = 0;
            int scanLine = image.Width * 4;
            for (int y = 0; y < image.Height; ++y)
            {
                for (int g = 0; g < numGlyphs; ++g)
                {
                    int index = start + g * 2;
                    byte mask = 0x80;

                    for (int x = 0; x < 8; ++x)
                    {
                        if (data[y * scanLine + g * 12 * 4 + x * 4] != 0)
                            gdata[index] |= mask;
                        mask >>= 1;
                    }

                    ++index;
                    mask = 0x80;

                    for (int x = 8; x < 12; ++x)
                    {
                        if (data[y * scanLine + g * 12 * 4 + x * 4] != 0)
                            gdata[index] |= mask;
                        mask >>= 1;
                    }
                }

                start += numGlyphs * 2;
            }
            File.WriteAllBytes(outFile, gdata);
        }

        class GlyphInfo
        {
            public char ch;
            public int x;
            public int y;
            public int width;
            public int height;
            public int advance;
            public byte[] data;
        }

        static List<GlyphInfo> ParseGlyphInfos(string file)
        {
            var glyphInfos = new List<GlyphInfo>();
            var lines = File.ReadAllLines(file);
            var regex = new Regex("^(.*)xoffset.*x(.*)page.*", RegexOptions.Compiled);
            var valueRegex = new Regex("char id=([0-9]+)[ ]+x=([0-9]+)[ ]+y=([0-9]+)[ ]+width=([0-9]+)[ ]+height=([0-9]+)[ ]+advance=([0-9]+)");

            foreach (var line in lines)
            {
                if (!line.StartsWith("char id"))
                    continue;

                var match = regex.Match(line);

                if (!match.Success)
                    throw new Exception("Invalid font file");

                string result = match.Groups[1].Value.Trim() + " " +
                    match.Groups[2].Value.Trim();

                match = valueRegex.Match(result);

                if (!match.Success)
                    throw new Exception("Invalid font file");

                glyphInfos.Add(new GlyphInfo
                {
                    ch = (char)int.Parse(match.Groups[1].Value),
                    x = int.Parse(match.Groups[2].Value),
                    y = int.Parse(match.Groups[3].Value),
                    width = int.Parse(match.Groups[4].Value),
                    height = int.Parse(match.Groups[5].Value),
                    advance = int.Parse(match.Groups[6].Value)
                });
            }

            return glyphInfos;
        }

        static void CreateFont(string fontFile, string pngFile, string output, int additionalAdvance)
        {
            var list = ParseGlyphInfos(fontFile);
            var bitmap = (Bitmap)Image.FromFile(pngFile);
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var buffer = new byte[bitmap.Width * bitmap.Height * 4];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            bitmap.UnlockBits(data);

            for (int i = 0; i < list.Count; ++i)
            {
                int resultWidth = list[i].width % 8 == 0 ? list[i].width : list[i].width + (8 - list[i].width % 8);
                list[i].data = new byte[resultWidth * list[i].height / 8];

                for (int y = 0; y < list[i].height; ++y)
                {
                    for (int x = 0; x < list[i].width; ++x)
                    {
                        byte b = buffer[((list[i].y + y) * bitmap.Width + list[i].x + x) * 4];

                        if (b != 0)
                        {
                            list[i].data[(y * resultWidth + x) / 8] |= (byte)(1 << (7 - x % 8));
                        }
                    }
                }

                list[i].width = resultWidth;
            }

            var writer = new BinaryWriter(File.Create(output));

            foreach (var glyph in list)
            {
                if (glyph.ch > 0x20)
                {
                    writer.Write((byte)glyph.ch);
                    writer.Write((byte)glyph.width);
                    writer.Write((byte)glyph.height);
                    writer.Write((byte)(glyph.advance + additionalAdvance));
                    writer.Write(glyph.data);
                }
            }

            writer.Close();
        }
    }
}
