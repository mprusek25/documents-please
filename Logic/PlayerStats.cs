namespace DocumentsPlease.Logic;

public sealed class PlayerStats
{
    public const int MaxWarnings = 3;

    public int Credits { get; set; }
    public int Warnings { get; set; }
    public int Day { get; set; } = 1;

    public int TotalCorrect { get; set; }
    public int TotalWrong { get; set; }
    public int ZombiesStopped { get; set; }
    public int ZombiesLetIn { get; set; }

    /// <summary>Příliš mnoho vpuštěných zombie = konec hry.</summary>
    public bool IsInfected => Warnings >= MaxWarnings;
}
