using Ambermoon.Data.Legacy.Serialization;
using System.Text.RegularExpressions;

namespace DiffPacker
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            string diffPath = args[0];
            string outPath = args[1];
            var versions = new Dictionary<byte, List<string>>();

            string[] files = new string[5]
            {
                "Party_data.diff",
                "Party_char.diff",
                "Chest_data.diff",
                "Merchant_data.diff",
                "Automap.diff"
            };

            foreach (var file in Directory.GetFiles(diffPath))
            {
                string fname = Path.GetFileName(file);

                if (FileRegex().IsMatch(fname) && files.Any(file.EndsWith))
                {
                    var match = FileRegex().Match(fname);
                    byte episode = byte.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);

                    if (!versions.ContainsKey(episode))
                        versions[episode] = new List<string>();

                    versions[episode].Add(file);
                }
            }

            var writer = new DataWriter();

            writer.Write((byte)versions.Count);

            // For each entry do the following
            foreach (var version in versions)
            {
                writer.Write(version.Key); // identifies the mapping (high nibble = source episode, low nibble = target episode, 0x12 = 1 to 2)
                int sizeIndex = writer.Position;
                writer.Write((uint)0); // size placeholder

                // Just concat the files and prepend them with the size as a word
                foreach (var f in files)
                {
                    var path = version.Value.FirstOrDefault(fn => fn.EndsWith(f));

                    if (path == null)
                    {
                        writer.Write((ushort)0);
                        continue;
                    }

                    var diffData = File.ReadAllBytes(path);

                    if (diffData.Length > ushort.MaxValue)
                    {
                        Console.WriteLine($"File {path} is empty or greater than {ushort.MaxValue} bytes.");
                        Environment.Exit(2);
                        return;
                    }

                    writer.Write((ushort)diffData.Length);

                    if (diffData.Length == 0)
                        Console.WriteLine($"WARNING: File {path} is empty.");
                    else
                        writer.Write(diffData);
                }

                writer.Replace(sizeIndex, (uint)(writer.Size - sizeIndex - 4));
            }

            using var outFile = File.Create(outPath);
            writer.CopyTo(outFile);
        }

        [GeneratedRegex("^([1-9]{2})_(.*\\.diff)$")]
        private static partial Regex FileRegex();
    }
}