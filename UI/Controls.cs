using System.Drawing;
using System.Windows.Forms;

namespace DocumentsPlease.UI;

/// <summary>Panel s dvojitým bufferem – kreslení bez blikání.</summary>
internal sealed class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);
    }
}

/// <summary>Tlačítko, které nebere fokus – klávesy tak vždy zpracuje formulář.</summary>
internal sealed class NoFocusButton : Button
{
    public NoFocusButton()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
    }
}
