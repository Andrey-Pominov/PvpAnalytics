export const mockTeamComposition = {
  teamId: 1,
  composition: 'Rogue-Mage-Priest',
  members: [
    {
      playerId: 1,
      playerName: 'Elyssia',
      realm: 'Ravencrest',
      class: 'Priest',
      spec: 'Discipline'
    },
    {
      playerId: 5,
      playerName: 'Rugina',
      realm: 'Silvermoon',
      class: 'Rogue',
      spec: 'Assassination'
    },
    {
      playerId: 9,
      playerName: 'Faitlesbrulé',
      realm: 'Archimonde',
      class: 'Mage',
      spec: 'Frost'
    }
  ],
  totalMatches: 156,
  wins: 98,
  losses: 58,
  winRate: 62.82,
  averageRating: 2380,
  peakRating: 2650,
  synergyScore: 78.5,
  firstMatchDate: '2025-10-15T10:00:00Z',
  lastMatchDate: '2025-11-20T15:30:00Z'
}

export const mockPlayerTeams = [
  mockTeamComposition,
  {
    ...mockTeamComposition,
    teamId: 2,
    composition: 'Priest-Warrior-Paladin',
    members: [
      {
        playerId: 1,
        playerName: 'Elyssia',
        realm: 'Ravencrest',
        class: 'Priest',
        spec: 'Discipline'
      },
      {
        playerId: 3,
        playerName: 'Varian',
        realm: 'Ravencrest',
        class: 'Warrior',
        spec: 'Arms'
      },
      {
        playerId: 4,
        playerName: 'Thandor',
        realm: 'Silvermoon',
        class: 'Paladin',
        spec: 'Retribution'
      }
    ],
    totalMatches: 89,
    wins: 52,
    losses: 37,
    winRate: 58.43,
    averageRating: 2250,
    peakRating: 2480,
    synergyScore: 65.2
  }
]

