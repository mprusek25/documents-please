using Avalonia.Media;

namespace DocumentsPlease.UI;

/// <summary>Jednotná barevná paleta hry – ponurý úřad v karanténní zóně.</summary>
public static class Palette
{
    public static Color Rgb(int r, int g, int b, int a = 255) => Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);

    // pozadí
    public static readonly Color BgDeep = Rgb(10, 12, 11);
    public static readonly Color BgRoom = Rgb(21, 26, 24);
    public static readonly Color Panel = Rgb(28, 35, 33);
    public static readonly Color PanelHi = Rgb(37, 46, 43);
    public static readonly Color Rule = Rgb(58, 69, 65);

    // text
    public static readonly Color Ink = Rgb(232, 230, 221);
    public static readonly Color InkDim = Rgb(140, 148, 143);
    public static readonly Color InkFaint = Rgb(95, 103, 99);

    // akcenty
    public static readonly Color Accent = Rgb(143, 168, 107);
    public static readonly Color Good = Rgb(124, 176, 112);
    public static readonly Color Warn = Rgb(200, 132, 43);
    public static readonly Color Danger = Rgb(181, 53, 46);

    // papír
    public static readonly Color PaperManual = Rgb(220, 214, 194);
    public static readonly Color PaperId = Rgb(201, 212, 224);
    public static readonly Color PaperPermit = Rgb(205, 217, 196);
    public static readonly Color PaperInk = Rgb(34, 35, 31);
    public static readonly Color PaperDim = Rgb(104, 104, 94);
}
