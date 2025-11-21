export interface Player {
  id: number
  name: string
  realm: string
  class: string
  faction: string
}

export interface Match {
  id: number
  uniqueHash: string
  createdOn: string
  arenaZone: number
  arenaMatchId: string | null
  gameMode: string
  duration: number
  isRanked: boolean
}

export interface PlayerStats {
  playerId: number
  playerName: string
  realm: string
  totalMatches: number
  wins: number
  losses: number
  winRate: number
  averageMatchDuration: number
  favoriteGameMode: string | null
  favoriteSpec: string | null
}

export interface PlayerMatch {
  matchId: number
  createdOn: string
  mapName: string
  arenaZone: number
  gameMode: string
  duration: number
  isRanked: boolean
  isWinner: boolean
  ratingBefore: number
  ratingAfter: number
  spec: string | null
}

export type UploadResponse = Match[]

export interface MatchBasicInfo {
  id: number
  uniqueHash: string
  createdOn: string
  mapName: string
  arenaZone: number
  gameMode: string
  duration: number
  isRanked: boolean
}

export interface ParticipantInfo {
  playerId: number
  playerName: string
  realm: string
  class: string
  spec: string | null
  team: string
  ratingBefore: number
  ratingAfter: number
  isWinner: boolean
  totalDamage: number
  totalHealing: number
  totalCC: number
}

export interface TeamInfo {
  teamName: string
  participants: ParticipantInfo[]
  totalDamage: number
  totalHealing: number
  isWinner: boolean
}

export interface TimelineEvent {
  timestamp: number
  eventType: string
  sourcePlayerId: number | null
  sourcePlayerName: string | null
  targetPlayerId: number | null
  targetPlayerName: string | null
  ability: string
  damageDone: number | null
  healingDone: number | null
  crowdControl: string | null
  isImportant: boolean
  isCooldown: boolean
  isCC: boolean
}

export interface MatchDetailDto {
  basicInfo: MatchBasicInfo
  teams: TeamInfo[]
  timelineEvents: TimelineEvent[]
}

