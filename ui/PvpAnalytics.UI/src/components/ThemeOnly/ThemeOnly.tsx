import type { PropsWithChildren } from 'react'
import { useThemeStore } from '../../store/themeStore'
import type { Theme } from '../../config/themeConfig'

interface ThemeOnlyProps extends PropsWithChildren {
  /**
   * Theme(s) in which this component should be visible
   * - 'dark': Only visible in dark theme
   * - 'light': Only visible in light theme
   * - ['dark', 'light']: Visible in both themes (same as not using this component)
   */
  theme: Theme | Theme[]
}

/**
 * Component wrapper that conditionally renders children based on current theme
 * 
 * @example
 * // Only show in dark theme
 * <ThemeOnly theme="dark">
 *   <DarkModeComponent />
 * </ThemeOnly>
 * 
 * @example
 * // Only show in light theme
 * <ThemeOnly theme="light">
 *   <LightModeComponent />
 * </ThemeOnly>
 */
const ThemeOnly = ({ theme, children }: ThemeOnlyProps) => {
  const { theme: currentTheme } = useThemeStore()
  
  const themes = Array.isArray(theme) ? theme : [theme]
  const shouldShow = themes.includes(currentTheme)
  
  if (!shouldShow) {
    return null
  }
  
  return <>{children}</>
}

export default ThemeOnly

