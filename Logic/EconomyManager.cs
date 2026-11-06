using DocumentsPlease.Models;

namespace DocumentsPlease.Logic;

/// <summary>Herní ekonomika: odměny, pokuty, poplatky a vyhodnocení rozhodnutí.</summary>
public sealed class EconomyManager
{
    public const int StartingCredits = 50;
    public const int RewardCorrect = 10;
    public const int RewardAlarmBonus = 15;
    public const int PenaltyZombieLetIn = -50;
    public const int PenaltyDetainInnocent = -15;
    public const int PenaltyMissedAdminError = -20;
    public const int PenaltyFalseAlarm = -15;
    public const int InspectionCost = 2;
    public const int DailyUpkeep = 40;

    private readonly PlayerStats _stats;

    public EconomyManager(PlayerStats stats) => _stats = stats;

    /// <summary>Vyhodnotí rozhodnutí, upraví kredity a statistiky a vrátí popis výsledku.</summary>
    public DecisionResult Resolve(Visitor visitor, CheckReport report, Decision decision)
    {
        int delta = 0;
        bool correct = false;
        bool warning = false;
        string headline = "";
        string explanation = "";
        var findings = new List<string>();

        if (visitor.Type == VisitorType.HiddenZombie)
        {
            findings.AddRange(visitor.ZombieSigns.Select(s => "Příznak: " + s.Czech()));
            if (report.Discrepancies.Count > 0)
                findings.AddRange(report.Discrepancies.Select(d => "Doklady: " + d.Description));

            if (decision == Decision.Allow)
            {
                delta = PenaltyZombieLetIn;
                warning = true;
                headline = "KRITICKÉ SELHÁNÍ!";
                explanation = "Vpustil jsi infikovanou osobu. Dostáváš pokutu a oficiální varování.";
                _stats.Warnings++;
                _stats.ZombiesLetIn++;
            }
            else if (decision == Decision.Alarm)
            {
                delta = RewardAlarmBonus;
                correct = true;
                headline = "Výborně – poplach!";
                explanation = "Zombie byla zneškodněna. Za pohotovost dostáváš bonus.";
                _stats.ZombiesStopped++;
            }
            else
            {
                delta = RewardCorrect;
                correct = true;
                headline = "Zadrženo – správně.";
                explanation = "Osoba měla příznaky infekce a byla odvedena do karantény.";
                _stats.ZombiesStopped++;
            }
        }
        else if (!report.IsClean)
        {
            findings.AddRange(report.Discrepancies.Select(d => d.Description));

            if (decision == Decision.Allow)
            {
                delta = PenaltyMissedAdminError;
                headline = "Přehlédnutá chyba v dokladech.";
                explanation = "Vpustil jsi člověka s neplatnými doklady. Úřad ti strhává pokutu.";
            }
            else if (decision == Decision.Detain)
            {
                delta = RewardCorrect;
                correct = true;
                headline = "Zadrženo – správně.";
                explanation = "Doklady obsahovaly chybu. Podvodník je zadržen.";
            }
            else
            {
                delta = PenaltyFalseAlarm;
                headline = "Planý poplach.";
                explanation = "Osoba nebyla nakažená, šlo jen o chybu v dokladech. Poplach se nevyplácí.";
            }
        }
        else
        {
            findings.Add("Doklady v pořádku, žádné příznaky infekce.");

            if (decision == Decision.Allow)
            {
                delta = RewardCorrect;
                correct = true;
                headline = "Vpuštěn – správně.";
                explanation = "Čistý člověk s platnými doklady.";
            }
            else if (decision == Decision.Detain)
            {
                delta = PenaltyDetainInnocent;
                headline = "Byrokratická šikana!";
                explanation = "Zadržel jsi nevinného člověka. Dostáváš pokutu.";
            }
            else
            {
                delta = PenaltyFalseAlarm;
                headline = "Planý poplach!";
                explanation = "Vyhlásil jsi poplach kvůli nevinnému člověku. Dostáváš pokutu.";
            }
        }

        _stats.Credits += delta;
        if (correct) _stats.TotalCorrect++; else _stats.TotalWrong++;

        return new DecisionResult(decision, visitor.Type, correct, delta, warning, headline, explanation, findings);
    }

    /// <summary>Strhne poplatek za detailní prohlídku.</summary>
    public int ChargeInspection()
    {
        _stats.Credits -= InspectionCost;
        return InspectionCost;
    }

    /// <summary>Strhne denní nájem a živobytí na konci směny.</summary>
    public int ChargeUpkeep()
    {
        _stats.Credits -= DailyUpkeep;
        return DailyUpkeep;
    }
}
