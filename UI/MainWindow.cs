using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DocumentsPlease.Logic;
using DocumentsPlease.Models;

namespace DocumentsPlease.UI;

/// <summary>Hlavní okno: plátno + lišta s tlačítky. Řídí jen vstup, logiku má GameEngine.</summary>
public sealed class MainWindow : Window
{
    private readonly GameEngine _engine = new();
    private readonly GameView _view;
    private readonly DispatcherTimer _timer;

    private readonly FlatButton _allow;
    private readonly FlatButton _detain;
    private readonly FlatButton _alarm;
    private readonly FlatButton _inspect;
    private readonly FlatButton _advance;

    public MainWindow()
    {
        Title = "Documents, Please";
        Width = 1200;
        Height = 800;
        MinWidth = 1000;
        MinHeight = 700;
        Background = new SolidColorBrush(Palette.BgDeep);

        _view = new GameView(_engine);

        _allow = Make("PUSTIT   [1]", Palette.Rgb(58, 104, 58), () => _engine.Decide(Decision.Allow));
        _detain = Make("ZADRŽET   [2]", Palette.Rgb(154, 102, 34), () => _engine.Decide(Decision.Detain));
        _alarm = Make("POPLACH   [3]", Palette.Rgb(150, 46, 40), () => _engine.Decide(Decision.Alarm));
        _inspect = Make("PROHLÍDKA   [I]", Palette.Rgb(52, 66, 82), () => _engine.Inspect());
        _advance = Make("POKRAČOVAT   [ENTER]", Palette.Rgb(74, 92, 60), () => _engine.Advance(), 330);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _allow, _detain, _alarm, _inspect, _advance }
        };

        var bar = new Border
        {
            Height = 78,
            Background = new SolidColorBrush(Palette.Rgb(13, 16, 15)),
            BorderBrush = new SolidColorBrush(Palette.Accent),
            BorderThickness = new Thickness(0, 2, 0, 0),
            Child = buttons
        };

        var root = new DockPanel();
        DockPanel.SetDock(bar, Dock.Bottom);
        root.Children.Add(bar);
        root.Children.Add(_view); // poslední dítě vyplní zbytek

        Content = root;

        _engine.Changed += OnEngineChanged;
        KeyDown += OnKeyDown;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) => _view.InvalidateVisual();
        _timer.Start();

        UpdateButtons();
    }

    private static FlatButton Make(string text, Color color, Action onClick, double width = 180)
    {
        var button = new FlatButton(text, color, width);
        button.Clicked += onClick;
        return button;
    }

    private void OnEngineChanged()
    {
        UpdateButtons();
        _view.InvalidateVisual();
    }

    private void UpdateButtons()
    {
        bool playing = _engine.State == GameState.Inspecting;

        _allow.IsVisible = playing;
        _detain.IsVisible = playing;
        _alarm.IsVisible = playing;
        _inspect.IsVisible = playing;
        _advance.IsVisible = !playing;

        bool inspected = _engine.InspectionLines != null;
        _inspect.Active = playing && !inspected;
        _inspect.Text = inspected
            ? "PROHLÉDNUTO"
            : $"PROHLÍDKA   [I]   −{EconomyManager.InspectionCost}";

        _advance.Text = _engine.State switch
        {
            GameState.MainMenu => "NASTOUPIT DO SLUŽBY   [ENTER]",
            GameState.ShiftIntro => "OTEVŘÍT OKÉNKO   [ENTER]",
            GameState.ShowingResult => "DALŠÍ NÁVŠTĚVNÍK   [ENTER]",
            GameState.ShiftSummary => "POKRAČOVAT   [ENTER]",
            GameState.GameOver => "NOVÁ HRA   [ENTER]",
            _ => "POKRAČOVAT   [ENTER]"
        };
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        bool handled = true;

        switch (e.Key)
        {
            case Key.D1:
            case Key.NumPad1:
                _engine.Decide(Decision.Allow);
                break;
            case Key.D2:
            case Key.NumPad2:
                _engine.Decide(Decision.Detain);
                break;
            case Key.D3:
            case Key.NumPad3:
                _engine.Decide(Decision.Alarm);
                break;
            case Key.I:
                _engine.Inspect();
                break;
            case Key.Enter:
            case Key.Space:
                _engine.Advance();
                break;
            default:
                handled = false;
                break;
        }

        e.Handled = handled;
    }
}
