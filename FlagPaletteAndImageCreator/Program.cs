using System.Drawing;
using System.Runtime.InteropServices;

namespace FlagPaletteAndImageCreator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using var bitmap = (Bitmap)Image.FromFile(args[0]);
            int size = bitmap.Width * bitmap.Height;
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bgra = new byte[size * 4];
            Marshal.Copy(data.Scan0, bgra, 0, bgra.Length);
            bitmap.UnlockBits(data);

            unsafe
            {
                fixed (byte* ptr = bgra)
                {
                    uint* p = (uint*)ptr;
                    uint* end = p + size;
                    var colors = new HashSet<uint>(32);
                    var colorMapping = new List<uint>(size);

                    while (p != end)
                    {
                        uint color = *p++;
                        colorMapping.Add(color);
                        colors.Add(color);
                    }

                    if (colors.Count > 32)
                        throw new Exception("Too many colors in this image.");

                    int i;
                    using var writer = new BinaryWriter(File.Create(args[1]));
                    var colorList = new List<uint>(colors);

                    // Store the palette
                    for (i = 0; i < colorList.Count; ++i)
                    {
                        var c = colorList[i];
                        writer.Write((byte)((c & 0xff0000) >> 16)); // R
                        writer.Write((byte)((c & 0xff00) >> 8)); // G                        
                        writer.Write((byte)(c & 0xff)); // B
                        writer.Write((byte)(c >> 24)); // A
                    }

                    for (; i < 32; ++i) // fill up missing colors
                    {
                        writer.Write(0u);
                    }

                    // Store the total size
                    writer.Write((byte)(bitmap.Width >> 8));
                    writer.Write((byte)(bitmap.Width & 0xff));
                    writer.Write((byte)(bitmap.Height >> 8));
                    writer.Write((byte)(bitmap.Height & 0xff));

                    // Store the indices
                    writer.Write(colorMapping.Select(m => (byte)colorList.IndexOf(m)).ToArray());
                }
            }            
        }
    }
}