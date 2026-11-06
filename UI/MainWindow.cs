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

    private readonly Button _allow;
    private readonly Button _detain;
    private readonly Button _alarm;
    private readonly Button _inspect;
    private readonly Button _advance;

    public MainWindow()
    {
        Title = "Documents, Please";
        Width = 1200;
        Height = 800;
        MinWidth = 960;
        MinHeight = 680;
        Background = new SolidColorBrush(Color.Parse("#0E0F12"));

        _view = new GameView(_engine);

        _allow = MakeButton("PUSTIT  [1]", "#2E7D32", () => _engine.Decide(Decision.Allow));
        _detain = MakeButton("ZADRŽET  [2]", "#D87814", () => _engine.Decide(Decision.Detain));
        _alarm = MakeButton("POPLACH  [3]", "#C62828", () => _engine.Decide(Decision.Alarm));
        _inspect = MakeButton("PROHLÍDKA  [I]", "#37476E", () => _engine.Inspect());
        _advance = MakeButton("Pokračovat  [Enter]", "#37476E", () => _engine.Advance());

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _allow, _detain, _alarm, _inspect, _advance }
        };

        var bar = new Border
        {
            Height = 80,
            Background = new SolidColorBrush(Color.Parse("#0E0F12")),
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

    private static Button MakeButton(string text, string color, Action onClick)
    {
        var button = new Button
        {
            Content = text,
            Width = 190,
            Height = 52,
            Background = new SolidColorBrush(Color.Parse(color)),
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            FontSize = 14,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Focusable = false // klávesy tak vždy zpracuje okno
        };
        button.Click += (_, _) => onClick();
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
        _inspect.IsEnabled = playing && !inspected;
        _inspect.Content = inspected
            ? "PROHLÉDNUTO"
            : $"PROHLÍDKA  [I]  (−{EconomyManager.InspectionCost})";

        _advance.Width = 340;
        _advance.Content = _engine.State switch
        {
            GameState.MainMenu => "Nová hra  [Enter]",
            GameState.ShiftIntro => "Zahájit směnu  [Enter]",
            GameState.ShowingResult => "Další návštěvník  [Enter]",
            GameState.ShiftSummary => "Pokračovat  [Enter]",
            GameState.GameOver => "Nová hra  [Enter]",
            _ => "Pokračovat  [Enter]"
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
