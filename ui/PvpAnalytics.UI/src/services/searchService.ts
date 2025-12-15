import axios from 'axios'
import type { Player } from '../types/api'

export interface SearchResult {
  type: 'player' | 'match'
  id: number
  title: string
  subtitle?: string
  metadata?: string
}

export interface SearchResponse {
  players: Player[]
  matches: Array<{
    id: number
    createdOn: string
    gameMode: string
    arenaZone: number
  }>
}

const getBaseUrl = (): string => {
  return import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
}

// Debounce helper
let searchTimeout: ReturnType<typeof setTimeout> | null = null

export const searchPlayers = async (term: string): Promise<Player[]> => {
  if (!term || term.trim().length < 2) {
    return []
  }

  try {
    const baseUrl = getBaseUrl()
    const { data } = await axios.get<Player[]>(`${baseUrl}/players`, {
      params: { search: term.trim() },
    })
    return data.slice(0, 8) // Limit to 8 results
  } catch (error) {
    console.error('Error searching players:', error)
    return []
  }
}

export const searchMatches = async (term: string): Promise<Array<{ id: number; createdOn: string; gameMode: string; arenaZone: number }>> => {
  if (!term || term.trim().length < 2) {
    return []
  }

  try {
    const baseUrl = getBaseUrl()
    // Try to parse as match ID first
    const matchId = Number.parseInt(term.trim(), 10)
    if (!Number.isNaN(matchId)) {
      try {
        const { data } = await axios.get(`${baseUrl}/matches/${matchId}`, {
          validateStatus: (status) => status === 200 || status === 404,
        })
        if (data?.id) {
          return [{
            id: data.id,
            createdOn: data.createdOn || new Date().toISOString(),
            gameMode: data.gameMode || 'Unknown',
            arenaZone: data.arenaZone || 0,
          }]
        }
      } catch {
        // If not found, continue with search
      }
    }

    // Search matches by term (if API supports search parameter)
    try {
      const { data } = await axios.get(`${baseUrl}/matches`, {
        params: { search: term.trim() },
        validateStatus: (status) => status === 200 || status === 404,
      })
      if (Array.isArray(data)) {
        return data.slice(0, 8).map((m: any) => ({
          id: m.id,
          createdOn: m.createdOn || new Date().toISOString(),
          gameMode: m.gameMode || 'Unknown',
          arenaZone: m.arenaZone || 0,
        }))
      }
    } catch {
      // API might not support search parameter, return empty
    }

    return []
  } catch (error) {
    console.error('Error searching matches:', error)
    return []
  }
}

export const debouncedSearch = async (
  term: string,
  callback: (results: SearchResponse) => void
): Promise<void> => {
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }

  searchTimeout = setTimeout(async () => {
    if (term.trim().length < 2) {
      callback({ players: [], matches: [] })
      return
    }

    const [players, matches] = await Promise.all([
      searchPlayers(term),
      searchMatches(term),
    ])

    callback({ players, matches })
  }, 300)
}

