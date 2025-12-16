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
        if (state && isValidTheme(state.theme)) {
          applyThemeClass(state.theme)
        } else {
          const defaultTheme = getDefaultTheme()
          applyThemeClass(defaultTheme)
            if(state) {
                state.theme = defaultTheme
            }
        }
      },
    }
  )
)

