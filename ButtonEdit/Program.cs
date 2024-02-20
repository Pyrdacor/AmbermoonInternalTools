using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Ambermoon.Data;
using Ambermoon.Data.Enumerations;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using Color = Ambermoon.Data.Enumerations.Color;

namespace ButtonEdit
{
	internal class Program
	{
		static void Usage()
		{
			Console.WriteLine("Usage: ButtonEdit <op> <data-file> <png-folder>");
			Console.WriteLine();
			Console.WriteLine("Examples:");
			Console.WriteLine();
			Console.WriteLine("ButtonEdit -e Button_graphics button-images");
			Console.WriteLine("ButtonEdit -i Button_graphics button-images");
		}

		static void Main(string[] args)
		{
			if (args.Length != 3 || (args[0] != "-e" && args[0] != "-i"))
			{
				Usage();
				return;
			}

			if (args[0] == "-e")
				Export(args);
			else
				Import(args);
		}

		static readonly ButtonType[] buttonTypes = new ButtonType[]
		{
			ButtonType.Yes,
			ButtonType.No,
			ButtonType.Ok,
			ButtonType.Exit,
			ButtonType.Quit,
			ButtonType.Opt
		};

		static void Export(string[] args)
		{
			string dataFile = args[1];
			string pngFolder = args[2];

			var buttonData = new FileReader().ReadFile("", new DataReader(File.ReadAllBytes(dataFile))).Files[1];
			var palette = Resource.Palette;

			// There are 78 buttons, each 32x13 pixels with 3bpp and palette offset 24.
			// But we only need 6 buttons which contain text.
			const int buttonSize = 32 * 13 * 3 / 8;
			var graphicReader = new GraphicReader();
			var graphicInfo = new GraphicInfo
			{
				Width = 32,
				Height = 13,
				GraphicFormat = GraphicFormat.Palette3Bit,
				PaletteOffset = 24,
				Alpha = false
			};

			Graphic LoadButtonGraphic(IDataReader dataReader)
			{
				var graphic = new Graphic();
				graphicReader.ReadGraphic(graphic, dataReader, graphicInfo);
				return graphic;
			}

			byte[] ToBitmapData(Graphic graphic)
			{
				byte[] data = new byte[graphic.Width * graphic.Height * 4];
				int size = data.Length / 4;

				for (int i = 0; i < size; i++)
				{
					if (graphic.Data[i] == 24)
					{
						data[i * 4 + 3] = 0xff;
					}
					else
					{
						new ReadOnlySpan<byte>(palette, graphic.Data[i] * 4, 4).CopyTo(new Span<byte>(data, i * 4, 4));
						(data[i * 4], data[i * 4 + 2]) = (data[i * 4 + 2], data[i * 4]);
					}
				}

				return data;
			}

			Directory.CreateDirectory(pngFolder);

			foreach (var buttonType in buttonTypes)
			{
				buttonData.Position = (int)buttonType * buttonSize;
				var buttonGraphicData = ToBitmapData(LoadButtonGraphic(buttonData));
				var buttonImage = new Bitmap(32, 13, PixelFormat.Format32bppArgb);
				var imageData = buttonImage.LockBits(new Rectangle(0, 0, 32, 13), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

				Marshal.Copy(buttonGraphicData, 0, imageData.Scan0, buttonGraphicData.Length);

				buttonImage.UnlockBits(imageData);
				buttonImage.Save(Path.Combine(pngFolder, buttonType.ToString() + ".png"));
			}
		}

		class ColorComponentNode
		{
			public byte Value { get; init; }
			private int? ColorIndex { get; set; }
			private Dictionary<byte, ColorComponentNode> Next { get; } = new();

			public void Add(byte[] data, int index, int length, int colorIndex)
			{
				if (length == 0)
				{
					ColorIndex = colorIndex;
					return;
				}

				if (!Next.TryGetValue(data[index], out var next))
				{
					next = new ColorComponentNode() { Value = data[index] };
					Next.Add(data[index], next);
				}

				next.Add(data, index + 1, length - 1, colorIndex);
			}

			public int FindColorIndex(byte[] data, int index)
			{
				return ColorIndex ?? Next[data[index]].FindColorIndex(data, index + 1);
			}
		}

		static void Import(string[] args)
		{
			string dataFile = args[1];
			string pngFolder = args[2];
			var palette = Resource.Palette;
			var paletteLookup = new ColorComponentNode();

			var data = new FileReader().ReadFile("", new DataReader(File.ReadAllBytes(dataFile))).Files[1];
			var writer = new DataWriter();
			writer.Write(data.ReadToEnd());

			paletteLookup.Add(new byte[4] { 0, 0, 0, 0xff }, 0, 4, 0);

			for (int i = 25; i < 32; i++)
			{
				paletteLookup.Add(palette, i * 4, 4, i - 24);
			}

			byte GetColorIndex(byte[] data, int index)
			{
				(data[index], data[index + 2]) = (data[index + 2], data[index]);
				return (byte)paletteLookup.FindColorIndex(data, index);
			}

			foreach (var buttonType in buttonTypes)
			{
				var buttonImage = new Bitmap(Path.Combine(pngFolder, buttonType.ToString() + ".png"));
				var buttonData = new byte[32 * 13 * 4];
				var imageData = buttonImage.LockBits(new Rectangle(0, 0, 32, 13), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

				Marshal.Copy(imageData.Scan0, buttonData, 0, buttonData.Length);

				buttonImage.UnlockBits(imageData);

				var graphic = new Graphic
				{
					Width = 32,
					Height = 13,
					Data = new byte[32 * 13],
					IndexedGraphic = true
				};

				for (int i = 0; i < graphic.Data.Length; i++)
				{
					graphic.Data[i] = GetColorIndex(buttonData, i * 4);
				}

				var graphicInfo = new GraphicInfo
				{
					Width = 32,
					Height = 13,
					GraphicFormat = GraphicFormat.Palette3Bit,
					PaletteOffset = 24,
					Alpha = false
				};

				const int buttonSize = 32 * 13 * 3 / 8;
				var buttonWriter = new DataWriter();
				GraphicWriter.WriteGraphic(graphic, buttonWriter, graphicInfo);
				writer.Replace((int)buttonType * buttonSize, buttonWriter.ToArray());
			}

			var outputWriter = new DataWriter();
			FileWriter.WriteJH(outputWriter, writer.ToArray(), 0xd2e7, true);
			File.WriteAllBytes(dataFile, outputWriter.ToArray());
		}
	}
}
