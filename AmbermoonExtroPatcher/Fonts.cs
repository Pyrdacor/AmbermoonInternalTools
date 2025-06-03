using Ambermoon.Data.Serialization;

namespace AmbermoonExtroPatcher;

internal class Fonts
{
    public byte NumChars { get; }
    public byte NumGlyphs { get; }
    public byte SmallFontHeight { get; }
    public byte LargeFontHeight { get; }
    public byte UsedSmallFontHeight { get; }
    public byte UsedLargeFontHeight { get; }
    public byte SmallLineHeight { get; }
    public byte LargeLineHeight { get; }
    public byte SmallSpaceAdvance { get; }
    public byte LargeSpaceAdvance { get; }
    public byte[] GlyphMapping { get; }
    public byte[] SmallAdvanceValues { get; }
    public byte[] LargeAdvanceValues { get; }
    public byte[] SmallGlyphData { get; }
    public byte[] LargeGlyphData { get; }

    public Fonts(IDataReader reader)
    {
        NumChars = reader.ReadByte();
        NumGlyphs = reader.ReadByte();
        SmallFontHeight = reader.ReadByte();
        LargeFontHeight = reader.ReadByte();
        UsedSmallFontHeight = reader.ReadByte();
        UsedLargeFontHeight = reader.ReadByte();
        SmallLineHeight = reader.ReadByte();
        LargeLineHeight = reader.ReadByte();
        SmallSpaceAdvance = reader.ReadByte();
        LargeSpaceAdvance = reader.ReadByte();
        GlyphMapping = reader.ReadBytes(NumChars);
        SmallAdvanceValues = reader.ReadBytes(NumGlyphs);
        LargeAdvanceValues = reader.ReadBytes(NumGlyphs);
        SmallGlyphData = reader.ReadBytes(NumGlyphs * 2 * UsedSmallFontHeight);
        LargeGlyphData = reader.ReadBytes(NumGlyphs * 4 * UsedLargeFontHeight);
    }
}
