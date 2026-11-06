using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;
using DocumentsPlease.Logic;

namespace DocumentsPlease.UI;

/// <summary>Plátno, které jen zavolá renderer. Žádná herní logika.</summary>
public sealed class GameView : Control
{
    private readonly GameEngine _engine;
    private readonly GameRenderer _renderer = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public GameView(GameEngine engine) => _engine = engine;

    public override void Render(DrawingContext context)
    {
        _renderer.Render(context, Bounds.Size, _engine, _clock.Elapsed.TotalSeconds);
    }
}
