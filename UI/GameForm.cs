using System.Drawing;
using System.Windows.Forms;
using DocumentsPlease.Logic;
using DocumentsPlease.Models;

namespace DocumentsPlease.UI;

/// <summary>Hlavní okno: plátno pro vykreslování + lišta s tlačítky. Žádná herní logika.</summary>
public sealed class GameForm : Form
{
    private readonly GameEngine _engine = new();
    private readonly GameRenderer _renderer = new();
    private readonly BufferedPanel _canvas = new() { Dock = DockStyle.Fill };
    private readonly Panel _bar = new() { Dock = DockStyle.Bottom, Height = 80, BackColor = Color.FromArgb(14, 15, 18) };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

    private readonly NoFocusButton _allow;
    private readonly NoFocusButton _detain;
    private readonly NoFocusButton _alarm;
    private readonly NoFocusButton _inspect;
    private readonly NoFocusButton _advance;

    public GameForm()
    {
        Text = "Documents, Please";
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = new Size(1200, 800);
        MinimumSize = new Size(960, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(14, 15, 18);
        KeyPreview = true;

        _allow = MakeButton("PUSTIT  [1]", Color.FromArgb(46, 125, 50), () => _engine.Decide(Decision.Allow));
        _detain = MakeButton("ZADRŽET  [2]", Color.FromArgb(216, 120, 20), () => _engine.Decide(Decision.Detain));
        _alarm = MakeButton("POPLACH  [3]", Color.FromArgb(198, 40, 40), () => _engine.Decide(Decision.Alarm));
        _inspect = MakeButton("PROHLÍDKA  [I]", Color.FromArgb(55, 71, 110), () => _engine.Inspect());
        _advance = MakeButton("Pokračovat  [Enter]", Color.FromArgb(55, 71, 110), () => _engine.Advance());

        _bar.Controls.AddRange(new Control[] { _allow, _detain, _alarm, _inspect, _advance });

        // Pořadí: nejdřív výplň, potom dock dolů (tak to dělá i designer).
        Controls.Add(_canvas);
        Controls.Add(_bar);

        _canvas.Paint += OnCanvasPaint;
        _bar.Resize += (_, _) => LayoutButtons();
        _engine.Changed += OnEngineChanged;
        KeyDown += OnKeyDown;

        _timer.Tick += (_, _) => _canvas.Invalidate();
        _timer.Start();

        UpdateButtons();
    }

    private NoFocusButton MakeButton(string text, Color color, Action onClick)
    {
        var button = new NoFocusButton
        {
            Text = text,
            Size = new Size(190, 52),
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => onClick();
        return button;
    }

    private void OnEngineChanged()
    {
        UpdateButtons();
        _canvas.Invalidate();
    }

    private void UpdateButtons()
    {
        bool playing = _engine.State == GameState.Inspecting;

        _allow.Visible = playing;
        _detain.Visible = playing;
        _alarm.Visible = playing;
        _inspect.Visible = playing;
        _advance.Visible = !playing;

        bool inspected = _engine.InspectionLines != null;
        _inspect.Enabled = playing && !inspected;
        _inspect.Text = inspected
            ? "PROHLÉDNUTO"
            : $"PROHLÍDKA  [I]  (−{EconomyManager.InspectionCost})";

        _advance.Text = _engine.State switch
        {
            GameState.MainMenu => "Nová hra  [Enter]",
            GameState.ShiftIntro => "Zahájit směnu  [Enter]",
            GameState.ShowingResult => "Další návštěvník  [Enter]",
            GameState.ShiftSummary => "Pokračovat  [Enter]",
            GameState.GameOver => "Nová hra  [Enter]",
            _ => "Pokračovat  [Enter]"
        };

        LayoutButtons();
    }

    private void LayoutButtons()
    {
        const int gap = 14;
        const int width = 190;
        int groupWidth = 4 * width + 3 * gap;
        int x = Math.Max(10, (_bar.Width - groupWidth) / 2);
        int y = Math.Max(0, (_bar.Height - 52) / 2);

        foreach (var button in new[] { _allow, _detain, _alarm, _inspect })
        {
            button.Location = new Point(x, y);
            x += width + gap;
        }

        _advance.Size = new Size(340, 52);
        _advance.Location = new Point(Math.Max(0, (_bar.Width - _advance.Width) / 2), y);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        bool handled = true;

        switch (e.KeyCode)
        {
            case Keys.D1:
            case Keys.NumPad1:
                _engine.Decide(Decision.Allow);
                break;
            case Keys.D2:
            case Keys.NumPad2:
                _engine.Decide(Decision.Detain);
                break;
            case Keys.D3:
            case Keys.NumPad3:
                _engine.Decide(Decision.Alarm);
                break;
            case Keys.I:
                _engine.Inspect();
                break;
            case Keys.Enter:
            case Keys.Space:
                _engine.Advance();
                break;
            default:
                handled = false;
                break;
        }

        e.Handled = handled;
        e.SuppressKeyPress = handled;
    }

    private void OnCanvasPaint(object? sender, PaintEventArgs e)
    {
        _renderer.Render(e.Graphics, _canvas.ClientSize, _engine, (float)_clock.Elapsed.TotalSeconds);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _renderer.Dispose();
        }

        base.Dispose(disposing);
    }
}
