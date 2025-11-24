using Microsoft.EntityFrameworkCore;
using PvpAnalytics.Core.DTOs;
using PvpAnalytics.Infrastructure;

namespace PvpAnalytics.Application.Services;

public interface ISessionAnalysisService
{
    Task<SessionAnalysisDto> GetSessionAnalysisAsync(
        long playerId,
        int thresholdMinutes = 60,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);
    Task<OptimalPlayTimes> GetOptimalPlayTimesAsync(long playerId, CancellationToken ct = default);
    Task<FatigueAnalysis> GetFatigueAnalysisAsync(long playerId, CancellationToken ct = default);
}

public class SessionAnalysisService(PvpAnalyticsDbContext dbContext) : ISessionAnalysisService
{
    public async Task<SessionAnalysisDto> GetSessionAnalysisAsync(
        long playerId,
        int thresholdMinutes = 60,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        var player = await dbContext.Players.FindAsync([playerId], ct);
        var dto = new SessionAnalysisDto
        {
            PlayerId = playerId,
            PlayerName = player?.Name ?? "Unknown",
            ThresholdMinutes = thresholdMinutes,
            StartDate = startDate,
            EndDate = endDate
        };

        var query = dbContext.MatchResults
            .Include(mr => mr.Match)
            .Where(mr => mr.PlayerId == playerId)
            .OrderBy(mr => mr.Match.CreatedOn)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(mr => mr.Match.CreatedOn >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(mr => mr.Match.CreatedOn <= endDate.Value);
        }

        var matchResults = await query.ToListAsync(ct);

        if (!matchResults.Any())
            return dto;

        // Group matches into sessions based on time threshold
        var sessions = new List<SessionData>();
        var currentSession = new List<(DateTime matchDate, int ratingBefore, int ratingAfter, bool isWinner, long matchId, long duration)>();

        for (int i = 0; i < matchResults.Count; i++)
        {
            var current = matchResults[i];
            var matchDate = current.Match.CreatedOn;

            if (currentSession.Count == 0)
            {
                // Start new session
                currentSession.Add((matchDate, current.RatingBefore, current.RatingAfter, current.IsWinner, current.MatchId, current.Match.Duration));
            }
            else
            {
                var lastMatchDate = currentSession.Last().matchDate;
                var timeDiff = (matchDate - lastMatchDate).TotalMinutes;

                if (timeDiff <= thresholdMinutes)
                {
                    // Continue current session
                    currentSession.Add((matchDate, current.RatingBefore, current.RatingAfter, current.IsWinner, current.MatchId, current.Match.Duration));
                }
                else
                {
                    // End current session and start new one
                    sessions.Add(CreateSessionData(currentSession, sessions.Count + 1));
                    currentSession = new List<(DateTime, int, int, bool, long, long)>
                    {
                        (matchDate, current.RatingBefore, current.RatingAfter, current.IsWinner, current.MatchId, current.Match.Duration)
                    };
                }
            }
        }

        // Add final session
        if (currentSession.Any())
        {
            sessions.Add(CreateSessionData(currentSession, sessions.Count + 1));
        }

        dto.Sessions = sessions;

        // Calculate summary
        dto.Summary = new SessionSummary
        {
            TotalSessions = sessions.Count,
            AverageSessionDuration = sessions.Any() ? Math.Round(sessions.Average(s => s.Duration.TotalMinutes), 2) : 0,
            AverageMatchesPerSession = sessions.Any() ? Math.Round(sessions.Average(s => (double)s.MatchCount), 2) : 0,
            AverageWinRate = sessions.Any() ? Math.Round(sessions.Average(s => s.WinRate), 2) : 0,
            TotalRatingGain = sessions.Sum(s => s.RatingChange > 0 ? s.RatingChange : 0),
            TotalRatingLoss = Math.Abs(sessions.Sum(s => s.RatingChange < 0 ? s.RatingChange : 0)),
            NetRatingChange = sessions.Sum(s => s.RatingChange)
        };

        // Get optimal times
        dto.OptimalTimes = await GetOptimalPlayTimesAsync(playerId, ct);

        // Get fatigue analysis
        dto.Fatigue = await GetFatigueAnalysisAsync(playerId, ct);

        return dto;
    }

    private static SessionData CreateSessionData(
        List<(DateTime matchDate, int ratingBefore, int ratingAfter, bool isWinner, long matchId, long duration)> matches,
        long sessionId)
    {
        var startTime = matches.First().matchDate;
        var endTime = matches.Last().matchDate;
        var wins = matches.Count(m => m.isWinner);
        var ratingStart = matches.First().ratingBefore;
        var ratingEnd = matches.Last().ratingAfter;

        return new SessionData
        {
            SessionId = sessionId,
            StartTime = startTime,
            EndTime = endTime,
            Duration = endTime - startTime,
            MatchCount = matches.Count,
            Wins = wins,
            Losses = matches.Count - wins,
            WinRate = matches.Count > 0 ? Math.Round(wins * 100.0 / matches.Count, 2) : 0,
            RatingStart = ratingStart,
            RatingEnd = ratingEnd,
            RatingChange = ratingEnd - ratingStart,
            AverageMatchDuration = matches.Any() ? Math.Round(matches.Average(m => (double)m.duration), 2) : 0,
            DayOfWeek = startTime.DayOfWeek.ToString(),
            HourOfDay = startTime.Hour
        };
    }

    public async Task<OptimalPlayTimes> GetOptimalPlayTimesAsync(long playerId, CancellationToken ct = default)
    {
        var sessions = await GetSessionAnalysisAsync(playerId, 60, null, null, ct);
        var sessionData = sessions.Sessions;

        if (!sessionData.Any())
            return new OptimalPlayTimes();

        // Group by hour
        var byHour = sessionData
            .GroupBy(s => s.HourOfDay)
            .Select(g => new TimeSlotPerformance
            {
                TimeSlot = $"{g.Key}-{g.Key + 1}",
                Sessions = g.Count(),
                WinRate = g.Count() > 0 ? Math.Round(g.Average(s => s.WinRate), 2) : 0,
                AverageRatingChange = g.Count() > 0 ? Math.Round(g.Average(s => (double)s.RatingChange), 2) : 0
            })
            .OrderByDescending(t => t.WinRate)
            .ToList();

        // Group by day of week
        var byDay = sessionData
            .GroupBy(s => s.DayOfWeek)
            .Select(g => new TimeSlotPerformance
            {
                TimeSlot = g.Key,
                Sessions = g.Count(),
                WinRate = g.Count() > 0 ? Math.Round(g.Average(s => s.WinRate), 2) : 0,
                AverageRatingChange = g.Count() > 0 ? Math.Round(g.Average(s => (double)s.RatingChange), 2) : 0
            })
            .OrderByDescending(t => t.WinRate)
            .ToList();

        var bestHour = byHour.OrderByDescending(h => h.WinRate).FirstOrDefault();
        var bestDay = byDay.OrderByDescending(d => d.WinRate).FirstOrDefault();

        return new OptimalPlayTimes
        {
            ByHour = byHour,
            ByDayOfWeek = byDay,
            BestHour = bestHour?.TimeSlot ?? "Unknown",
            BestDay = bestDay?.TimeSlot ?? "Unknown"
        };
    }

    public async Task<FatigueAnalysis> GetFatigueAnalysisAsync(long playerId, CancellationToken ct = default)
    {
        var sessions = await GetSessionAnalysisAsync(playerId, 60, null, null, ct);
        var sessionData = sessions.Sessions;

        if (!sessionData.Any())
            return new FatigueAnalysis();

        // Group sessions by date and order within day
        var sessionsByDate = sessionData
            .GroupBy(s => s.StartTime.Date)
            .SelectMany(g => g.OrderBy(s => s.StartTime).Select((s, index) => new
            {
                SessionOrder = index + 1,
                Session = s
            }))
            .ToList();

        // Group by session order
        var performanceByOrder = sessionsByDate
            .GroupBy(s => s.SessionOrder)
            .Select(g => new SessionPerformanceByOrder
            {
                SessionOrder = g.Key,
                SessionCount = g.Count(),
                AverageWinRate = g.Count() > 0 ? Math.Round(g.Average(s => s.Session.WinRate), 2) : 0,
                AverageRatingChange = g.Count() > 0 ? Math.Round(g.Average(s => (double)s.Session.RatingChange), 2) : 0
            })
            .OrderBy(p => p.SessionOrder)
            .ToList();

        // Determine fatigue pattern
        var firstHalf = performanceByOrder.Take(performanceByOrder.Count / 2).ToList();
        var secondHalf = performanceByOrder.Skip(performanceByOrder.Count / 2).ToList();

        var firstHalfAvg = firstHalf.Any() ? firstHalf.Average(p => p.AverageWinRate) : 0;
        var secondHalfAvg = secondHalf.Any() ? secondHalf.Average(p => p.AverageWinRate) : 0;

        var showsFatigue = secondHalfAvg < firstHalfAvg - 5; // 5% drop indicates fatigue
        var fatiguePattern = showsFatigue ? "Declining" : (secondHalfAvg > firstHalfAvg + 5 ? "Improving" : "Stable");

        // Calculate optimal session length (average duration of best performing sessions)
        var bestSessions = sessionData
            .OrderByDescending(s => s.WinRate)
            .Take(Math.Max(1, sessionData.Count / 4))
            .ToList();

        var optimalLength = bestSessions.Any() ? (int)Math.Round(bestSessions.Average(s => s.Duration.TotalMinutes)) : 60;

        return new FatigueAnalysis
        {
            PerformanceByOrder = performanceByOrder,
            ShowsFatigue = showsFatigue,
            FatiguePattern = fatiguePattern,
            OptimalSessionLength = optimalLength
        };
    }
}

