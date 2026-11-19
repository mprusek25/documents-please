using DocumentsPlease.Models;

namespace DocumentsPlease.Logic;

/// <summary>Generuje Manuál občana (denní direktivu) pro každou směnu.</summary>
public static class DirectiveGenerator
{
    public const int BaseYear = 2041;

    private static readonly string[] Bulletins =
    {
        "Hlášeno: v okolních zónách přibývá případů infekce. Buďte ostražití.",
        "Velitelství: šetřete kredity, prohlídky nejsou zadarmo.",
        "Upozornění: včerejší směna zadržela dva podezřelé. Nepolevujte.",
        "Za zdí se ozývá divný hluk. Nic neřešte, kontrolujte doklady.",
        "Karanténní správa připomíná: zombie nemají přednost, ani když mají razítko.",
        "Bezpečnostní zpravodaj: nová mutace způsobuje abnormálně klidné chování.",
        "Pozor: hlášen výskyt padělaných povolení ze sektoru Delta.",
        "Dnes je zvýšená pohotovost. Zadržujte bez milosti.",
        "Zpráva z fronty: infikovaní se naučili skrývat příznaky. Prohlídka je nutná.",
        "Správa zóny oznamuje: kdo vpustí zombie, nese osobní odpovědnost."
    };

    public static DailyDirective Generate(int day, Random rng)
    {
        var date = new DateOnly(BaseYear, 11, 1).AddDays(day - 1);
        var sector = (Sector)rng.Next(Enum.GetValues<Sector>().Length);
        var stamp = (StampColor)rng.Next(Enum.GetValues<StampColor>().Length);
        // Minimum platnosti mírně roste s dny, ale ne příliš agresivně
        int minYear = BaseYear + (day - 1) / 4;

        var visible = Enum.GetValues<ZombieSign>().Where(s => s.IsVisible()).OrderBy(_ => rng.Next()).ToList();
        var hidden = Enum.GetValues<ZombieSign>().Where(s => !s.IsVisible()).OrderBy(_ => rng.Next()).ToList();

        // První dva dny jen viditelné příznaky, pak přibyde i skrytý (odhalitelný jen prohlídkou).
        var warnings = new List<ZombieSign> { visible[0], visible[1] };
        if (day >= 3) warnings.Add(hidden[0]);

        return new DailyDirective
        {
            Day = day,
            Date = date,
            AllowedSector = sector,
            AllowedStamp = stamp,
            MinValidityYear = minYear,
            WarningSigns = warnings,
            Bulletin = Bulletins[rng.Next(Bulletins.Length)]
        };
    }
}
