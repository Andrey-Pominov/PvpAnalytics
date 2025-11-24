import { create } from 'zustand'
import axios from 'axios'
import type { PlayerStatistics } from '../types/stats'
import { mockPlayerStatistics } from '../mocks/playerStats'

interface StatsState {
  data: PlayerStatistics | null
  loading: boolean
  error: string | null
  loadStats: (playerId?: string) => Promise<void>
}

const cloneMock = (stats: PlayerStatistics): PlayerStatistics => JSON.parse(JSON.stringify(stats))

export const useStatsStore = create<StatsState>((set) => ({
  data: null,
  loading: false,
  error: null,
  async loadStats(playerId = 'elyssia-priest') {
    set({ loading: true, error: null })

    try {
      const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL as string | undefined

      if (baseUrl && baseUrl !== 'mock') {
        const encodedId = encodeURIComponent(playerId)
        const { data } = await axios.get<PlayerStatistics>(`${baseUrl}/players/${encodedId}/stats`, {
          timeout: 5000,
        })
        set({ data, loading: false })
        return
      }

      // Mock data until API endpoint is ready. Remove this block when wiring real API responses.
      const data = cloneMock(mockPlayerStatistics)
      set({ data, loading: false })
    } catch (error) {
      console.error('Failed to load stats', error)
      set({ error: 'Failed to load statistics', loading: false })
    }
  },
}))

