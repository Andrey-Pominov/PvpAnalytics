using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Repositories;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IOpponentScoutingService
{
    Task<OpponentScoutDto?> GetScoutingDataAsync(long playerId, CancellationToken ct = default);
    Task<List<OpponentScoutDto>> SearchPlayersAsync(string name, string? realm = null, CancellationToken ct = default);
    Task<List<CompositionWinRate>> GetPlayerCompositionsAsync(long playerId, CancellationToken ct = default);
    Task<List<ClassMatchup>> GetPlayerMatchupsAsync(long playerId, CancellationToken ct = default);
}

public class OpponentScoutingService(
    IRepository<Player> playerRepo,
    IRepository<MatchResult> matchResultRepo,
    IRepository<Match> matchRepo,
    IRepository<CombatLogEntry> combatLogRepo,
    PvpAnalyticsDbContext dbContext) : IOpponentScoutingService
{
    public async Task<OpponentScoutDto?> GetScoutingDataAsync(long playerId, CancellationToken ct = default)
    {
        var player = await playerRepo.GetByIdAsync(playerId, ct);
        if (player == null) return null;

        var scout = new OpponentScoutDto
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            Realm = player.Realm,
            Class = player.Class,
        };

        // Get match results with matches
        var matchResults = await dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => mr.PlayerId == playerId)
            .ToListAsync(ct);

        if (!matchResults.Any())
            return scout;

        // Calculate basic stats
        var totalMatches = matchResults.Count;
        var wins = matchResults.Count(mr => mr.IsWinner);
        scout.TotalMatches = totalMatches;
        scout.WinRate = totalMatches > 0 ? Math.Round(wins * 100.0 / totalMatches, 2) : 0;

        // Get current and peak rating
        var latestResult = matchResults.OrderByDescending(mr => mr.Match.CreatedOn).First();
        scout.CurrentRating = latestResult.RatingAfter;
        scout.PeakRating = matchResults.Max(mr => Math.Max(mr.RatingBefore, mr.RatingAfter));
        scout.CurrentSpec = latestResult.Spec;

        // Get common compositions (team compositions this player plays)
        scout.CommonCompositions = await GetPlayerCompositionsAsync(playerId, ct);

        // Get preferred maps
        var mapStats = matchResults
            .GroupBy(mr => mr.Match.MapName)
            .Select(g => new MapPreference
            {
                MapName = g.Key,
                Matches = g.Count(),
                Wins = g.Count(m => m.IsWinner),
                WinRate = g.Count() > 0 ? Math.Round(g.Count(m => m.IsWinner) * 100.0 / g.Count(), 2) : 0
            })
            .OrderByDescending(m => m.Matches)
            .Take(10)
            .ToList();

        scout.PreferredMaps = mapStats;

        // Calculate playstyle pattern
        var combatLogs = await dbContext.CombatLogEntries
            .Where(c => c.SourcePlayerId == playerId)
            .ToListAsync(ct);

        var matchIds = matchResults.Select(mr => mr.MatchId).Distinct().ToList();
        var matches = await dbContext.Matches
            .Where(m => matchIds.Contains(m.Id))
            .ToListAsync(ct);

        var avgDamage = combatLogs.Any() ? combatLogs.Average(c => (double)c.DamageDone) : 0;
        var avgHealing = combatLogs.Any() ? combatLogs.Average(c => (double)c.HealingDone) : 0;
        var avgCC = combatLogs.Any() ? combatLogs.Count(c => !string.IsNullOrWhiteSpace(c.CrowdControl)) / (double)matchIds.Count : 0;
        var avgDuration = matches.Any() ? matches.Average(m => (double)m.Duration) : 0;

        scout.Playstyle = new PlaystylePattern
        {
            AverageDamagePerMatch = Math.Round(avgDamage, 2),
            AverageHealingPerMatch = Math.Round(avgHealing, 2),
            AverageCCPerMatch = Math.Round(avgCC, 2),
            AverageMatchDuration = Math.Round(avgDuration, 2),
            Style = DeterminePlaystyle(avgDamage, avgHealing, avgCC)
        };

        // Get class matchups
        scout.ClassMatchups = await GetPlayerMatchupsAsync(playerId, ct);

        return scout;
    }

    public async Task<List<OpponentScoutDto>> SearchPlayersAsync(string name, string? realm = null, CancellationToken ct = default)
    {
        var query = dbContext.Players.AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(realm))
        {
            query = query.Where(p => p.Realm.Contains(realm, StringComparison.OrdinalIgnoreCase));
        }

        var players = await query.Take(20).ToListAsync(ct);
        var results = new List<OpponentScoutDto>();

        foreach (var player in players)
        {
            var scout = await GetScoutingDataAsync(player.Id, ct);
            if (scout != null)
                results.Add(scout);
        }

        return results;
    }

    public async Task<List<CompositionWinRate>> GetPlayerCompositionsAsync(long playerId, CancellationToken ct = default)
    {
        // Get all matches where player participated
        var playerMatches = await dbContext.MatchResults
            .Where(mr => mr.PlayerId == playerId)
            .Select(mr => mr.MatchId)
            .Distinct()
            .ToListAsync(ct);

        if (!playerMatches.Any())
            return new List<CompositionWinRate>();

        // Get all match results for these matches, grouped by match and team
        var teamCompositions = await dbContext.MatchResults
            .Include(mr => mr.Player)
            .Include(mr => mr.Match)
            .Where(mr => playerMatches.Contains(mr.MatchId))
            .GroupBy(mr => new { mr.MatchId, mr.Team })
            .Select(g => new
            {
                MatchId = g.Key.MatchId,
                Team = g.Key.Team,
                Players = g.Select(mr => new { mr.Player.Class, mr.Spec }).ToList(),
                IsWinner = g.Any(mr => mr.IsWinner),
                Rating = g.Average(mr => (double)mr.RatingBefore)
            })
            .ToListAsync(ct);

        // Group by composition (sorted class names)
        var compositionGroups = teamCompositions
            .Where(tc => tc.Players.Any(p => p.Class != null))
            .GroupBy(tc =>
            {
                var classes = tc.Players
                    .Where(p => !string.IsNullOrWhiteSpace(p.Class))
                    .Select(p => p.Class!)
                    .OrderBy(c => c)
                    .ToList();
                return string.Join("-", classes);
            })
            .Select(g => new CompositionWinRate
            {
                Composition = g.Key,
                Matches = g.Count(),
                Wins = g.Count(tc => tc.IsWinner),
                WinRate = g.Count() > 0 ? Math.Round(g.Count(tc => tc.IsWinner) * 100.0 / g.Count(), 2) : 0,
                AverageRating = Math.Round(g.Average(tc => tc.Rating), 0)
            })
            .OrderByDescending(c => c.Matches)
            .Take(10)
            .ToList();

        return compositionGroups;
    }

    public async Task<List<ClassMatchup>> GetPlayerMatchupsAsync(long playerId, CancellationToken ct = default)
    {
        // Get all matches where player participated
        var playerMatchIds = await dbContext.MatchResults
            .Where(mr => mr.PlayerId == playerId)
            .Select(mr => mr.MatchId)
            .ToListAsync(ct);

        if (!playerMatchIds.Any())
            return new List<ClassMatchup>();

        // Get opponent classes/specs from same matches but different teams
        var playerTeam = await dbContext.MatchResults
            .Where(mr => mr.PlayerId == playerId)
            .Select(mr => new { mr.MatchId, mr.Team })
            .ToListAsync(ct);

        var opponentResults = await dbContext.MatchResults
            .Include(mr => mr.Player)
            .Include(mr => mr.Match)
            .Where(mr => playerMatchIds.Contains(mr.MatchId) && 
                        !playerTeam.Any(pt => pt.MatchId == mr.MatchId && pt.Team == mr.Team))
            .ToListAsync(ct);

        var matchups = opponentResults
            .GroupBy(mr => new { mr.Player.Class, mr.Spec })
            .Select(g => new ClassMatchup
            {
                OpponentClass = g.Key.Class ?? "Unknown",
                OpponentSpec = g.Key.Spec,
                Matches = g.Count(),
                Wins = g.Count(mr => !mr.IsWinner), // Opponent lost = player won
                WinRate = g.Count() > 0 ? Math.Round(g.Count(mr => !mr.IsWinner) * 100.0 / g.Count(), 2) : 0
            })
            .OrderByDescending(m => m.Matches)
            .Take(20)
            .ToList();

        return matchups;
    }

    private static string DeterminePlaystyle(double avgDamage, double avgHealing, double avgCC)
    {
        if (avgDamage > avgHealing * 2)
            return "Aggressive";
        if (avgHealing > avgDamage * 2)
            return "Defensive";
        return "Balanced";
    }
}

