import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useState, useRef, useEffect } from 'react'
import SearchBar from '../SearchBar/SearchBar'
import axios from 'axios'

const Navigation = () => {
  const location = useLocation()
  const navigate = useNavigate()
  const [searchLoading, setSearchLoading] = useState(false)
  const abortControllerRef = useRef<AbortController | null>(null)
  const searchTokenRef = useRef(0)

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort()
        abortControllerRef.current = null
      }
    }
  }, [])

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

    // Abort any previous search
    if (abortControllerRef.current) {
      abortControllerRef.current.abort()
    }

    // Create new abort controller and increment search token
    const abortController = new AbortController()
    abortControllerRef.current = abortController
    const currentToken = ++searchTokenRef.current

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
            signal: abortController.signal,
          })
          
          // Check if this search is still current and not aborted
          if (currentToken !== searchTokenRef.current || abortController.signal.aborted) {
            return
          }
          
          if (match?.id) {
            // Match found - navigate to match detail page
            navigate(`/matches/${trimmedTerm}`)
            return
          }
        } catch (error) {
          // Check if aborted or outdated
          if (abortController.signal.aborted || currentToken !== searchTokenRef.current) {
            return
          }
          // Match not found or error - continue to player lookup
        }
        
        // Try to fetch as player
        try {
          const { data: player } = await axios.get<{ id: number }>(`${baseUrl}/players/${trimmedTerm}`, {
            validateStatus: (status) => status === 200 || status === 404,
            signal: abortController.signal,
          })
          
          // Check if this search is still current and not aborted
          if (currentToken !== searchTokenRef.current || abortController.signal.aborted) {
            return
          }
          
          if (player?.id) {
            // Player found - navigate to player profile page
            navigate(`/players/${trimmedTerm}`)
            return
          }
        } catch (error) {
          // Check if aborted or outdated
          if (abortController.signal.aborted || currentToken !== searchTokenRef.current) {
            return
          }
          // Player not found or error
          console.debug('Player lookup failed:', error)
        }
        
        // Check if this search is still current and not aborted before navigating
        if (currentToken === searchTokenRef.current && !abortController.signal.aborted) {
          // Neither match nor player found - navigate to players page with search query
          navigate(`/players?search=${encodeURIComponent(trimmedTerm)}`)
        }
      } catch (error) {
        // Check if aborted or outdated
        if (abortController.signal.aborted || currentToken !== searchTokenRef.current) {
          return
        }
        console.error('Search error:', error)
        // On error, fallback to players page with search query
        navigate(`/players?search=${encodeURIComponent(trimmedTerm)}`)
      } finally {
        // Only update loading state if this is still the current search
        if (currentToken === searchTokenRef.current && !abortController.signal.aborted) {
          setSearchLoading(false)
        }
      }
    } else {
      // Non-numeric search - navigate to players page with search query
      navigate(`/players?search=${encodeURIComponent(trimmedTerm)}`)
    }
  }

  return (
    <nav className="sticky top-0 z-50 mb-8 flex flex-col gap-4 border-b border-accent-muted/30 bg-background/95 backdrop-blur-sm pb-4 pt-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
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
        <div className="flex-1 w-full sm:max-w-md">
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
