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

export type UploadResponse = Match[]

