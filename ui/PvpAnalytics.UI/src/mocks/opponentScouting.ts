export const mockOpponentScout = {
  playerId: 1,
  playerName: 'Elyssia',
  realm: 'Ravencrest',
  class: 'Priest',
  currentSpec: 'Discipline',
  currentRating: 2450,
  peakRating: 2680,
  totalMatches: 342,
  winRate: 62.5,
  commonCompositions: [
    {
      composition: 'Priest-Mage-Rogue',
      matches: 89,
      wins: 58,
      winRate: 65.17,
      averageRating: 2400
    },
    {
      composition: 'Priest-Warrior-Paladin',
      matches: 67,
      wins: 42,
      winRate: 62.69,
      averageRating: 2350
    },
    {
      composition: 'Priest-Druid-Hunter',
      matches: 45,
      wins: 28,
      winRate: 62.22,
      averageRating: 2300
    }
  ],
  preferredMaps: [
    { mapName: 'Nagrand Arena', matches: 78, wins: 52, winRate: 66.67 },
    { mapName: 'Dalaran Arena', matches: 65, wins: 41, winRate: 63.08 },
    { mapName: 'Maldraxxus Coliseum', matches: 54, wins: 33, winRate: 61.11 }
  ],
  playstyle: {
    averageDamagePerMatch: 125000,
    averageHealingPerMatch: 450000,
    averageCCPerMatch: 8.5,
    style: 'Defensive',
    averageMatchDuration: 285
  },
  classMatchups: [
    { opponentClass: 'Rogue', opponentSpec: 'Assassination', matches: 45, wins: 28, winRate: 62.22 },
    { opponentClass: 'Mage', opponentSpec: 'Frost', matches: 38, wins: 24, winRate: 63.16 },
    { opponentClass: 'Warrior', opponentSpec: 'Arms', matches: 32, wins: 18, winRate: 56.25 },
    { opponentClass: 'Paladin', opponentSpec: 'Retribution', matches: 28, wins: 19, winRate: 67.86 }
  ]
}

export const mockOpponentScoutList = [
  mockOpponentScout,
  {
    ...mockOpponentScout,
    playerId: 2,
    playerName: 'Danara',
    class: 'Druid',
    currentSpec: 'Restoration',
    currentRating: 2320,
    peakRating: 2550,
    winRate: 58.3
  },
  {
    ...mockOpponentScout,
    playerId: 3,
    playerName: 'Varian',
    class: 'Warrior',
    currentSpec: 'Arms',
    currentRating: 2180,
    peakRating: 2400,
    winRate: 55.2
  }
]

