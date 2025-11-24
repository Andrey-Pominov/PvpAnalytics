const generateRatingDataPoints = () => {
  const points = []
  let currentRating = 1800
  const startDate = new Date('2025-10-01')
  
  for (let i = 0; i < 50; i++) {
    const ratingChange = Math.floor(Math.random() * 40) - 20 // -20 to +20
    const newRating = Math.max(1500, Math.min(2800, currentRating + ratingChange))
    const isWinner = ratingChange > 0
    
    points.push({
      matchDate: new Date(startDate.getTime() + i * 24 * 60 * 60 * 1000).toISOString(),
      matchId: 100 + i,
      ratingBefore: currentRating,
      ratingAfter: newRating,
      ratingChange: ratingChange,
      isWinner: isWinner,
      spec: 'Discipline',
      gameMode: i % 3 === 0 ? 'TwoVsTwo' : 'ThreeVsThree'
    })
    
    currentRating = newRating
  }
  
  return points
}

export const mockRatingProgression = {
  playerId: 1,
  playerName: 'Elyssia',
  gameMode: null,
  spec: null,
  startDate: null,
  endDate: null,
  dataPoints: generateRatingDataPoints(),
  summary: {
    currentRating: 2450,
    peakRating: 2680,
    lowestRating: 1780,
    averageRating: 2234.5,
    totalRatingGain: 1240,
    totalRatingLoss: 580,
    netRatingChange: 660
  }
}

export const mockRatingSummary = {
  currentRating: 2450,
  peakRating: 2680,
  lowestRating: 1780,
  averageRating: 2234.5,
  totalRatingGain: 1240,
  totalRatingLoss: 580,
  netRatingChange: 660
}

