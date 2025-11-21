using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Core.Entities;
using PvpAnalytics.Core.Logs;
using PvpAnalytics.Core.Repositories;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface IMatchDetailService
{
    Task<MatchDetailDto?> GetMatchDetailAsync(long matchId, CancellationToken ct = default);
}

public class MatchDetailService : IMatchDetailService
{
    private readonly PvpAnalyticsDbContext _dbContext;
    private readonly IRepository<Match> _matchRepo;
    private readonly IRepository<MatchResult> _matchResultRepo;
    private readonly IRepository<CombatLogEntry> _combatLogRepo;

    public MatchDetailService(
        PvpAnalyticsDbContext dbContext,
        IRepository<Match> matchRepo,
        IRepository<MatchResult> matchResultRepo,
        IRepository<CombatLogEntry> combatLogRepo)
    {
        _dbContext = dbContext;
        _matchRepo = matchRepo;
        _matchResultRepo = matchResultRepo;
        _combatLogRepo = combatLogRepo;
    }

    public async Task<MatchDetailDto?> GetMatchDetailAsync(long matchId, CancellationToken ct = default)
    {
        // Load match with related entities
        var match = await _dbContext.Matches
            .Include(m => m.Results)
                .ThenInclude(r => r.Player)
            .Include(m => m.CombatLogs)
                .ThenInclude(e => e.SourcePlayer)
            .Include(m => m.CombatLogs)
                .ThenInclude(e => e.TargetPlayer)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match == null)
        {
            return null;
        }

        var matchStartTime = match.CreatedOn;

        // Build basic info
        var basicInfo = new MatchBasicInfo
        {
            Id = match.Id,
            UniqueHash = match.UniqueHash,
            CreatedOn = match.CreatedOn,
            MapName = match.MapName,
            ArenaZone = match.ArenaZone,
            GameMode = match.GameMode,
            Duration = match.Duration,
            IsRanked = match.IsRanked,
        };

        // Group participants by team and aggregate stats
        var participantsByTeam = match.Results
            .GroupBy(r => r.Team)
            .ToList();

        var teams = new List<TeamInfo>();
        var participantStats = new Dictionary<long, (long damage, long healing, int cc)>();

        // Aggregate stats from combat logs
        foreach (var entry in match.CombatLogs)
        {
            if (entry.SourcePlayerId > 0)
            {
                if (!participantStats.ContainsKey(entry.SourcePlayerId))
                {
                    participantStats[entry.SourcePlayerId] = (0, 0, 0);
                }

                var stats = participantStats[entry.SourcePlayerId];
                stats.damage += entry.DamageDone;
                stats.healing += entry.HealingDone;
                if (!string.IsNullOrWhiteSpace(entry.CrowdControl))
                {
                    stats.cc++;
                }
                participantStats[entry.SourcePlayerId] = stats;
            }
        }

        // Build teams
        foreach (var teamGroup in participantsByTeam)
        {
            var teamName = teamGroup.Key;
            var participants = teamGroup.Select(r =>
            {
                var stats = participantStats.GetValueOrDefault(r.PlayerId, (0, 0, 0));
                return new ParticipantInfo
                {
                    PlayerId = r.PlayerId,
                    PlayerName = r.Player.Name,
                    Realm = r.Player.Realm,
                    Class = r.Player.Class,
                    Spec = r.Spec,
                    Team = r.Team,
                    RatingBefore = r.RatingBefore,
                    RatingAfter = r.RatingAfter,
                    IsWinner = r.IsWinner,
                    TotalDamage = stats.damage,
                    TotalHealing = stats.healing,
                    TotalCC = stats.cc,
                };
            }).ToList();

            var teamDamage = participants.Sum(p => p.TotalDamage);
            var teamHealing = participants.Sum(p => p.TotalHealing);
            var isWinner = participants.Any(p => p.IsWinner);

            teams.Add(new TeamInfo
            {
                TeamName = teamName,
                Participants = participants,
                TotalDamage = teamDamage,
                TotalHealing = teamHealing,
                IsWinner = isWinner,
            });
        }

        // Build timeline events
        var timelineEvents = match.CombatLogs
            .OrderBy(e => e.Timestamp)
            .Select(e =>
            {
                var relativeTimestamp = (long)(e.Timestamp - matchStartTime).TotalSeconds;
                var isCooldown = ImportantAbilities.IsCooldownOrDefensive(e.Ability);
                var isCC = ImportantAbilities.IsCrowdControl(e.Ability);
                var isImportant = isCooldown || isCC || e.DamageDone > 50000 || e.HealingDone > 30000; // Flag high-impact events

                string eventType = "damage";
                if (e.HealingDone > 0)
                {
                    eventType = "healing";
                }
                else if (!string.IsNullOrWhiteSpace(e.CrowdControl))
                {
                    eventType = "cc";
                }
                else if (isCooldown)
                {
                    eventType = "cooldown";
                }

                return new TimelineEvent
                {
                    Timestamp = relativeTimestamp,
                    EventType = eventType,
                    SourcePlayerId = e.SourcePlayerId,
                    SourcePlayerName = e.SourcePlayer?.Name,
                    TargetPlayerId = e.TargetPlayerId,
                    TargetPlayerName = e.TargetPlayer?.Name,
                    Ability = e.Ability,
                    DamageDone = e.DamageDone > 0 ? e.DamageDone : null,
                    HealingDone = e.HealingDone > 0 ? e.HealingDone : null,
                    CrowdControl = !string.IsNullOrWhiteSpace(e.CrowdControl) ? e.CrowdControl : null,
                    IsImportant = isImportant,
                    IsCooldown = isCooldown,
                    IsCC = isCC,
                };
            })
            .Where(e => e.IsImportant || e.DamageDone > 10000 || e.HealingDone > 5000) // Filter to important events only
            .ToList();

        return new MatchDetailDto
        {
            BasicInfo = basicInfo,
            Teams = teams,
            TimelineEvents = timelineEvents,
        };
    }
}

