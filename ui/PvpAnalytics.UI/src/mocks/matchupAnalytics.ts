export const mockMatchupAnalytics = {
  class1: 'Rogue',
  spec1: 'Assassination',
  class2: 'Mage',
  spec2: 'Frost',
  gameMode: 'ThreeVsThree',
  ratingMin: 2000,
  ratingMax: 2500,
  totalMatches: 156,
  winsForClass1: 89,
  winsForClass2: 67,
  winRateForClass1: 57.05,
  winRateForClass2: 42.95,
  averageMatchDuration: 278,
  stats: {
    averageDamageClass1: 185000,
    averageDamageClass2: 165000,
    averageHealingClass1: 12000,
    averageHealingClass2: 15000,
    averageCCClass1: 12.5,
    averageCCClass2: 10.3
  },
  commonStrategies: [
    'Rogue opens with stun, Mage follows with burst',
    'Mage kites while Rogue peels',
    'Both focus same target for quick kill'
  ]
}

export const mockPlayerMatchupSummary = {
  playerId: 1,
  playerName: 'Elyssia',
  weaknesses: [
    { opponentClass: 'Rogue', opponentSpec: 'Subtlety', matches: 23, wins: 8, winRate: 34.78 },
    { opponentClass: 'Warrior', opponentSpec: 'Fury', matches: 19, wins: 7, winRate: 36.84 },
    { opponentClass: 'Death Knight', opponentSpec: 'Unholy', matches: 15, wins: 6, winRate: 40.00 }
  ],
  strengths: [
    { opponentClass: 'Hunter', opponentSpec: 'Marksmanship', matches: 28, wins: 22, winRate: 78.57 },
    { opponentClass: 'Warlock', opponentSpec: 'Affliction', matches: 24, wins: 18, winRate: 75.00 },
    { opponentClass: 'Shaman', opponentSpec: 'Elemental', matches: 21, wins: 15, winRate: 71.43 }
  ]
}

