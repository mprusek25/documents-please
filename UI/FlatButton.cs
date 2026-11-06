using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace DocumentsPlease.UI;

/// <summary>
/// Hranaté tlačítko ve stylu hry. Postavené na Borderu, aby ho Fluent téma
/// nepřebarvovalo při najetí myší.
/// </summary>
public sealed class FlatButton : Border
{
    private readonly TextBlock _label;
    private readonly Color _base;
    private bool _hot;
    private bool _down;
    private bool _enabled = true;

    public event Action? Clicked;

    public FlatButton(string text, Color baseColor, double width = 186, double height = 50)
    {
        _base = baseColor;

        _label = new TextBlock
        {
            Text = text,
            FontFamily = FontFamily.Default,
            Foreground = new SolidColorBrush(Palette.Ink),
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        Width = width;
        Height = height;
        Child = _label;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(2);
        Cursor = new Cursor(StandardCursorType.Hand);

        PointerEntered += (_, _) => { _hot = true; Repaint(); };
        PointerExited += (_, _) => { _hot = false; _down = false; Repaint(); };
        PointerPressed += (_, _) => { _down = true; Repaint(); };
        PointerReleased += (_, _) =>
        {
            bool fire = _down && _enabled;
            _down = false;
            Repaint();
            if (fire) Clicked?.Invoke();
        };

        Repaint();
    }

    public string Text
    {
        get => _label.Text ?? "";
        set => _label.Text = value;
    }

    public bool Active
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            Cursor = new Cursor(value ? StandardCursorType.Hand : StandardCursorType.Arrow);
            Repaint();
        }
    }

    private void Repaint()
    {
        Color fill = _base;

        if (!_enabled) fill = Mix(_base, Palette.BgRoom, 0.62);
        else if (_down) fill = Mix(_base, Colors.Black, 0.30);
        else if (_hot) fill = Mix(_base, Colors.White, 0.16);

        Background = new SolidColorBrush(fill);
        BorderBrush = new SolidColorBrush(Mix(fill, Colors.White, _enabled ? 0.22 : 0.06));
        _label.Foreground = new SolidColorBrush(_enabled ? Palette.Ink : Palette.InkFaint);
    }

    private static Color Mix(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t));
}
