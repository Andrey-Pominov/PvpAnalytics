import type { PlayerStatistics } from '../types/stats'

export const mockPlayerStatistics: PlayerStatistics = {
  player: {
    id: 'elyssia-priest',
    name: 'Elyssia',
    title: 'Night Elf Priest',
    className: 'Priest',
    rating: 1970,
    avatarUrl: undefined,
  },
  overviewTrend: [48, 52, 49, 55, 58, 61, 66, 64, 68, 70],
  winRateByBracket: [
    { label: '2v2', value: 62 },
    { label: '3v3', value: 55 },
    { label: 'RBG', value: 61 },
  ],
  winRateByMap: [
    { label: 'Danara', value: 64 },
    { label: 'Elyssia', value: 58 },
    { label: 'Varian', value: 0 },
  ],
  matches: [
    { id: '1', date: 'Apr 23', mode: '2 vs 2', map: 'Nagrand Arena', result: 'Victory', duration: '2:35' },
    { id: '2', date: 'Apr 23', mode: '2 vs 3', map: 'Ruins of Lordaeron', result: 'Victory', duration: '4:17' },
    { id: '3', date: 'Apr 22', mode: '2 vs 3', map: "Blade's Edge", result: 'Defeat', duration: '4:17' },
    { id: '4', date: 'Apr 20', mode: '2 vs 3', map: 'Nagrand Arena', result: 'Victory', duration: '2:35' },
  ],
  highlight: {
    id: 'highlight-1',
    mode: '3v3',
    map: 'Nagrand Arena',
    ratingDelta: 3.08,
    result: 'Victory',
    teams: [
      {
        name: 'Danara',
        players: [
          { name: 'Druid', className: 'Druid', specialization: 'Restoration', role: 'Healer', damageDone: 518_000, healingDone: 0, crowdControl: 6.9 },
          { name: 'Elyssia', className: 'Priest', specialization: 'Discipline', role: 'Healer', damageDone: 1_360_000, healingDone: 952_000, crowdControl: 35.1 },
          { name: 'Varian', className: 'Warrior', specialization: 'Arms', role: 'Damage', damageDone: 1_670_000, healingDone: 0, crowdControl: 9.1 },
        ],
      },
      {
        name: 'Thandor',
        players: [
          { name: 'Thandor', className: 'Paladin', specialization: 'Holy', role: 'Healer', damageDone: 585_000, healingDone: 585_000, crowdControl: 35.1 },
          { name: 'Rugina', className: 'Rogue', specialization: 'Subtlety', role: 'Damage', damageDone: 1_560_000, healingDone: 0, crowdControl: 9.0 },
          { name: 'Unknown', className: 'Mage', specialization: 'Frost', role: 'Damage', damageDone: 1_020_000, healingDone: 0, crowdControl: 8.5 },
        ],
      },
    ],
    timeline: [
      { timestamp: 0, description: 'Opening engage' },
      { timestamp: 46, description: 'Elyssia lands Psychic Scream' },
      { timestamp: 92, description: "Danara uses Tranquility" },
      { timestamp: 172, description: 'Varian executes final blow' },
    ],
  },
}

