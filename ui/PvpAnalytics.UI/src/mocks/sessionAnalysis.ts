import { getSecureRandomFloat, getSecureRandomInt } from '../utils/secureRandom'

const generateSessions = () => {
  const sessions = []
  const startDate = new Date('2025-11-18T14:00:00Z')
  
  for (let i = 0; i < 8; i++) {
    const sessionStart = new Date(startDate.getTime() + i * 2 * 60 * 60 * 1000)
    const matchCount = getSecureRandomInt(3, 8) // 3-7 matches
    const wins = Math.floor(matchCount * (0.5 + getSecureRandomFloat() * 0.3)) // 50-80% win rate
    const ratingStart = 2200 + getSecureRandomInt(0, 200)
    const ratingChange = getSecureRandomInt(-30, 31) // -30 to +30
    const ratingEnd = ratingStart + ratingChange
    
    const sessionEnd = new Date(sessionStart.getTime() + matchCount * 5 * 60 * 1000)
    sessions.push({
      sessionId: i + 1,
      startTime: sessionStart.toISOString(),
      endTime: sessionEnd.toISOString(),
      duration: {
        hours: Math.floor(matchCount * 5 / 60),
        minutes: (matchCount * 5) % 60,
        seconds: 0,
        totalMinutes: matchCount * 5,
        totalSeconds: matchCount * 5 * 60
      },
      matchCount: matchCount,
      wins: wins,
      losses: matchCount - wins,
      winRate: Math.round((wins / matchCount) * 100 * 100) / 100,
      ratingStart: ratingStart,
      ratingEnd: ratingEnd,
      ratingChange: ratingChange,
      averageMatchDuration: 280 + getSecureRandomInt(0, 40),
      dayOfWeek: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'][sessionStart.getDay()],
      hourOfDay: sessionStart.getHours()
    })
  }
  
  return sessions
}

export const mockSessionAnalysis = {
  playerId: 1,
  playerName: 'Elyssia',
  thresholdMinutes: 60,
  startDate: null,
  endDate: null,
  sessions: generateSessions(),
  summary: {
    totalSessions: 8,
    averageSessionDuration: 42.5,
    averageMatchesPerSession: 5.2,
    averageWinRate: 62.3,
    totalRatingGain: 145,
    totalRatingLoss: 95,
    netRatingChange: 50
  },
  optimalTimes: {
    byHour: [
      { timeSlot: '14-15', sessions: 2, winRate: 68.5, averageRatingChange: 8.5 },
      { timeSlot: '16-17', sessions: 2, winRate: 65.2, averageRatingChange: 6.2 },
      { timeSlot: '18-19', sessions: 2, winRate: 58.3, averageRatingChange: -2.1 },
      { timeSlot: '20-21', sessions: 2, winRate: 55.8, averageRatingChange: -4.2 }
    ],
    byDayOfWeek: [
      { timeSlot: 'Monday', sessions: 2, winRate: 65.0, averageRatingChange: 5.0 },
      { timeSlot: 'Tuesday', sessions: 1, winRate: 70.0, averageRatingChange: 12.0 },
      { timeSlot: 'Wednesday', sessions: 2, winRate: 60.0, averageRatingChange: 2.0 },
      { timeSlot: 'Thursday', sessions: 1, winRate: 55.0, averageRatingChange: -5.0 },
      { timeSlot: 'Friday', sessions: 2, winRate: 62.5, averageRatingChange: 4.5 }
    ],
    bestHour: '14-15',
    bestDay: 'Tuesday'
  },
  fatigue: {
    performanceByOrder: [
      { sessionOrder: 1, sessionCount: 8, averageWinRate: 68.5, averageRatingChange: 8.2 },
      { sessionOrder: 2, sessionCount: 5, averageWinRate: 62.3, averageRatingChange: 3.5 },
      { sessionOrder: 3, sessionCount: 3, averageWinRate: 55.8, averageRatingChange: -2.1 }
    ],
    showsFatigue: true,
    fatiguePattern: 'Declining',
    optimalSessionLength: 45
  }
}

export const mockOptimalPlayTimes = mockSessionAnalysis.optimalTimes
export const mockFatigueAnalysis = mockSessionAnalysis.fatigue

