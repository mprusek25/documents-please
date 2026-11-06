using DocumentsPlease.Models;

namespace DocumentsPlease.Logic;

/// <summary>
/// Porovná doklady návštěvníka mezi sebou, s jeho vzhledem a s denním manuálem.
/// Jediný zdroj pravdy o administrativních chybách.
/// </summary>
public static class DocumentChecker
{
    public static CheckReport Check(Visitor visitor, DailyDirective directive)
    {
        var found = new List<Discrepancy>();
        var passport = visitor.Passport;
        var permit = visitor.Permit;

        if (passport.ValidUntilYear < directive.MinValidityYear)
            found.Add(new Discrepancy(DocumentError.ExpiredDocument,
                $"OP je propadlý (platnost do {passport.ValidUntilYear}, požadováno min. {directive.MinValidityYear})."));

        if (permit.ValidUntilYear < directive.MinValidityYear)
            found.Add(new Discrepancy(DocumentError.ExpiredDocument,
                $"Povolení je propadlé (platnost do {permit.ValidUntilYear}, požadováno min. {directive.MinValidityYear})."));

        if (passport.FullName != permit.FullName)
            found.Add(new Discrepancy(DocumentError.NameMismatch,
                $"Jméno v OP ('{passport.FullName}') se liší od jména v povolení ('{permit.FullName}')."));

        if (passport.BirthDate != permit.BirthDate)
            found.Add(new Discrepancy(DocumentError.BirthDateMismatch,
                $"Datum narození v OP ({passport.BirthDate:dd.MM.yyyy}) se liší od povolení ({permit.BirthDate:dd.MM.yyyy})."));

        if (permit.TargetSector != directive.AllowedSector)
            found.Add(new Discrepancy(DocumentError.WrongSector,
                $"Povolení platí pro sektor {permit.TargetSector.ToString().ToUpperInvariant()}, dnes je povolen {directive.AllowedSector.ToString().ToUpperInvariant()}."));

        if (permit.Stamp != directive.AllowedStamp)
            found.Add(new Discrepancy(DocumentError.WrongStamp,
                $"Razítko je {permit.Stamp.Czech()}, dnes platí {directive.AllowedStamp.Czech()}."));

        if (visitor.Face != passport.Photo)
            found.Add(new Discrepancy(DocumentError.PhotoMismatch,
                "Fotografie v OP neodpovídá osobě u okénka."));

        return new CheckReport(found);
    }
}
