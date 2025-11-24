export const mockPerformanceComparison = {
  playerId: 1,
  playerName: 'Elyssia',
  spec: 'Discipline',
  ratingMin: 2000,
  playerMetrics: {
    winRate: 62.5,
    averageDamage: 125000,
    averageHealing: 450000,
    averageCC: 8.5,
    currentRating: 2450,
    peakRating: 2680,
    averageMatchDuration: 285
  },
  topPlayerMetrics: {
    averageWinRate: 65.8,
    averageDamage: 118000,
    averageHealing: 480000,
    averageCC: 9.2,
    averageRating: 2520,
    averageMatchDuration: 275
  },
  gaps: {
    winRateGap: -3.3,
    damageGap: 7000,
    healingGap: -30000,
    CCGap: -0.7,
    ratingGap: -70,
    strengths: ['Damage Output'],
    weaknesses: ['Win Rate', 'Healing Output', 'CC Usage']
  },
  percentiles: {
    winRatePercentile: 58.5,
    damagePercentile: 72.3,
    healingPercentile: 42.1,
    CCPercentile: 38.9,
    ratingPercentile: 61.2
  }
}

export const mockPercentileRankings = {
  winRatePercentile: 58.5,
  damagePercentile: 72.3,
  healingPercentile: 42.1,
  CCPercentile: 38.9,
  ratingPercentile: 61.2
}

