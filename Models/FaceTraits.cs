namespace DocumentsPlease.Models;

/// <summary>
/// Rysy obličeje – používají se pro skutečnou osobu i pro fotografii v OP.
/// Barvy jsou jen indexy do palet (vykreslování je řeší samo), logika tak nezávisí na System.Drawing.
/// </summary>
public sealed record FaceTraits(int SkinTone, int HairColor, HairStyle Hair, bool Glasses, bool Beard)
{
    public const int SkinToneCount = 4;
    public const int HairColorCount = 5;
}
