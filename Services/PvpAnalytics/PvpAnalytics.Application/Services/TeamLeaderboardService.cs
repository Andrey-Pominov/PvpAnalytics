using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface ITeamLeaderboardService
{
    Task<TeamLeaderboardDto> GetLeaderboardAsync(string bracket, string? region = null, int? limit = 100, CancellationToken ct = default);
}

public class TeamLeaderboardService(PvpAnalyticsDbContext dbContext) : ITeamLeaderboardService
{
    public async Task<TeamLeaderboardDto> GetLeaderboardAsync(string bracket, string? region = null, int? limit = 100, CancellationToken ct = default)
    {
        var query = dbContext.Teams
            .Include(t => t.Members)
                .ThenInclude(m => m.Player)
            .Where(t => t.Bracket == bracket);

        if (!string.IsNullOrEmpty(region))
            query = query.Where(t => t.Region == region);

        var teams = await query.ToListAsync(ct);

        var entries = new List<TeamLeaderboardEntryDto>();

        foreach (var team in teams)
        {
            var teamMatches = await dbContext.TeamMatches
                .Include(tm => tm.Match)
                .Where(tm => tm.TeamId == team.Id)
                .ToListAsync(ct);

            var totalMatches = teamMatches.Count;
            var wins = teamMatches.Count(tm => tm.IsWin);
            var losses = totalMatches - wins;
            var winRate = totalMatches > 0 ? Math.Round(wins * 100.0 / totalMatches, 2) : 0.0;

            var lastMatchDate = teamMatches.Any()
                ? teamMatches.Max(tm => tm.Match.CreatedOn)
                : team.CreatedAt;

            entries.Add(new TeamLeaderboardEntryDto
            {
                TeamId = team.Id,
                TeamName = team.Name,
                Rating = team.Rating,
                TotalMatches = totalMatches,
                Wins = wins,
                Losses = losses,
                WinRate = winRate,
                LastMatchDate = lastMatchDate,
                MemberNames = team.Members.Select(m => m.Player.Name).ToList()
            });
        }

        // Sort by rating (descending), then by win rate, then by total matches
        entries = entries
            .OrderByDescending(e => e.Rating ?? 0)
            .ThenByDescending(e => e.WinRate)
            .ThenByDescending(e => e.TotalMatches)
            .Take(limit ?? 100)
            .ToList();

        // Assign ranks
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].Rank = i + 1;
        }

        return new TeamLeaderboardDto
        {
            Bracket = bracket,
            Region = region,
            Entries = entries,
            TotalTeams = entries.Count,
            LastUpdated = DateTime.UtcNow
        };
    }
}

