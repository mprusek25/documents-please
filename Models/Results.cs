namespace DocumentsPlease.Models;

/// <summary>Jedna nalezená nesrovnalost v dokladech.</summary>
public sealed record Discrepancy(DocumentError Kind, string Description);

/// <summary>Výsledek kontroly dokladů proti manuálu.</summary>
public sealed record CheckReport(IReadOnlyList<Discrepancy> Discrepancies)
{
    public bool IsClean => Discrepancies.Count == 0;
}

/// <summary>Vyhodnocení jednoho rozhodnutí hráče.</summary>
public sealed record DecisionResult(
    Decision Decision,
    VisitorType VisitorType,
    bool WasCorrect,
    int CreditsDelta,
    bool WarningIssued,
    string Headline,
    string Explanation,
    IReadOnlyList<string> Findings);

/// <summary>Souhrn odpracované směny.</summary>
public sealed record ShiftReport(
    int Day,
    int Processed,
    int Correct,
    int Wrong,
    int DecisionCredits,
    int InspectionCredits,
    int UpkeepCredits,
    int EndingCredits,
    bool GameOver);
