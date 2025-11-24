using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Logs;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IKeyMomentService
{
    Task<KeyMomentDto> GetKeyMomentsForMatchAsync(long matchId, CancellationToken ct = default);
    Task<PlayerKeyMomentsDto> GetRecentKeyMomentsAsync(long playerId, int limit = 10, CancellationToken ct = default);
}

public class KeyMomentService(PvpAnalyticsDbContext dbContext) : IKeyMomentService
{
    public async Task<KeyMomentDto> GetKeyMomentsForMatchAsync(long matchId, CancellationToken ct = default)
    {
        var match = await dbContext.Matches
            .Include(m => m.CombatLogs)
            .ThenInclude(c => c.SourcePlayer)
            .Include(m => m.CombatLogs)
            .ThenInclude(c => c.TargetPlayer)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match == null)
            return new KeyMomentDto { MatchId = matchId };

        var dto = new KeyMomentDto
        {
            MatchId = matchId,
            MatchDate = match.CreatedOn
        };

        var moments = new List<KeyMoment>();
        var matchStart = match.CreatedOn;

        // Get combat logs ordered by timestamp
        var combatLogs = match.CombatLogs.OrderBy(c => c.Timestamp).ToList();

        // Detect deaths (large damage spike followed by no activity from that player)
        var playerLastActivity = new Dictionary<long, DateTime>();
        foreach (var log in combatLogs)
        {
            if (log.SourcePlayerId > 0)
                playerLastActivity[log.SourcePlayerId] = log.Timestamp;

            if (log.TargetPlayerId.HasValue && log.DamageDone > 50000) // Large damage spike
            {
                var targetId = log.TargetPlayerId.Value;
                var timeAfter = combatLogs
                    .Where(c => c.Timestamp > log.Timestamp && c.Timestamp <= log.Timestamp.AddSeconds(5))
                    .Any(c => c.SourcePlayerId == targetId);

                if (!timeAfter && playerLastActivity.ContainsKey(targetId))
                {
                    var relativeTime = (long)(log.Timestamp - matchStart).TotalSeconds;
                    moments.Add(new KeyMoment
                    {
                        Timestamp = relativeTime,
                        EventType = "death",
                        Description = $"{log.TargetPlayer?.Name ?? "Unknown"} died",
                        SourcePlayerId = log.SourcePlayerId,
                        SourcePlayerName = log.SourcePlayer?.Name,
                        TargetPlayerId = targetId,
                        TargetPlayerName = log.TargetPlayer?.Name,
                        Ability = log.Ability,
                        DamageDone = log.DamageDone,
                        ImpactScore = 0.9,
                        IsCritical = true
                    });
                }
            }
        }

        // Detect major cooldowns
        foreach (var log in combatLogs)
        {
            if (ImportantAbilities.IsCooldownOrDefensive(log.Ability))
            {
                var relativeTime = (long)(log.Timestamp - matchStart).TotalSeconds;
                moments.Add(new KeyMoment
                {
                    Timestamp = relativeTime,
                    EventType = "cooldown",
                    Description = $"{log.SourcePlayer?.Name ?? "Unknown"} used {log.Ability}",
                    SourcePlayerId = log.SourcePlayerId,
                    SourcePlayerName = log.SourcePlayer?.Name,
                    TargetPlayerId = log.TargetPlayerId,
                    TargetPlayerName = log.TargetPlayer?.Name,
                    Ability = log.Ability,
                    ImpactScore = 0.7,
                    IsCritical = false
                });
            }
        }

        // Detect CC chains (multiple CCs in quick succession)
        var ccLogs = combatLogs
            .Where(c => !string.IsNullOrWhiteSpace(c.CrowdControl))
            .OrderBy(c => c.Timestamp)
            .ToList();

        for (int i = 0; i < ccLogs.Count - 1; i++)
        {
            var current = ccLogs[i];
            var next = ccLogs[i + 1];
            var timeDiff = (next.Timestamp - current.Timestamp).TotalSeconds;

            if (timeDiff <= 3 && current.TargetPlayerId == next.TargetPlayerId) // CC chain
            {
                var relativeTime = (long)(current.Timestamp - matchStart).TotalSeconds;
                moments.Add(new KeyMoment
                {
                    Timestamp = relativeTime,
                    EventType = "cc_chain",
                    Description = $"CC chain on {current.TargetPlayer?.Name ?? "Unknown"}",
                    SourcePlayerId = current.SourcePlayerId,
                    SourcePlayerName = current.SourcePlayer?.Name,
                    TargetPlayerId = current.TargetPlayerId,
                    TargetPlayerName = current.TargetPlayer?.Name,
                    CrowdControl = current.CrowdControl,
                    ImpactScore = 0.8,
                    IsCritical = true
                });
            }
        }

        // Detect damage spikes
        var damageSpikes = combatLogs
            .Where(c => c.DamageDone > 100000) // Very large damage
            .ToList();

        foreach (var spike in damageSpikes)
        {
            var relativeTime = (long)(spike.Timestamp - matchStart).TotalSeconds;
            moments.Add(new KeyMoment
            {
                Timestamp = relativeTime,
                EventType = "damage_spike",
                Description = $"{spike.SourcePlayer?.Name ?? "Unknown"} dealt {spike.DamageDone:N0} damage",
                SourcePlayerId = spike.SourcePlayerId,
                SourcePlayerName = spike.SourcePlayer?.Name,
                TargetPlayerId = spike.TargetPlayerId,
                TargetPlayerName = spike.TargetPlayer?.Name,
                Ability = spike.Ability,
                DamageDone = spike.DamageDone,
                ImpactScore = Math.Min(spike.DamageDone / 200000.0, 1.0),
                IsCritical = spike.DamageDone > 150000
            });
        }

        // Get rating changes from match results
        var matchResults = await dbContext.MatchResults
            .Where(mr => mr.MatchId == matchId)
            .ToListAsync(ct);

        foreach (var result in matchResults)
        {
            var ratingChange = result.RatingAfter - result.RatingBefore;
            if (Math.Abs(ratingChange) >= 10) // Significant rating change
            {
                var relativeTime = (long)(match.CreatedOn - matchStart).TotalSeconds;
                moments.Add(new KeyMoment
                {
                    Timestamp = relativeTime,
                    EventType = "rating_change",
                    Description = $"{result.Player?.Name ?? "Unknown"} {(ratingChange > 0 ? "gained" : "lost")} {Math.Abs(ratingChange)} rating",
                    TargetPlayerId = result.PlayerId,
                    ImpactScore = Math.Min(Math.Abs(ratingChange) / 50.0, 1.0),
                    IsCritical = Math.Abs(ratingChange) >= 20
                });
            }
        }

        dto.Moments = moments
            .OrderBy(m => m.Timestamp)
            .ToList();

        return dto;
    }

    public async Task<PlayerKeyMomentsDto> GetRecentKeyMomentsAsync(long playerId, int limit = 10, CancellationToken ct = default)
    {
        var player = await dbContext.Players.FindAsync([playerId], ct);
        var dto = new PlayerKeyMomentsDto
        {
            PlayerId = playerId,
            PlayerName = player?.Name ?? "Unknown"
        };

        // Get recent matches for this player
        var recentMatchIds = await dbContext.MatchResults
            .Where(mr => mr.PlayerId == playerId)
            .OrderByDescending(mr => mr.Match.CreatedOn)
            .Take(limit)
            .Select(mr => mr.MatchId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var matchId in recentMatchIds)
        {
            var keyMoments = await GetKeyMomentsForMatchAsync(matchId, ct);
            dto.RecentMatches.Add(keyMoments);
        }

        return dto;
    }
}

