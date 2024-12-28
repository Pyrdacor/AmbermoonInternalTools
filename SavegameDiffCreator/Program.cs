using Ambermoon.Data.Legacy;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace SavegameDiffCreator;

internal class Program
{
    // Order matters. Do not change!
    static readonly string[] SaveFileNames =
    [
        "Party_data.sav",
        "Party_char.amb",
        "Chest_data.amb",
        "Merchant_data.amb",
        "Automap.amb"
    ];

    static int currentSubfileIndex = 0;
    static int numActions = 0;

    enum DiffType : byte
    {
        ByteValueChange,
        WordValueChange,
        BitfieldBitsAdded,
        BitfieldBitsCleared,
        SubfileAdded,
        SubfileRemoved,
        SubfileExtended,
        SubfileShrunk,
        ByteReplacement,
        AddInventoryItem,
        SetSubfile,
    }

    static void Main(string[] args)
    {
        // args[0] = source savegame folder (Save.00)
        // args[1] = target savegame folder (Save.00)
        // args[2] = episode key (0xST) where S is the source episode and T is the target episode
        // args[3] = output directory

        var sourceGameData = new GameData(GameData.LoadPreference.ForceExtracted);
        sourceGameData.Load(args[0], true);

        var targetGameData = new GameData(GameData.LoadPreference.ForceExtracted);
        targetGameData.Load(args[1], true);

        // Ep1 -> Ep2 algorithm was lost. I could reconstruct it but we use something new
        // now.

        ushort index = 0;

        foreach (var file in SaveFileNames)
        {
            currentSubfileIndex = 0;
            numActions = 0;

            var sourceContainer = sourceGameData.Files[$"Save.00/{file}"];
            var targetContainer = targetGameData.Files[$"Save.00/{file}"];

            var writer = new DataWriter();

            if (index == 0)
            {
                DiffPartyData(writer, sourceContainer.Files[1].ReadToEnd(), targetContainer.Files[1].ReadToEnd());
            }
            else
            {
                var indices = sourceContainer.Files.Keys.Concat(targetContainer.Files.Keys).Distinct().OrderBy(x => x).ToArray();

                foreach (var subFileIndex in indices)
                {
                    if (sourceContainer.Files.TryGetValue(subFileIndex, out var sourceSubFile) &&
                        targetContainer.Files.TryGetValue(subFileIndex, out var targetSubFile))
                    {
                        byte[] targetData = [];

                        if (sourceSubFile.Size != targetSubFile.Size)
                        {
                            if (sourceSubFile.Size == 0)
                            {
                                Console.WriteLine($"Subfile {file} [{subFileIndex}] added.");
                                AddSubfileAdded(writer, (ushort)subFileIndex, targetSubFile.ReadToEnd());
                                continue;
                            }
                            else if (targetSubFile.Size == 0)
                            {
                                Console.WriteLine($"Subfile {file} [{subFileIndex}] removed.");
                                AddSubfileRemoved(writer, (ushort)subFileIndex);
                                continue;
                            }
                            else if (sourceSubFile.Size < targetSubFile.Size)
                            {
                                Console.WriteLine($"Subfile {file} [{subFileIndex}] extended.");
                                targetData = targetSubFile.ReadBytes(sourceSubFile.Size);
                                AddSubfileExtended(writer, (ushort)subFileIndex, targetSubFile.ReadToEnd());
                            }
                            else
                            {
                                if (index != 4)
                                    throw new Exception($"Shrinking sub files is not supported except for automaps. {file} [{subFileIndex}]");

                                Console.WriteLine($"Subfile {file} [{subFileIndex}] shrunk.");
                                AddSubfileShrunk(writer, (ushort)subFileIndex, (ushort)targetSubFile.Size);
                            }
                        }

                        if (targetData.Length == 0)
                            targetData = targetSubFile.ReadToEnd();

                        if (sourceSubFile.Size == 0)
                            continue;

                        if (index == 1)
                            DiffPartyChar(writer, (ushort)subFileIndex, sourceSubFile.ReadToEnd(), targetData);
                        else if (index < 4)
                            DiffChestOrMerchantData(writer, (ushort)subFileIndex, sourceSubFile.ReadToEnd(), targetData, index == 2);
                        else
                            DiffAutomap(writer, (ushort)subFileIndex, sourceSubFile.ReadToEnd(), targetData);
                    }
                    else if (sourceContainer.Files.ContainsKey(subFileIndex))
                    {
                        Console.WriteLine($"Subfile {file} [{subFileIndex}] removed.");
                        AddSubfileRemoved(writer, (ushort)subFileIndex);
                    }
                    else
                    {
                        Console.WriteLine($"Subfile {file} [{subFileIndex}] added.");
                        AddSubfileAdded(writer, (ushort)subFileIndex, targetContainer.Files[subFileIndex].ReadToEnd());
                    }
                }
            }

            var stream = File.Create(Path.Combine(args[3], $"{args[2]}_" + file[..^3] + "diff"));
            using var binaryWriter = new BinaryWriter(stream);
            binaryWriter.Write((byte)((numActions >> 8) & 0xff));
            binaryWriter.Write((byte)(numActions & 0xff));
            binaryWriter.Write(writer.ToArray());

            index++;
        }
    }

    private static void AddByteChange(IDataWriter writer, ushort index, short change)
    {
        Console.WriteLine($"Change byte at {index} by {change}");
        writer.Write((byte)DiffType.ByteValueChange);
        writer.Write(index);
        writer.Write(unchecked((ushort)change));

        ++numActions;
    }

    private static void AddWordChange(IDataWriter writer, ushort index, int change)
    {
        Console.WriteLine($"Change word at {index} by {change}");
        writer.Write((byte)DiffType.WordValueChange);
        writer.Write(index);
        writer.Write(unchecked((uint)change));

        ++numActions;
    }

    private static string ToBitString(byte b)
    {
        return Convert.ToString(b, 2).PadLeft(8, '0');
    }

    private static void AddBitfieldBitsAdded(IDataWriter writer, ushort index, byte bitfield)
    {
        Console.WriteLine($"Bits added at {index}: {ToBitString(bitfield)}");
        writer.Write((byte)DiffType.BitfieldBitsAdded);
        writer.Write(index);
        writer.Write(bitfield);

        ++numActions;
    }

    private static void AddBitfieldBitsCleared(IDataWriter writer, ushort index, byte bitfield)
    {
        Console.WriteLine($"Bits cleared at {index}: {ToBitString(bitfield)}");
        writer.Write((byte)DiffType.BitfieldBitsCleared);
        writer.Write(index);
        writer.Write(bitfield);

        ++numActions;
    }

    private static void AddSubfileAdded(IDataWriter writer, ushort index, byte[] data)
    {
        writer.Write((byte)DiffType.SubfileAdded);
        writer.Write(index);
        writer.Write((ushort)data.Length);
        writer.Write(data);

        ++numActions;
    }

    private static void AddSubfileRemoved(IDataWriter writer, ushort index)
    {
        writer.Write((byte)DiffType.SubfileRemoved);
        writer.Write(index);

        ++numActions;
    }

    private static void AddSubfileExtended(IDataWriter writer, ushort index, byte[] data)
    {
        Console.WriteLine($"Extending by {string.Join(' ', data.Select(b => b.ToString("x2")))}");
        writer.Write((byte)DiffType.SubfileExtended);
        writer.Write(index);
        writer.Write((ushort)data.Length);
        writer.Write(data);

        ++numActions;
    }

    private static void AddSubfileShrunk(IDataWriter writer, ushort index, ushort size)
    {
        Console.WriteLine($"Shrinking to {size}");
        writer.Write((byte)DiffType.SubfileShrunk);
        writer.Write(index);
        writer.Write(size);

        ++numActions;
    }

    private static void AddByteReplacement(IDataWriter writer, ushort index, byte replacement)
    {
        Console.WriteLine($"Replace byte {index} with {replacement:x2}");
        writer.Write((byte)DiffType.ByteReplacement);
        writer.Write(index);
        writer.Write(replacement);

        ++numActions;
    }

    private static void AddInventoryItem(IDataWriter writer, byte amount, byte[] remainingSlotData)
    {
        Console.WriteLine($"Add item {amount}x {(remainingSlotData[3] << 8) | remainingSlotData[4]}");
        writer.Write((byte)DiffType.AddInventoryItem);
        writer.Write(amount);
        writer.Write(remainingSlotData);

        ++numActions;
    }

    private static void SetSubfile(IDataWriter writer, ushort index)
    {
        Console.WriteLine($"Processing subfile {index}");
        writer.Write((byte)DiffType.SetSubfile);
        writer.Write(index);

        ++numActions;
    }

    private static short ByteDiff(byte newByte, byte oldByte)
    {
        return (short)(newByte - oldByte);
    }

    private static int WordDiff(byte[] newData, byte[] oldData, int offset)
    {
        int newWord = (newData[offset] << 8) | newData[offset + 1];
        int oldWord = (oldData[offset] << 8) | oldData[offset + 1];

        return newWord - oldWord;
    }

    private static void AddBitfieldBitsChange(IDataWriter writer, int offset, byte newValue, byte oldValue, bool addOnly = false)
    {
        byte addedBits = (byte)(newValue & ~oldValue);
        byte removedBits = (byte)(oldValue & ~newValue);

        if (addedBits != 0)
            AddBitfieldBitsAdded(writer, (ushort)offset, addedBits);

        if (!addOnly && removedBits != 0)
            AddBitfieldBitsCleared(writer, (ushort)offset, removedBits);
    }

    private static void EnsureSubfile(IDataWriter writer, int subfile)
    {
        if (currentSubfileIndex != subfile)
        {
            SetSubfile(writer, (ushort)subfile);
            currentSubfileIndex = subfile;
        }
    }

    private static void DiffChestOrMerchantData(IDataWriter writer, int subfile, byte[] oldData, byte[] newData, bool chest)
    {
        for (int i = 0; i < 24 * 6; i += 6)
        {
            if (oldData[i] != newData[i] || oldData[i + 4] != newData[i + 4] || oldData[i + 5] != newData[i + 5])
            {
                if (oldData[i] < newData[i])
                {
                    // add item
                    EnsureSubfile(writer, subfile);
                    int amount = newData[i] - oldData[i];
                    if (chest && amount > 99)
                        amount = 99;
                    else if (!chest && newData[i] == 255)
                        amount = 255;
                    AddInventoryItem(writer, (byte)amount, newData.Skip(i + 1).Take(5).ToArray());
                }
            }
        }

        if (chest)
        {
            for (int i = 24 * 6; i < 24 * 6 + 4; i++)
            {
                if (oldData[i] != newData[i])
                {
                    EnsureSubfile(writer, subfile);

                    if (i % 2 == 0)
                    {
                        AddWordChange(writer, (ushort)i, WordDiff(newData, oldData, i));
                        i++;
                    }
                    else
                        AddWordChange(writer, (ushort)(i - 1), WordDiff(newData, oldData, i - 1));
                }
            }
        }
    }

    private static void DiffAutomap(IDataWriter writer, int subfile, byte[] oldData, byte[] newData)
    {
        // only explore
        for (int i = 0; i < newData.Length; i++)
        {
            if (newData[i] != oldData[i])
            {
                byte added = (byte)(newData[i] & ~oldData[i]);

                if (added != 0)
                {
                    EnsureSubfile(writer, subfile);
                    AddBitfieldBitsAdded(writer, (ushort)i, added);
                }
            }
        }
    }

    private static void DiffPartyChar(IDataWriter writer, int subfile, byte[] oldData, byte[] newData)
    {
        for (int i = 1; i < 0x14; i++)
        {
            if (i == 6 || i == 7)
                continue;

            if (oldData[i] != newData[i])
            {
                EnsureSubfile(writer, subfile);

                if (i < 4 || i == 9 || i == 10)
                    AddByteReplacement(writer, (ushort)i, newData[i]);
                else if (i == 0x04 || i == 0x08 || i == 0x10 || i == 0x12)
                    AddBitfieldBitsChange(writer, i, newData[i], oldData[i]);
                else
                    AddByteChange(writer, (ushort)i, ByteDiff(newData[i], oldData[i]));
            }
        }

        for (int i = 0x14; i < 0xee; i++)
        {
            if (oldData[i] != newData[i])
            {
                EnsureSubfile(writer, subfile);

                if (i % 2 == 0)
                {
                    AddWordChange(writer, (ushort)i, WordDiff(newData, oldData, i));
                    i++;
                }
                else
                {
                    AddWordChange(writer, (ushort)(i - 1), WordDiff(newData, oldData, i - 1));
                }
            }
        }

        for (int i = 0xee; i < 0x122; i++)
        {
            if (oldData[i] != newData[i])
            {
                // for now we don't support long changes
                if (i < 0xee + 4)
                    Console.WriteLine($"Exp changed for char {subfile}, but we don't track it.");
                else
                {
                    if (i >= 0xf2 && i < 0x102)
                    {
                        EnsureSubfile(writer, subfile);
                        AddBitfieldBitsChange(writer, i, newData[i], oldData[i], true);
                    }
                    else if (i >= 0x112 && i < 0x122)
                    {
                        EnsureSubfile(writer, subfile);
                        AddByteReplacement(writer, (ushort)i, newData[i]);
                    }
                }
            }
        }

        // inventory items
        for (int i = 0x158; i < 0x1e8; i += 6)
        {
            if (oldData[i] != newData[i] || oldData[i + 4] != newData[i + 4] || oldData[i + 5] != newData[i + 5])
            {
                if (oldData[i] < newData[i])
                {
                    // add item
                    EnsureSubfile(writer, subfile);
                    int amount = newData[i] - oldData[i];
                    if (amount > 99)
                        amount = 99;
                    AddInventoryItem(writer, (byte)amount, newData.Skip(i + 1).Take(5).ToArray());
                }
            }
        }

        for (int i = 0x1e8; i < oldData.Length; i++)
        {
            if (oldData[i] != newData[i])
            {
                EnsureSubfile(writer, subfile);
                AddByteReplacement(writer, (ushort)i, newData[i]);
            }
        }
    }

    private static void DiffPartyData(IDataWriter writer, byte[] oldData, byte[] newData)
    {
        for (int i = 0x44; i < 0x104; i += 6)
        {
            if (oldData[i] != 0)
                continue;

            if (newData[i] == 0)
                break;
            
            for (int j = 0; j < 3; j++)
                AddByteChange(writer, (ushort)(i + j), ByteDiff(newData[i + j], oldData[i + j]));

            AddWordChange(writer, (ushort)(i + 4), WordDiff(newData, oldData, i + 4));
        }

        for (int i = 0x104; i < 0x35E4; i++)
        {
            if (oldData[i] != newData[i])
            {
                AddBitfieldBitsChange(writer, i, newData[i], oldData[i]);
            }
        }

        if (newData.Length > oldData.Length)
        {
            AddSubfileExtended(writer, 0, newData.Skip(oldData.Length).ToArray());
        }
    }
}
