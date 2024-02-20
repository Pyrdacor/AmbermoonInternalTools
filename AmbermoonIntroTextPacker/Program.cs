using System.Text;
using Ambermoon.Data.Legacy.Serialization;
using TextWriter = Ambermoon.Data.Legacy.Serialization.TextWriter;

namespace AmbermoonIntroTextPacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var nonCommandTexts = new List<string>();
            var commandTexts = new List<string[]>();
            var currentCommandTexts = new List<string>();
            var path = @"D:\Projects\Ambermoon\Disks\Bugfixing\Czech\IntroTexts";

            foreach (var file in Directory.GetFiles(path).OrderBy(f => int.Parse(Path.GetFileName(f)[0..3])).ThenBy(f =>
            {
                var fname = Path.GetFileNameWithoutExtension(f);
                if (fname.Contains('.'))
                    return int.Parse(fname[4..7]);
                return 0;
            }))
            {
                var fname = Path.GetFileNameWithoutExtension(file);
                string text = File.ReadAllText(file, Encoding.UTF8);

                if (fname.Contains('.'))
                {
                    if (int.Parse(fname[4..7]) == 0) // New command
                    {
                        if (currentCommandTexts.Count != 0)
                        {
                            commandTexts.Add(currentCommandTexts.ToArray());
                            currentCommandTexts.Clear();
                        }
                    }

                    currentCommandTexts.Add(text);
                }
                else
                {
                    nonCommandTexts.Add(text);
                }
            }

            if (currentCommandTexts.Count != 0)
            {
                commandTexts.Add(currentCommandTexts.ToArray());
                currentCommandTexts.Clear();
            }

            var dataWriter = new DataWriter();

            dataWriter.Write((byte)nonCommandTexts.Count);
            foreach (var nonCommandText in nonCommandTexts)
                dataWriter.WriteNullTerminated(nonCommandText, Encoding.UTF8);

            dataWriter.Write((byte)commandTexts.Count);
            foreach (var commandText in commandTexts)
            {
                dataWriter.Write((byte)commandText.Length);
                foreach (var commandTextEntry in commandText)
                    dataWriter.WriteNullTerminated(commandTextEntry, Encoding.UTF8);
            }

            File.WriteAllBytes(@"D:\Projects\Ambermoon\Disks\Bugfixing\Czech\Intro_texts.amb", dataWriter.ToArray());
        }
    }
}