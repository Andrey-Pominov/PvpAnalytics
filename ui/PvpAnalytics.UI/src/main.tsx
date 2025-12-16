import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { getDefaultTheme, isValidTheme, applyThemeClass } from './config/themeConfig'

// Initialize theme on app load - use system preference or saved preference
const initializeTheme = () => {
  const saved = localStorage.getItem('theme-preference')
  let theme = getDefaultTheme()
  
  if (saved) {
    try {
      const parsed = JSON.parse(saved)
      const savedTheme = parsed.state?.theme
      if (savedTheme && isValidTheme(savedTheme)) {
        theme = savedTheme
      }
    } catch {
      // If parsing fails, use default theme
    }
  }
  
  applyThemeClass(theme)
}

initializeTheme()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
