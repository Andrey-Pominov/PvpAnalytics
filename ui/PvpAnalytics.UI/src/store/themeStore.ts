import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { Theme } from '../config/themeConfig'
import { getDefaultTheme, getNextTheme, applyThemeClass, isValidTheme } from '../config/themeConfig'

interface ThemeState {
  theme: Theme
  setTheme: (theme: Theme) => void
  toggleTheme: () => void
}

export type { Theme }

export const useThemeStore = create<ThemeState>()(
  persist(
    (set) => ({
      theme: getDefaultTheme(),
      setTheme: (theme) => {
        set({ theme })
        applyThemeClass(theme)
      },
      toggleTheme: () => {
        set((state) => {
          const newTheme = getNextTheme(state.theme)
          applyThemeClass(newTheme)
          return { theme: newTheme }
        })
      },
    }),
    {
      name: 'theme-preference',
      onRehydrateStorage: () => (state) => {
        // Apply theme on rehydration, validate it first
        if (state?.theme && isValidTheme(state.theme)) {
          applyThemeClass(state.theme)
        } else {
          // If invalid or missing theme, apply default theme
          // Note: The state will be corrected on next store access via the initial state
          const defaultTheme = getDefaultTheme()
          applyThemeClass(defaultTheme)
        }
      },
    }
  )
)

