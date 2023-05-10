using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VersionPacker
{
    class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadAllLines(args[0]);

            List<byte[]> dataEntries = new List<byte[]>();
            using var stream = File.Create(Path.Combine(Path.GetDirectoryName(args[0]), "versions.dat"));
            using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

            writer.Write((ushort)0); // placeholder for version count, filled later

            void WriteWord(ushort word)
            {
                writer.Write((byte)(word >> 8));
                writer.Write((byte)word);
            }

            void WriteDword(uint dword)
            {
                writer.Write((byte)(dword >> 24));
                writer.Write((byte)(dword >> 16));
                writer.Write((byte)(dword >> 8));
                writer.Write((byte)dword);
            }

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                    continue;

                var parts = trimmed.Split(',');
                var version = parts[0];
                var language = parts[1];
                var info = parts[2];
                var file = parts[3];
                var features = int.Parse(parts[4]);
                bool merge = int.Parse(parts[5]) > 0;
                uint offset = (uint)writer.BaseStream.Position;

                writer.Write(version);
                writer.Write(language);
                writer.Write(info);
                WriteWord((ushort)features);
                writer.Write((byte)(merge ? 1 : 0));
                if (!Path.IsPathRooted(file))
                    file = Path.Combine(Path.GetDirectoryName(args[0]), file);
                var bytes = File.ReadAllBytes(file);
                WriteDword((uint)bytes.Length);
                dataEntries.Add(bytes);
            }

            foreach (var dataEntry in dataEntries)
            {
                writer.Write(dataEntry);
            }

            writer.BaseStream.Position = 0;
            writer.Write((byte)(dataEntries.Count >> 8));
            writer.Write((byte)dataEntries.Count);
        }
    }
}
