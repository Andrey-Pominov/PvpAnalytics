import { create } from 'zustand'
import axios from 'axios'
import type { PlayerStatistics } from '../types/stats'

interface StatsState {
  data: PlayerStatistics | null
  loading: boolean
  error: string | null
  loadStats: (playerId?: string) => Promise<void>
}

export const useStatsStore = create<StatsState>((set) => ({
  data: null,
  loading: false,
  error: null,
  async loadStats(playerId = 'elyssia-priest') {
    set({ loading: true, error: null })

    try {
      const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL as string | undefined

      if (!baseUrl) {
        throw new Error('Analytics API base URL is not configured.')
      }

      const encodedId = encodeURIComponent(playerId)
      const { data } = await axios.get<PlayerStatistics>(`${baseUrl}/players/${encodedId}/stats`, {
        timeout: 5000,
      })
      set({ data, loading: false })
    } catch (error) {
      console.error('Failed to load stats', error)
      set({ error: 'Failed to load statistics', loading: false })
    }
  },
}))

