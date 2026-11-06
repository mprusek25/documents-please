using DocumentsPlease.Models;

namespace DocumentsPlease.Logic;

/// <summary>Dynamicky generuje návštěvníky: čisté lidi, administrativní podvodníky a skryté zombie.</summary>
public sealed class VisitorGenerator
{
    private static readonly string[] MaleFirst =
        { "Jan", "Petr", "Martin", "Tomáš", "Jakub", "Lukáš", "Pavel", "Ondřej", "Michal", "David", "Marek", "Karel" };
    private static readonly string[] FemaleFirst =
        { "Jana", "Eva", "Lucie", "Tereza", "Hana", "Petra", "Kateřina", "Lenka", "Markéta", "Veronika", "Zuzana", "Alena" };
    private static readonly string[] MaleLast =
        { "Novák", "Svoboda", "Dvořák", "Černý", "Procházka", "Kučera", "Veselý", "Horák", "Němec", "Pospíšil", "Hájek", "Král", "Jelínek", "Růžička", "Beneš" };
    private static readonly string[] FemaleLast =
        { "Nováková", "Svobodová", "Dvořáková", "Černá", "Procházková", "Kučerová", "Veselá", "Horáková", "Němcová", "Pospíšilová", "Hájková", "Králová", "Jelínková", "Růžičková", "Benešová" };
    private static readonly string[] Purposes =
        { "Práce", "Návštěva rodiny", "Obchodní jednání", "Léčení", "Studium", "Stěhování" };

    private readonly Random _rng;
    private int _nextId = 1;

    public VisitorGenerator(Random rng) => _rng = rng;

    public Visitor Generate(DailyDirective directive)
    {
        return Build(RollType(directive.Day), directive);
    }

    private VisitorType RollType(int day)
    {
        double zombieChance = Math.Min(0.18 + 0.02 * day, 0.35);
        const double fraudChance = 0.27;
        double roll = _rng.NextDouble();

        if (roll < zombieChance) return VisitorType.HiddenZombie;
        if (roll < zombieChance + fraudChance) return VisitorType.AdministrativeFraudster;
        return VisitorType.CleanHuman;
    }

    private Visitor Build(VisitorType type, DailyDirective d)
    {
        bool female = _rng.Next(2) == 0;
        string name = $"{Pick(female ? FemaleFirst : MaleFirst)} {Pick(female ? FemaleLast : MaleLast)}";
        var face = RandomFace(female);
        var birth = d.Date.AddYears(-_rng.Next(18, 71)).AddDays(-_rng.Next(0, 365));
        string purpose = Pick(Purposes);

        // Nejdřív vždy dokonale platné doklady, pak se podle typu případně pokazí.
        var draft = new Draft
        {
            PassName = name,
            PermitName = name,
            PassBirth = birth,
            PermitBirth = birth,
            PassYear = ValidYear(d),
            PermitYear = ValidYear(d),
            TargetSector = d.AllowedSector,
            Stamp = d.AllowedStamp,
            Photo = face
        };

        var signs = new HashSet<ZombieSign>();
        bool nervous;

        switch (type)
        {
            case VisitorType.AdministrativeFraudster:
                int errorCount = _rng.Next(100) < 25 ? 2 : 1;
                foreach (var err in Shuffle(Enum.GetValues<DocumentError>()).Take(errorCount))
                    ApplyError(err, draft, d, name);
                nervous = _rng.Next(100) < 70;
                break;

            case VisitorType.HiddenZombie:
                signs = RollZombieSigns(d);
                if (_rng.Next(100) < 15)
                    ApplyError(Shuffle(Enum.GetValues<DocumentError>()).First(), draft, d, name);
                nervous = _rng.Next(100) < 10;
                break;

            default:
                nervous = _rng.Next(100) < 10;
                break;
        }

        return new Visitor
        {
            Id = _nextId++,
            Type = type,
            FullName = name,
            Face = face,
            JacketColor = _rng.Next(6),
            Passport = new PassportDocument
            {
                FullName = draft.PassName,
                BirthDate = draft.PassBirth,
                IdNumber = $"OP{_rng.Next(100000, 999999)}",
                ValidUntilYear = draft.PassYear,
                Photo = draft.Photo
            },
            Permit = new EntryPermit
            {
                FullName = draft.PermitName,
                BirthDate = draft.PermitBirth,
                TargetSector = draft.TargetSector,
                Stamp = draft.Stamp,
                Purpose = purpose,
                PermitNumber = $"PV-{_rng.Next(1000, 9999)}-{(char)('A' + _rng.Next(26))}",
                ValidUntilYear = draft.PermitYear
            },
            ZombieSigns = signs,
            Nervous = nervous,
            Greeting = BuildGreeting(purpose, signs, nervous)
        };
    }

    private HashSet<ZombieSign> RollZombieSigns(DailyDirective d)
    {
        // Zombie vždy projeví aspoň jeden z dnes hlášených příznaků (hra je tak férová),
        // k tomu může mít až dva další.
        var signs = new HashSet<ZombieSign> { d.WarningSigns[_rng.Next(d.WarningSigns.Count)] };
        var all = Enum.GetValues<ZombieSign>();
        int extra = _rng.Next(0, 3);
        for (int i = 0; i < extra; i++)
            signs.Add(all[_rng.Next(all.Length)]);
        return signs;
    }

    private void ApplyError(DocumentError error, Draft draft, DailyDirective d, string trueName)
    {
        switch (error)
        {
            case DocumentError.ExpiredDocument:
                int expired = d.MinValidityYear - _rng.Next(1, 4);
                if (_rng.Next(2) == 0) draft.PassYear = expired; else draft.PermitYear = expired;
                break;

            case DocumentError.NameMismatch:
                string typo = MakeTypo(trueName);
                if (_rng.Next(2) == 0) draft.PassName = typo; else draft.PermitName = typo;
                break;

            case DocumentError.BirthDateMismatch:
                int shift = _rng.Next(1, 28) * (_rng.Next(2) == 0 ? 1 : -1);
                draft.PermitBirth = draft.PermitBirth.AddDays(shift);
                break;

            case DocumentError.WrongSector:
                var sectors = Enum.GetValues<Sector>().Where(s => s != d.AllowedSector).ToArray();
                draft.TargetSector = sectors[_rng.Next(sectors.Length)];
                break;

            case DocumentError.WrongStamp:
                var stamps = Enum.GetValues<StampColor>().Where(s => s != d.AllowedStamp).ToArray();
                draft.Stamp = stamps[_rng.Next(stamps.Length)];
                break;

            case DocumentError.PhotoMismatch:
                draft.Photo = draft.Photo with
                {
                    HairColor = (draft.Photo.HairColor + 1 + _rng.Next(FaceTraits.HairColorCount - 1)) % FaceTraits.HairColorCount,
                    Glasses = _rng.Next(2) == 0 ? !draft.Photo.Glasses : draft.Photo.Glasses
                };
                break;
        }
    }

    /// <summary>Vyrobí věrohodný překlep: prohození, záměna nebo vynechání písmene.</summary>
    private string MakeTypo(string s)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            int i = _rng.Next(s.Length);
            if (!char.IsLetter(s[i])) continue;

            string result;
            switch (_rng.Next(3))
            {
                case 0:
                    if (i + 1 >= s.Length || !char.IsLetter(s[i + 1])) continue;
                    result = s.Substring(0, i) + s[i + 1] + s[i] + s.Substring(i + 2);
                    break;
                case 1:
                    char repl = "aeiouyrnstklmd"[_rng.Next(14)];
                    result = s.Substring(0, i) + (char.IsUpper(s[i]) ? char.ToUpper(repl) : repl) + s.Substring(i + 1);
                    break;
                default:
                    result = s.Remove(i, 1);
                    break;
            }

            if (result != s) return result;
        }

        return s + "x";
    }

    private FaceTraits RandomFace(bool female)
    {
        var styles = female
            ? new[] { HairStyle.Long, HairStyle.Long, HairStyle.Curly, HairStyle.Short }
            : new[] { HairStyle.Short, HairStyle.Short, HairStyle.Curly, HairStyle.Bald };

        return new FaceTraits(
            SkinTone: _rng.Next(FaceTraits.SkinToneCount),
            HairColor: _rng.Next(FaceTraits.HairColorCount),
            Hair: styles[_rng.Next(styles.Length)],
            Glasses: _rng.Next(100) < 25,
            Beard: !female && _rng.Next(100) < 35);
    }

    private static string BuildGreeting(string purpose, HashSet<ZombieSign> signs, bool nervous)
    {
        string p = purpose.ToLowerInvariant();

        if (signs.Contains(ZombieSign.Groaning))
            return $"D-dooobrý... deeen... Jdu... za účelem... {p}... mmmrrr...";
        if (nervous)
            return $"Ehm... dobrý den. {purpose}, jen na chvilku. Doklady mám v pořádku, slibuju!";
        return $"Dobrý den. Přicházím kvůli účelu: {p}.";
    }

    private int ValidYear(DailyDirective d) => d.MinValidityYear + _rng.Next(0, 5);

    private string Pick(string[] items) => items[_rng.Next(items.Length)];

    private List<T> Shuffle<T>(IEnumerable<T> source) => source.OrderBy(_ => _rng.Next()).ToList();

    /// <summary>Pracovní kopie dokladů, než se z nich stanou neměnné objekty.</summary>
    private sealed class Draft
    {
        public string PassName = "";
        public string PermitName = "";
        public DateOnly PassBirth;
        public DateOnly PermitBirth;
        public int PassYear;
        public int PermitYear;
        public Sector TargetSector;
        public StampColor Stamp;
        public FaceTraits Photo = null!;
    }
}
