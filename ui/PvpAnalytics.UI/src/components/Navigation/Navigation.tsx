import { Link, useLocation, useNavigate } from 'react-router-dom'
import SearchBar from '../SearchBar/SearchBar'

const Navigation = () => {
  const location = useLocation()
  const navigate = useNavigate()

  const navItems = [
    { path: '/', label: 'Stats', icon: '📊' },
    { path: '/players', label: 'Players', icon: '👥' },
    { path: '/matches', label: 'Matches', icon: '⚔️' },
    { path: '/upload', label: 'Upload', icon: '📤' },
  ]

  const handleGlobalSearch = (term: string) => {
    if (!term.trim()) {
      // Clear search if empty
      if (location.pathname === '/players') {
        navigate('/players')
      }
      return
    }

    // Try to parse as player ID or match ID
    const numId = parseInt(term, 10)
    if (!isNaN(numId) && numId > 0) {
      // Check if it's a player ID (try navigating to player profile)
      navigate(`/players/${numId}`)
      return
    }

    // Otherwise, search in players page
    navigate(`/players?search=${encodeURIComponent(term)}`)
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
            placeholder="Search player ID, match ID, or name..."
            onSearch={handleGlobalSearch}
          />
        </div>
      </div>
    </nav>
  )
}

export default Navigation

