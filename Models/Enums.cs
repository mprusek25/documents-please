namespace DocumentsPlease.Models;

/// <summary>Skutečná podstata návštěvníka. Zná ji jen engine – hráč ji musí odhalit.</summary>
public enum VisitorType
{
    CleanHuman,
    AdministrativeFraudster,
    HiddenZombie
}

/// <summary>Rozhodnutí hráče u okénka.</summary>
public enum Decision
{
    Allow,
    Detain,
    Alarm
}

public enum Sector { Alfa, Beta, Gama, Delta }

public enum StampColor { Red, Green, Blue }

/// <summary>Druhy administrativních chyb, které může podvodník mít v dokladech.</summary>
public enum DocumentError
{
    ExpiredDocument,
    NameMismatch,
    BirthDateMismatch,
    WrongSector,
    WrongStamp,
    PhotoMismatch
}

public enum HairStyle { Short, Long, Curly, Bald }

/// <summary>Příznaky zombie infekce. Část je vidět na první pohled, část odhalí jen prohlídka.</summary>
public enum ZombieSign
{
    Pallor,
    Scars,
    MilkyEyes,
    Drooling,
    Twitching,
    Groaning,
    ColdSkin,
    NoPulse,
    Stench
}

public static class EnumText
{
    public static string Czech(this StampColor color) => color switch
    {
        StampColor.Red => "červené",
        StampColor.Green => "zelené",
        StampColor.Blue => "modré",
        _ => color.ToString()
    };

    public static string Czech(this ZombieSign sign) => sign switch
    {
        ZombieSign.Pallor => "bledá, šedozelená pleť",
        ZombieSign.Scars => "hluboké jizvy a šité rány",
        ZombieSign.MilkyEyes => "zakalené, mléčné oči",
        ZombieSign.Drooling => "slintání",
        ZombieSign.Twitching => "záškuby a třes",
        ZombieSign.Groaning => "chraptivá, vleklá řeč",
        ZombieSign.ColdSkin => "ledově chladná kůže",
        ZombieSign.NoPulse => "chybějící tep",
        ZombieSign.Stench => "zápach rozkladu",
        _ => sign.ToString()
    };

    /// <summary>Viditelné příznaky jsou poznat na první pohled; ostatní odhalí jen prohlídka.</summary>
    public static bool IsVisible(this ZombieSign sign) =>
        sign is not (ZombieSign.ColdSkin or ZombieSign.NoPulse or ZombieSign.Stench);
}
