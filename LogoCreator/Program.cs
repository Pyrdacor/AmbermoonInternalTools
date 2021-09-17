using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LogoCreator
{
    class Program
    {
        enum CommandType
        {
            Wait,
            Blend,
            Replace,
            FadeOut,
            PrintText
        }

        struct Command
        {
            public CommandType Type;
            public uint Time;
            public int ImageIndex;
            public byte[] Parameters;
        }

        static readonly Regex SizeRegex = new Regex(@"([0-9]+) ([0-9]+)", RegexOptions.Compiled);
        static readonly Regex NoParamRegex = new Regex(@"([0-9]+):([0-9])", RegexOptions.Compiled);
        static readonly Regex ImageParamRegex = new Regex(@"([0-9]+):([0-9]):([0-9])", RegexOptions.Compiled);
        static readonly Regex AreaRegex = new Regex(@"([0-9]+):([0-9]):([0-9]+) ([0-9]+) \.\. ([0-9]+) ([0-9]+):([0-9]+)", RegexOptions.Compiled);
        static readonly Regex TextParamRegex = new Regex(@"([0-9]+):([0-9]):([ a-zA-Z\(\)0-9\.,_-]+)", RegexOptions.Compiled);

        static void Main(string[] args)
        {
            var commandLines = File.ReadAllLines(args[0]);
            var image = (Bitmap)Image.FromFile(args[1]);

            Size? frameSize = null;
            var commandList = new List<Command>();
            int lineNumber = 1;

            foreach (var line in commandLines)
            {
                var command = line.Trim();

                if (command.Length == 0 || command.StartsWith('#'))
                {
                    ++lineNumber;
                    continue;
                }

                if (frameSize == null)
                {
                    var match = SizeRegex.Match(command);

                    if (!match.Success || match.Length != command.Length)
                        throw new Exception($"Invalid size format in line {lineNumber}.");

                    frameSize = new Size(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
                }
                else
                {
                    var match = NoParamRegex.Match(command);

                    if (match.Success && match.Length == command.Length)
                    {
                        commandList.Add(new Command
                        {
                            Type = (CommandType)int.Parse(match.Groups[2].Value),
                            Time = uint.Parse(match.Groups[1].Value)
                        });
                    }
                    else
                    {
                        match = AreaRegex.Match(command);

                        if (match.Success && match.Length == command.Length)
                        {
                            commandList.Add(new Command
                            {
                                Type = (CommandType)int.Parse(match.Groups[2].Value),
                                Time = uint.Parse(match.Groups[1].Value),
                                Parameters = new byte[4]
                                {
                                    byte.Parse(match.Groups[3].Value),
                                    byte.Parse(match.Groups[4].Value),
                                    byte.Parse(match.Groups[5].Value),
                                    byte.Parse(match.Groups[6].Value)
                                },
                                ImageIndex = int.Parse(match.Groups[7].Value)
                            });
                        }
                        else
                        {
                            match = ImageParamRegex.Match(command);

                            if (match.Success && match.Length == command.Length)
                            {
                                commandList.Add(new Command
                                {
                                    Type = (CommandType)int.Parse(match.Groups[2].Value),
                                    Time = uint.Parse(match.Groups[1].Value),
                                    ImageIndex = int.Parse(match.Groups[3].Value)
                                });
                            }
                            else
                            {
                                match = TextParamRegex.Match(command);

                                if (!match.Success || match.Length != command.Length)
                                    throw new Exception($"Invalid command in line {lineNumber}.");

                                commandList.Add(new Command
                                {
                                    Type = (CommandType)int.Parse(match.Groups[2].Value),
                                    Time = uint.Parse(match.Groups[1].Value),
                                    Parameters = Encoding.UTF8.GetBytes(match.Groups[3].Value)
                                });
                            }
                        }
                    }
                }

                ++lineNumber;
            }

            if (frameSize == null)
                throw new Exception("No frame size given.");
            var outputData = ProcessCommands(commandList);
            outputData.AddRange(CreateCompressedAtlas(image, frameSize.Value));

            using var outputStream = File.Create(args[2]);
            using var deflateStream = new System.IO.Compression.DeflateStream(outputStream, System.IO.Compression.CompressionLevel.Optimal);

            deflateStream.Write(outputData.ToArray());
        }

        static List<byte> ProcessCommands(List<Command> commands)
        {
            if (commands.Count == 0)
                throw new Exception("No commands given.");
            if (commands.Count > 255)
                throw new Exception("Too many commands. Max is 255.");

            var output = new List<byte>();

            output.Add((byte)commands.Count);

            foreach (var command in commands)
            {
                output.Add((byte)(command.Time >> 8));
                output.Add((byte)(command.Time & 0xff));
                output.Add((byte)command.Type);

                if (command.Type == CommandType.Blend)
                {
                    if (command.Parameters == null)
                        throw new Exception($"No parameters given for command 'Blend'");

                    for (int i = 0; i < 4; ++i)
                        output.Add(command.Parameters[i]);

                    output.Add((byte)command.ImageIndex);
                }
                else if (command.Type == CommandType.Replace)
                {
                    output.Add((byte)command.ImageIndex);
                }
                else if (command.Type == CommandType.PrintText)
                {
                    if (command.Parameters == null)
                        throw new Exception($"No parameters given for command 'Blend'");

                    output.Add((byte)command.Parameters.Length);

                    for (int i = 0; i < command.Parameters.Length; ++i)
                        output.Add(command.Parameters[i]);
                }
            }

            return output;
        }

        static List<byte> CreateCompressedAtlas(Bitmap image, Size frameSize)
        {
            var output = new List<byte>();
            var palette = new List<Color>();
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var buffer = new byte[image.Width * image.Height * 4];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            image.UnlockBits(data);

            int numFrames = image.Width / frameSize.Width;
            var colorIndices = new byte[image.Width * image.Height];

            for (int y = 0; y < image.Height; ++y)
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    int index = x + y * image.Width;
                    byte b = buffer[index * 4 + 0];
                    byte g = buffer[index * 4 + 1];
                    byte r = buffer[index * 4 + 2];
                    byte a = buffer[index * 4 + 3];
                    var color = Color.FromArgb(a, r, g, b);

                    int colorIndex = palette.IndexOf(color);

                    if (colorIndex == -1)
                    {
                        if (palette.Count == 32)
                            throw new Exception("Too many colors in image. 32 is max.");
                        colorIndex = palette.Count;
                        palette.Add(color);
                    }

                    colorIndices[index] = (byte)colorIndex;
                }
            }

            // Store palette
            for (int i = 0; i < 32; ++i)
            {
                if (i >= palette.Count)
                {
                    output.Add(0);
                    output.Add(0);
                    output.Add(0);
                    output.Add(0);
                }
                else
                {
                    var color = palette[i];
                    output.Add(color.R);
                    output.Add(color.G);
                    output.Add(color.B);
                    output.Add(color.A);
                }
            }

            // Store frame size
            output.Add((byte)frameSize.Width);
            output.Add((byte)frameSize.Height);

            var pixelData = new byte[colorIndices.Length];

            for (int y = 0; y < frameSize.Height; ++y)
            {
                Array.Copy(colorIndices, y * image.Width, pixelData, y * image.Width, frameSize.Width);
            }

            for (int f = 1; f < numFrames; ++f)
            {
                for (int y = 0; y < frameSize.Height; ++y)
                {
                    for (int x = 0; x < frameSize.Width; ++x)
                    {
                        int tx = f * frameSize.Width + x;
                        int index = y * image.Width + tx;
                        int prevIndex = index - frameSize.Width;
                        pixelData[index] = unchecked((byte)(sbyte)(colorIndices[index] - colorIndices[prevIndex]));
                    }
                }
            }

            output.AddRange(pixelData);

            return output;
        }
    }
}
