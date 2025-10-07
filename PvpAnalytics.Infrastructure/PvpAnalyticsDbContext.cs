using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.Entities;


namespace PvpAnalytics.Infrastructure;

public class PvpAnalyticsDbContext(DbContextOptions<PvpAnalyticsDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchResult> MatchResults { get; set; }
    public DbSet<CombatLogEntry> CombatLogEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MatchResult>()
            .HasIndex(mr => new { mr.MatchId, mr.PlayerId })
            .IsUnique();

        modelBuilder.Entity<CombatLogEntry>()
            .HasOne(c => c.SourcePlayer)
            .WithMany(p => p.SourceCombatLogs)
            .HasForeignKey(c => c.SourcePlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CombatLogEntry>()
            .HasOne(c => c.TargetPlayer)
            .WithMany(p => p.TargetCombatLogs)
            .HasForeignKey(c => c.TargetPlayerId)
            .OnDelete(DeleteBehavior.Restrict);
        
        
        modelBuilder.Entity<Match>()
            .HasIndex(m => m.UniqueHash)
            .IsUnique();
    }
}