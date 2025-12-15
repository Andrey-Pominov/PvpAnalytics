import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export interface RecentlyViewedItem {
  id: number
  name: string
  realm: string
  class?: string
  faction?: string
  viewedAt: number // timestamp
}

interface RecentlyViewedState {
  items: RecentlyViewedItem[]
  addItem: (item: Omit<RecentlyViewedItem, 'viewedAt'>) => void
  removeItem: (id: number) => void
  clearAll: () => void
}

const MAX_ITEMS = 20

export const useRecentlyViewedStore = create<RecentlyViewedState>()(
  persist(
    (set) => ({
      items: [],
      addItem: (item) => set((state) => {
        // Remove existing item with same id if present
        const filtered = state.items.filter((i) => i.id !== item.id)
        // Add new item at the beginning
        const newItem: RecentlyViewedItem = {
          ...item,
          viewedAt: Date.now(),
        }
        // Keep only the most recent MAX_ITEMS
        const newItems = [newItem, ...filtered].slice(0, MAX_ITEMS)
        return { items: newItems }
      }),
      removeItem: (id) => set((state) => ({
        items: state.items.filter((i) => i.id !== id),
      })),
      clearAll: () => set({ items: [] }),
    }),
    {
      name: 'recently-viewed-players',
    }
  )
)

