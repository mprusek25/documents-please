namespace DocumentsPlease.Models;

public sealed class Visitor
{
    public int Id { get; init; }
    public VisitorType Type { get; init; }
    public string FullName { get; init; } = "";
    public FaceTraits Face { get; init; } = null!;
    public int JacketColor { get; init; }
    public PassportDocument Passport { get; init; } = null!;
    public EntryPermit Permit { get; init; } = null!;
    public HashSet<ZombieSign> ZombieSigns { get; init; } = new();
    public bool Nervous { get; init; }
    public string Greeting { get; init; } = "";

    public bool HasSign(ZombieSign sign) => ZombieSigns.Contains(sign);

    /// <summary>Výsledek detailní prohlídky – odhalí i příznaky, které nejsou vidět očima.</summary>
    public IReadOnlyList<string> Inspect()
    {
        return new List<string>
        {
            HasSign(ZombieSign.ColdSkin) ? "Dotyk: kůže ledově chladná" : "Dotyk: kůže teplá, 36,6 °C",
            HasSign(ZombieSign.NoPulse) ? "Tep: nenalezen" : $"Tep: {60 + (Id * 7) % 30} tepů/min",
            HasSign(ZombieSign.Stench) ? "Zápach: sladký odér rozkladu" : "Zápach: bez nápadností",
            HasSign(ZombieSign.Twitching) ? "Chování: nekontrolovatelné záškuby"
                : Nervous ? "Chování: nervózní, těká očima"
                : "Chování: klidné"
        };
    }
}
