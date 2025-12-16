import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export interface RecentSearch {
  term: string
  type: 'player' | 'match' | 'general'
  timestamp: number
}

interface SearchState {
  recentSearches: RecentSearch[]
  addRecentSearch: (term: string, type: 'player' | 'match' | 'general') => void
  clearRecentSearches: () => void
}

const MAX_RECENT_SEARCHES = 10

export const useSearchStore = create<SearchState>()(
  persist(
    (set) => ({
      recentSearches: [],
      addRecentSearch: (term, type) => set((state) => {
        const trimmedTerm = term.trim()
        if (!trimmedTerm) return state

        // Remove existing entry with same term
        const filtered = state.recentSearches.filter((s) => s.term !== trimmedTerm)
        
        // Add new entry at the beginning
        const newSearch: RecentSearch = {
          term: trimmedTerm,
          type,
          timestamp: Date.now(),
        }

        // Keep only the most recent MAX_RECENT_SEARCHES
        const newSearches = [newSearch, ...filtered].slice(0, MAX_RECENT_SEARCHES)
        
        return { recentSearches: newSearches }
      }),
      clearRecentSearches: () => set({ recentSearches: [] }),
    }),
    {
      name: 'search-history',
    }
  )
)

