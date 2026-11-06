using DocumentsPlease.Models;

namespace DocumentsPlease.Logic;

public enum GameState
{
    MainMenu,
    ShiftIntro,
    Inspecting,
    ShowingResult,
    ShiftSummary,
    GameOver
}

/// <summary>
/// Srdce hry – stavový automat, který řídí směny, návštěvníky a rozhodnutí.
/// Nezná žádné vykreslování; UI jen čte jeho stav a volá jeho metody.
/// </summary>
public sealed class GameEngine
{
    private Random _rng = new();
    private VisitorGenerator _visitors;
    private CheckReport? _truth;

    private int _shiftCorrect;
    private int _shiftWrong;
    private int _shiftDecisionCredits;
    private int _shiftInspectionCredits;

    public GameEngine()
    {
        _visitors = new VisitorGenerator(_rng);
        Stats = new PlayerStats { Credits = EconomyManager.StartingCredits };
        Economy = new EconomyManager(Stats);
        Directive = DirectiveGenerator.Generate(1, _rng);
    }

    public event Action? Changed;

    public GameState State { get; private set; } = GameState.MainMenu;
    public PlayerStats Stats { get; private set; }
    public EconomyManager Economy { get; private set; }
    public DailyDirective Directive { get; private set; }

    public Visitor? CurrentVisitor { get; private set; }
    public IReadOnlyList<string>? InspectionLines { get; private set; }
    public DecisionResult? LastResult { get; private set; }
    public ShiftReport? LastShiftReport { get; private set; }
    public string GameOverReason { get; private set; } = "";

    public int VisitorsTotal { get; private set; }
    public int VisitorsProcessed { get; private set; }

    /// <summary>Pořadí aktuálního návštěvníka ve směně (pro HUD).</summary>
    public int VisitorNumber => State == GameState.Inspecting ? VisitorsProcessed + 1 : VisitorsProcessed;

    /// <summary>Univerzální "pokračuj" – Enter / tlačítko dole. Význam závisí na stavu hry.</summary>
    public void Advance()
    {
        switch (State)
        {
            case GameState.MainMenu:
            case GameState.GameOver:
                NewGame();
                break;
            case GameState.ShiftIntro:
                BeginShift();
                break;
            case GameState.ShowingResult:
                AfterResult();
                break;
            case GameState.ShiftSummary:
                AfterSummary();
                break;
        }
    }

    public void NewGame()
    {
        _rng = new Random();
        _visitors = new VisitorGenerator(_rng);
        Stats = new PlayerStats { Credits = EconomyManager.StartingCredits };
        Economy = new EconomyManager(Stats);
        GameOverReason = "";
        LastShiftReport = null;
        PrepareShift();
        State = GameState.ShiftIntro;
        Changed?.Invoke();
    }

    public void BeginShift()
    {
        if (State != GameState.ShiftIntro) return;
        SpawnVisitor();
        State = GameState.Inspecting;
        Changed?.Invoke();
    }

    /// <summary>Detailní prohlídka návštěvníka (stojí kredity, odhalí skryté příznaky).</summary>
    public void Inspect()
    {
        if (State != GameState.Inspecting || CurrentVisitor == null || InspectionLines != null) return;

        _shiftInspectionCredits -= Economy.ChargeInspection();
        InspectionLines = CurrentVisitor.Inspect();
        Changed?.Invoke();
    }

    /// <summary>Hráč rozhodl: pustit, zadržet nebo vyhlásit poplach.</summary>
    public void Decide(Decision decision)
    {
        if (State != GameState.Inspecting || CurrentVisitor == null || _truth == null) return;

        var result = Economy.Resolve(CurrentVisitor, _truth, decision);
        LastResult = result;
        VisitorsProcessed++;
        _shiftDecisionCredits += result.CreditsDelta;
        if (result.WasCorrect) _shiftCorrect++; else _shiftWrong++;

        State = GameState.ShowingResult;
        Changed?.Invoke();
    }

    private void AfterResult()
    {
        if (Stats.IsInfected)
        {
            GameOverReason = "Vpustil jsi příliš mnoho zombie. Infekce se rozšířila a stanoviště padlo.";
            State = GameState.GameOver;
        }
        else if (VisitorsProcessed >= VisitorsTotal)
        {
            EndShift();
        }
        else
        {
            SpawnVisitor();
            State = GameState.Inspecting;
        }

        Changed?.Invoke();
    }

    private void EndShift()
    {
        int upkeep = Economy.ChargeUpkeep();
        bool broke = Stats.Credits < 0;

        LastShiftReport = new ShiftReport(
            Stats.Day, VisitorsProcessed, _shiftCorrect, _shiftWrong,
            _shiftDecisionCredits, _shiftInspectionCredits, -upkeep, Stats.Credits, broke);

        if (broke)
            GameOverReason = "Nemáš na nájem ani jídlo. Úřad ti ruší akreditaci a vyhazuje tě ze stanoviště.";

        State = GameState.ShiftSummary;
    }

    private void AfterSummary()
    {
        if (LastShiftReport is { GameOver: true })
        {
            State = GameState.GameOver;
        }
        else
        {
            Stats.Day++;
            PrepareShift();
            State = GameState.ShiftIntro;
        }

        Changed?.Invoke();
    }

    private void PrepareShift()
    {
        Directive = DirectiveGenerator.Generate(Stats.Day, _rng);
        VisitorsTotal = Math.Min(6 + Stats.Day, 14);
        VisitorsProcessed = 0;
        _shiftCorrect = 0;
        _shiftWrong = 0;
        _shiftDecisionCredits = 0;
        _shiftInspectionCredits = 0;
        CurrentVisitor = null;
        InspectionLines = null;
        LastResult = null;
        _truth = null;
    }

    private void SpawnVisitor()
    {
        CurrentVisitor = _visitors.Generate(Directive);
        _truth = DocumentChecker.Check(CurrentVisitor, Directive);
        InspectionLines = null;
        LastResult = null;
    }
}
