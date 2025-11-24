export const mockKeyMoments = {
  matchId: 1,
  matchDate: '2025-11-20T00:33:57Z',
  moments: [
    {
      timestamp: 5,
      eventType: 'cooldown',
      description: 'Elyssia used Power Word: Shield',
      sourcePlayerId: 1,
      sourcePlayerName: 'Elyssia',
      targetPlayerId: 5,
      targetPlayerName: 'Rugina',
      ability: 'Power Word: Shield',
      damageDone: null,
      healingDone: 45000,
      crowdControl: null,
      impactScore: 0.7,
      isCritical: false
    },
    {
      timestamp: 12,
      eventType: 'cc_chain',
      description: 'CC chain on Rugina',
      sourcePlayerId: 9,
      sourcePlayerName: 'Faitlesbrulé',
      targetPlayerId: 5,
      targetPlayerName: 'Rugina',
      ability: 'Polymorph',
      damageDone: null,
      healingDone: null,
      crowdControl: 'Polymorph',
      impactScore: 0.8,
      isCritical: true
    },
    {
      timestamp: 45,
      eventType: 'damage_spike',
      description: 'Rugina dealt 185,000 damage',
      sourcePlayerId: 5,
      sourcePlayerName: 'Rugina',
      targetPlayerId: 12,
      targetPlayerName: 'Iwannacry',
      ability: 'Eviscerate',
      damageDone: 185000,
      healingDone: null,
      crowdControl: null,
      impactScore: 0.9,
      isCritical: true
    },
    {
      timestamp: 78,
      eventType: 'death',
      description: 'Iwannacry died',
      sourcePlayerId: 5,
      sourcePlayerName: 'Rugina',
      targetPlayerId: 12,
      targetPlayerName: 'Iwannacry',
      ability: 'Killing Blow',
      damageDone: 125000,
      healingDone: null,
      crowdControl: null,
      impactScore: 0.95,
      isCritical: true
    },
    {
      timestamp: 273,
      eventType: 'rating_change',
      description: 'Elyssia gained 18 rating',
      targetPlayerId: 1,
      impactScore: 0.6,
      isCritical: false
    }
  ]
}

export const mockPlayerKeyMoments = {
  playerId: 1,
  playerName: 'Elyssia',
  recentMatches: [
    mockKeyMoments,
    {
      ...mockKeyMoments,
      matchId: 2,
      matchDate: '2025-11-20T00:28:30Z',
      moments: mockKeyMoments.moments.slice(0, 3)
    },
    {
      ...mockKeyMoments,
      matchId: 3,
      matchDate: '2025-11-19T23:34:30Z',
      moments: mockKeyMoments.moments.slice(1, 4)
    }
  ]
}

