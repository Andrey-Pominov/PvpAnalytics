/**
 * Centralized theme configuration
 * 
 * This module provides a single source of truth for all theme-related constants
 * and utilities. To add a new theme:
 * 1. Add the theme name to the THEMES array
 * 2. Add corresponding CSS in index.css (e.g., :root.themeName { ... })
 */

/**
 * Available themes in the application
 */
export const THEMES = ['dark', 'light'] as const

/**
 * Theme type derived from available themes
 */
export type Theme = typeof THEMES[number]

/**
 * Fallback theme when system preference cannot be detected
 */
const FALLBACK_THEME: Theme = 'dark'

/**
 * Detects the user's system theme preference
 * @returns 'dark' if prefers-color-scheme is dark, 'light' otherwise
 */
export function getSystemThemePreference(): Theme {
  if (typeof window === 'undefined') {
    return FALLBACK_THEME
  }

  try {
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
    return prefersDark ? 'dark' : 'light'
  } catch {
    return FALLBACK_THEME
  }
}

/**
 * Gets the default theme based on system preference or fallback
 * @returns The default theme to use
 */
export function getDefaultTheme(): Theme {
  return getSystemThemePreference()
}

/**
 * Type guard to check if a string is a valid theme
 * @param theme - String to validate
 * @returns True if the string is a valid theme
 */
export function isValidTheme(theme: string): theme is Theme {
  return THEMES.includes(theme as Theme)
}

/**
 * Gets the next theme in the cycle (for toggle functionality)
 * Currently cycles between dark and light
 * @param currentTheme - The current theme
 * @returns The next theme in the cycle
 */
export function getNextTheme(currentTheme: Theme): Theme {
  const currentIndex = THEMES.indexOf(currentTheme)
  const nextIndex = (currentIndex + 1) % THEMES.length
  return THEMES[nextIndex]
}

/**
 * Removes all theme classes from the document element
 * Useful when switching themes
 */
export function removeAllThemeClasses(): void {
  if (typeof document === 'undefined') {
    return
  }
  
  THEMES.forEach((theme) => {
    document.documentElement.classList.remove(theme)
  })
}

/**
 * Applies a theme class to the document element
 * @param theme - The theme to apply
 */
export function applyThemeClass(theme: Theme): void {
  if (typeof document === 'undefined') {
    return
  }
  
  removeAllThemeClasses()
  document.documentElement.classList.add(theme)
}

