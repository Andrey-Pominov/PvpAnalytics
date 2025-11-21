import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import SearchBar from '../SearchBar/SearchBar'
import axios from 'axios'

const Navigation = () => {
  const location = useLocation()
  const navigate = useNavigate()
  const [searchLoading, setSearchLoading] = useState(false)

  const navItems = [
    { path: '/', label: 'Stats', icon: '📊' },
    { path: '/players', label: 'Players', icon: '👥' },
    { path: '/matches', label: 'Matches', icon: '⚔️' },
    { path: '/upload', label: 'Upload', icon: '📤' },
  ]

  // Handle search: distinguish between player ID and match ID
  // Numeric IDs are checked as match first, then player; non-numeric searches go to players page
  const handleSearch = async (term: string) => {
    if (!term.trim()) {
      // Clear search if empty
      if (location.pathname === '/players') {
        navigate('/players')
      }
      return
    }

    const trimmedTerm = term.trim()
    const isNumericId = /^\d+$/.test(trimmedTerm)
    
    if (isNumericId) {
      setSearchLoading(true)
      try {
        const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
        
        // Try to fetch as match first (matches are typically searched by numeric ID)
        try {
          const { data: match } = await axios.get<{ id: number }>(`${baseUrl}/matches/${trimmedTerm}`, {
            validateStatus: (status) => status === 200 || status === 404,
          })
          
          if (match?.id) {
            // Match found - navigate to match detail page
            navigate(`/matches/${trimmedTerm}`)
            return
          }
        } catch {
          // Match not found or error - continue to player lookup
        }
        
        // Try to fetch as player
        try {
          const { data: player } = await axios.get<{ id: number }>(`${baseUrl}/players/${trimmedTerm}`, {
            validateStatus: (status) => status === 200 || status === 404,
          })
          
          if (player?.id) {
            // Player found - navigate to player profile page
            navigate(`/players/${trimmedTerm}`)
            return
          }
        } catch {
          // Player not found or error
        }
        
        // Neither match nor player found - navigate to players page with search query
        navigate(`/players?search=${encodeURIComponent(trimmedTerm)}`)
      } catch (error) {
        console.error('Search error:', error)
        // On error, fallback to players page with search query
        navigate(`/players?search=${encodeURIComponent(trimmedTerm)}`)
      } finally {
        setSearchLoading(false)
      }
    } else {
      // Non-numeric search - navigate to players page with search query
      navigate(`/players?search=${encodeURIComponent(trimmedTerm)}`)
    }
  }

  return (
    <nav className="sticky top-0 z-50 mb-8 flex flex-col gap-4 border-b border-accent-muted/30 bg-background/95 backdrop-blur-sm pb-4 pt-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-wrap items-center gap-2">
          {navItems.map((item) => {
            const isActive = location.pathname === item.path || (item.path === '/' && location.pathname === '/')
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold transition-colors ${
                  isActive
                    ? 'bg-gradient-to-r from-accent to-sky-400 text-white shadow-lg'
                    : 'text-text-muted hover:text-text hover:bg-surface/50'
                }`}
              >
                <span>{item.icon}</span>
                <span>{item.label}</span>
              </Link>
            )
          })}
        </div>
        <div className="flex-1 max-w-md">
          <SearchBar
            placeholder={searchLoading ? 'Searching...' : 'Search player ID, match ID, or name...'}
            onSearch={handleSearch}
          />
        </div>
      </div>
    </nav>
  )
}

export default Navigation

