namespace DocumentsPlease.Models;

/// <summary>Občanský průkaz.</summary>
public sealed class PassportDocument
{
    public string FullName { get; init; } = "";
    public DateOnly BirthDate { get; init; }
    public string IdNumber { get; init; } = "";
    public int ValidUntilYear { get; init; }
    public FaceTraits Photo { get; init; } = null!;
}

/// <summary>Povolení ke vstupu (s razítkem).</summary>
public sealed class EntryPermit
{
    public string FullName { get; init; } = "";
    public DateOnly BirthDate { get; init; }
    public Sector TargetSector { get; init; }
    public StampColor Stamp { get; init; }
    public string Purpose { get; init; } = "";
    public string PermitNumber { get; init; } = "";
    public int ValidUntilYear { get; init; }
}
