using System.ComponentModel.DataAnnotations;

namespace PvpAnalytics.Core.Entities;

public class Rival
{
    public long Id { get; set; }
    public Guid OwnerUserId { get; set; } // User from AuthService
    public long OpponentPlayerId { get; set; }
    public Player OpponentPlayer { get; set; } = null!;
    public string? Notes { get; set; }
    
    private int _intensityScore = 5;
    
    /// <summary>
    /// Intensity score on a 1-10 scale. Automatically clamped to valid range.
    /// </summary>
    [Range(1, 10, ErrorMessage = "IntensityScore must be between 1 and 10.")]
    public int IntensityScore
    {
        get => _intensityScore;
        set => _intensityScore = Math.Clamp(value, 1, 10);
    }
    
    public DateTime CreatedAt { get; set; }
}

