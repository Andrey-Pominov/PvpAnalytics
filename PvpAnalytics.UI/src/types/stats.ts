export interface PlayerSummary {
  id: string
  name: string
  title: string
  className: string
  rating: number
  avatarUrl?: string
}

export interface WinRateEntry {
  label: string
  value: number
}

export interface MatchSummary {
  id: string
  date: string
  mode: string
  map: string
  result: 'Victory' | 'Defeat'
  duration: string
}

export interface ParticipantStatLine {
  name: string
  className: string
  specialization: string
  role: string
  damageDone: number
  healingDone: number
  crowdControl: number
}

export interface MatchDetail {
  id: string
  mode: string
  map: string
  ratingDelta: number
  result: 'Victory' | 'Defeat'
  teams: Array<{
    name: string
    players: ParticipantStatLine[]
  }>
  timeline: Array<{
    timestamp: number
    description: string
  }>
}

export interface PlayerStatistics {
  player: PlayerSummary
  overviewTrend: number[]
  winRateByBracket: WinRateEntry[]
  winRateByMap: WinRateEntry[]
  matches: MatchSummary[]
  highlight: MatchDetail
}

