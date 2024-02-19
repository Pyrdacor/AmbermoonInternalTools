using System.Text;
using Ambermoon.Data.Legacy.Serialization;

namespace AmbermoonExtroTextPacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var outroTexts = new List<List<string>>[6] { new(), new(), new(), new(), new(), new() };
            int clickGroupIndex = 0;

            var path = @"D:\Projekte\Ambermoon\Disks\Bugfixing\Polish\ExtroTextGroups";

            foreach (var clickGroup in Directory.GetDirectories(path).OrderBy(d => int.Parse(Path.GetFileName(d)[0..3])))
            {
                var clickGroupTexts = outroTexts[clickGroupIndex++];

                foreach (var group in Directory.GetDirectories(clickGroup).OrderBy(d => int.Parse(Path.GetFileName(d)[0..3])))
                {
                    var groupTexts = new List<string>();

                    foreach (var file in Directory.GetFiles(group).OrderBy(f => int.Parse(Path.GetFileName(f)[0..3])))
                    {
                        string text = File.ReadAllText(file, Encoding.UTF8);
                        groupTexts.Add(text);
                    }

                    clickGroupTexts.Add(groupTexts);
                }
            }
            

            var dataWriter = new DataWriter();

            dataWriter.Write((ushort)6);

            for (int i = 0; i < 6; ++i)
                dataWriter.Write((ushort)outroTexts[i].Count);

            foreach (var clickGroup in outroTexts)
            {
                for (int i = 0; i < clickGroup.Count; ++i)
                    dataWriter.Write((ushort)clickGroup[i].Count);

                foreach (var group in clickGroup)
                {
                    foreach (var text in group)
                    {
                        dataWriter.WriteNullTerminated(text, Encoding.UTF8);
                    }
                }

                if (dataWriter.Size % 2 == 1)
                    dataWriter.Write((byte)0);
            }

            dataWriter.Write((ushort)1); // Number of translators
            dataWriter.WriteNullTerminated("galon3"); // Translator (note: ensure UTF8)

            dataWriter.WriteNullTerminated("<KLIKNIJ>", Encoding.UTF8);

            if (dataWriter.Size % 2 == 1)
                dataWriter.Write((byte)0);

            File.WriteAllBytes(@"D:\Projekte\Ambermoon\Disks\Bugfixing\Polish\Extro_texts.amb", dataWriter.ToArray());
        }
    }
}