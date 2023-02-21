using Ambermoon.Data.Legacy.Serialization;

namespace DiffPacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string diffPath = args[0];
            string outPath = args[1];

            // TODO: later load the source and target versions from CLI
            int sourceEpisode = 1;
            int targetEpisode = 2;

            byte CreateEpisodeMappingHeader()
            {
                byte header = (byte)(targetEpisode & 0xf);
                header = (byte)(header | ((sourceEpisode << 4) & 0xf0));
                return header;
            }

            string[] files = new string[5]
            {
                "Party_data.diff",
                "Party_char.diff",
                "Chest_data.diff",
                "Merchant_data.diff",
                "Automap.diff"
            };

            var writer = new DataWriter();

            writer.Write((byte)1); // only 1 entry for now (episode I to II)

            // For each entry do the following

            writer.Write(CreateEpisodeMappingHeader()); // identifies the mapping (high nibble = source episode, low nibble = target episode, 0x12 = 1 to 2)
            int sizeIndex = writer.Position;
            writer.Write((uint)0); // size placeholder

            // Just concat the files and prepend them with the size as a word
            foreach (var file in files)
            {
                string path = Path.Combine(diffPath, file);

                if (!File.Exists(path))
                {
                    Console.WriteLine($"File {path} was not found.");
                    Environment.Exit(1);
                    return;
                }

                var diffData = File.ReadAllBytes(path);

                if (diffData.Length == 0 || diffData.Length > ushort.MaxValue)
                {
                    Console.WriteLine($"File {path} is empty or greater than {ushort.MaxValue} bytes.");
                    Environment.Exit(2);
                    return;
                }

                writer.Write((ushort)diffData.Length);
                writer.Write(diffData);
            }

            writer.Replace(sizeIndex, (uint)(writer.Size - sizeIndex - 4));

            using var outFile = File.Create(outPath);
            writer.CopyTo(outFile);
        }
    }
}