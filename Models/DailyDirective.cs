namespace DocumentsPlease.Models;

/// <summary>Manuál občana – pravidla platná pro jednu směnu.</summary>
public sealed class DailyDirective
{
    public int Day { get; init; }
    public DateOnly Date { get; init; }
    public Sector AllowedSector { get; init; }
    public StampColor AllowedStamp { get; init; }
    public int MinValidityYear { get; init; }
    public IReadOnlyList<ZombieSign> WarningSigns { get; init; } = Array.Empty<ZombieSign>();
    public string Bulletin { get; init; } = "";

    /// <summary>Pravidla, která se mění každý den.</summary>
    public IReadOnlyList<string> DynamicRules() => new[]
    {
        $"Vstup povolen pouze do sektoru {AllowedSector.ToString().ToUpperInvariant()}.",
        $"Povolení musí nést {AllowedStamp.Czech()} razítko.",
        $"Oba doklady musí platit minimálně do roku {MinValidityYear}."
    };

    /// <summary>Pravidla platná vždy.</summary>
    public IReadOnlyList<string> StaticRules() => new[]
    {
        "Jméno a datum narození musí být na OP i povolení shodné (pozor na překlepy).",
        "Fotografie v OP musí odpovídat osobě u okénka: vlasy, brýle, vousy."
    };
}
