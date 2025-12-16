import { useState, useRef, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { debouncedSearch, type SearchResponse } from '../../services/searchService'
import { useSearchStore } from '../../store/searchStore'
import { useRecentlyViewedStore } from '../../store/recentlyViewedStore'
import { getWoWClassColor } from '../../utils/themeColors'

interface GlobalSearchProps {
  className?: string
}

const GlobalSearch = ({ className = '' }: GlobalSearchProps) => {
  const navigate = useNavigate()
  const [value, setValue] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const [results, setResults] = useState<SearchResponse>({ players: [], matches: [] })
  const [loading, setLoading] = useState(false)
  const [selectedIndex, setSelectedIndex] = useState(-1)
  
  const { recentSearches, addRecentSearch } = useSearchStore()
  const { items: recentlyViewed } = useRecentlyViewedStore()
  
  const inputRef = useRef<HTMLInputElement>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const containerRef = useRef<HTMLDivElement>(null)

  // Close dropdown on outside click
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        containerRef.current &&
        !containerRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false)
        setSelectedIndex(-1)
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  // Handle search as user types
  useEffect(() => {
    if (value.trim().length >= 2) {
      setLoading(true)
      debouncedSearch(value, (searchResults) => {
        setResults(searchResults)
        setLoading(false)
        setIsOpen(true)
      })
    } else if (value.trim().length === 0 && isOpen) {
      setResults({ players: [], matches: [] })
      setLoading(false)
    }
  }, [value, isOpen])

  // Handle focus - show recent/popular
  const handleFocus = () => {
    setIsOpen(true)
    setSelectedIndex(-1)
  }

  // Handle keyboard navigation
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!isOpen) {
      if (e.key === 'ArrowDown' || e.key === 'Enter') {
        setIsOpen(true)
      }
      return
    }

    // Build list of all items based on current state
    const allItems: Array<{ type: string; id: number | string; name?: string; term?: string }> = []
    
    if (value.trim().length < 2) {
      // Show recent items when input is empty
      allItems.push(
        ...recentSearches.slice(0, 5).map((s) => ({ type: 'recent' as const, id: s.term, term: s.term })),
        ...recentlyViewed.slice(0, 5).map((p) => ({ type: 'recent-player' as const, id: p.id, name: p.name }))
      )
    } else {
      // Show search results when typing
      allItems.push(
        ...results.players.map((p) => ({ type: 'player' as const, id: p.id, name: p.name })),
        ...results.matches.map((m) => ({ type: 'match' as const, id: m.id }))
      )
    }

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault()
        setSelectedIndex((prev) => (prev < allItems.length - 1 ? prev + 1 : prev))
        break
      case 'ArrowUp':
        e.preventDefault()
        setSelectedIndex((prev) => (prev > 0 ? prev - 1 : -1))
        break
      case 'Enter':
        e.preventDefault()
        if (selectedIndex >= 0 && selectedIndex < allItems.length) {
          handleSelectItem(allItems[selectedIndex])
        } else if (value.trim().length >= 2) {
          handleSearch(value.trim())
        }
        break
      case 'Escape':
        setIsOpen(false)
        setSelectedIndex(-1)
        inputRef.current?.blur()
        break
    }
  }

  const handleSelectItem = (item: { type: string; id: number | string; name?: string; term?: string }) => {
    if (item.type === 'player' || item.type === 'recent-player') {
      addRecentSearch(item.name || String(item.id), 'player')
      navigate(`/players/${item.id}`)
    } else if (item.type === 'match') {
      addRecentSearch(String(item.id), 'match')
      navigate(`/matches/${item.id}`)
    } else if (item.type === 'recent' && item.term) {
      setValue(item.term)
      handleSearch(item.term)
    }
    setIsOpen(false)
    setValue('')
    inputRef.current?.blur()
  }

  const handleSearch = (term: string) => {
    if (!term.trim()) return
    
    // Check if it's a numeric ID (match or player)
    const numericId = Number.parseInt(term.trim(), 10)
    if (!Number.isNaN(numericId)) {
      // Try match first, then player
      navigate(`/matches/${numericId}`)
      return
    }

    // Otherwise navigate to players page with search
    addRecentSearch(term, 'general')
    navigate(`/players?search=${encodeURIComponent(term)}`)
    setIsOpen(false)
    setValue('')
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (value.trim().length >= 2) {
      handleSearch(value.trim())
    }
  }


  const hasResults = results.players.length > 0 || results.matches.length > 0
  const hasRecentItems = recentSearches.length > 0 || recentlyViewed.length > 0
  const showDropdown = isOpen && (value.trim().length >= 2 ? hasResults || loading : hasRecentItems || value.trim().length === 0)

  return (
    <div ref={containerRef} className={`relative w-full ${className}`}>
      <form onSubmit={handleSubmit} className="relative">
        <input
          ref={inputRef}
          type="text"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onFocus={handleFocus}
          onKeyDown={handleKeyDown}
          placeholder="Search players, matches..."
          className="search-input w-full rounded-2xl border px-4 py-3 pl-10 pr-4 text-sm text-text placeholder:text-text-muted/70 focus:border-accent focus:outline-none focus:ring-2 focus:ring-accent/60 transition-colors"
        />
        <svg
          className="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-text-muted/70"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        {value && (
          <button
            type="button"
            onClick={() => {
              setValue('')
              setIsOpen(false)
              inputRef.current?.focus()
            }}
            className="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted/70 hover:text-text"
          >
            <svg className="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </form>

      {/* Dropdown */}
      {showDropdown && (
        <div
          ref={dropdownRef}
          className="dropdown-menu absolute left-0 right-0 top-full z-50 mt-2 max-h-96 overflow-y-auto rounded-lg border backdrop-blur-sm"
        >
          {value.trim().length < 2 ? (
            /* Recent/Popular Items */
            <div className="p-2">
                  {recentSearches.length > 0 && (
                <div className="mb-2">
                  <div className="px-3 py-2 text-xs font-semibold text-text-muted">Recent Searches</div>
                  {recentSearches.slice(0, 5).map((search, idx) => (
                    <button
                      key={`${search.term}-${search.timestamp}`}
                      type="button"
                      onClick={() => handleSelectItem({ type: 'recent', id: search.term, term: search.term })}
                      className={`menu-item w-full rounded px-3 py-2 text-left text-sm ${
                        selectedIndex === idx
                          ? 'active text-accent'
                          : 'text-text-muted hover:text-text'
                      }`}
                    >
                      <div className="flex items-center gap-2">
                        <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                        </svg>
                        <span>{search.term}</span>
                      </div>
                    </button>
                  ))}
                </div>
              )}
              {recentlyViewed.length > 0 && (
                <div>
                  <div className="px-3 py-2 text-xs font-semibold text-text-muted">Recently Viewed</div>
                  {recentlyViewed.slice(0, 5).map((player, idx) => {
                    const itemIndex = Math.min(recentSearches.length, 5) + idx
                    return (
                      <button
                        key={player.id}
                        type="button"
                        onClick={() => handleSelectItem({ type: 'recent-player', id: player.id, name: player.name })}
                        className={`menu-item w-full rounded px-3 py-2 text-left text-sm ${
                          selectedIndex === itemIndex
                            ? 'active text-accent'
                            : 'text-text-muted hover:text-text'
                        }`}
                      >
                      <div className="flex items-center gap-2">
                        <div className="grid h-6 w-6 place-items-center rounded bg-gradient-to-br from-accent to-sky-400 text-xs font-bold text-white">
                          {player.name.substring(0, 1).toUpperCase()}
                        </div>
                        <span className={getWoWClassColor(player.class)}>{player.name}</span>
                        <span className="text-xs text-text-muted">{player.realm}</span>
                      </div>
                    </button>
                    )
                  })}
                </div>
              )}
              {!hasRecentItems && (
                <div className="px-3 py-4 text-center text-sm text-text-muted">
                  Start typing to search players and matches
                </div>
              )}
            </div>
          ) : loading ? (
            <div className="px-3 py-4 text-center text-sm text-text-muted">Searching...</div>
          ) : hasResults ? (
            /* Search Results */
            <div className="p-2">
              {results.players.length > 0 && (
                <div className="mb-2">
                  <div className="px-3 py-2 text-xs font-semibold text-text-muted">Players</div>
                  {results.players.map((player, idx) => {
                    const itemIndex = idx
                    return (
                      <button
                        key={player.id}
                        type="button"
                        onClick={() => handleSelectItem({ type: 'player', id: player.id, name: player.name })}
                        className={`menu-item w-full rounded px-3 py-2 text-left text-sm ${
                          selectedIndex === itemIndex
                            ? 'active text-accent'
                            : 'text-text-muted hover:text-text'
                        }`}
                      >
                        <div className="flex items-center gap-2">
                          <div className="grid h-6 w-6 place-items-center rounded bg-gradient-to-br from-accent to-sky-400 text-xs font-bold text-white">
                            {player.name.substring(0, 1).toUpperCase()}
                          </div>
                          <div className="flex-1 min-w-0">
                            <div className={`truncate ${getWoWClassColor(player.class)}`}>{player.name}</div>
                            <div className="text-xs text-text-muted">{player.realm}</div>
                          </div>
                        </div>
                      </button>
                    )
                  })}
                </div>
              )}
              {results.matches.length > 0 && (
                <div>
                  <div className="px-3 py-2 text-xs font-semibold text-text-muted">Matches</div>
                  {results.matches.map((match, idx) => {
                    const itemIndex = results.players.length + idx
                    return (
                      <button
                        key={match.id}
                        type="button"
                        onClick={() => handleSelectItem({ type: 'match', id: match.id })}
                        className={`menu-item w-full rounded px-3 py-2 text-left text-sm ${
                          selectedIndex === itemIndex
                            ? 'active text-accent'
                            : 'text-text-muted hover:text-text'
                        }`}
                      >
                        <div className="flex items-center gap-2">
                          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                          </svg>
                          <div className="flex-1 min-w-0">
                            <div className="truncate">Match #{match.id}</div>
                            <div className="text-xs text-text-muted">{match.gameMode}</div>
                          </div>
                        </div>
                      </button>
                    )
                  })}
                </div>
              )}
            </div>
          ) : (
            <div className="px-3 py-4 text-center text-sm text-text-muted">No results found</div>
          )}
        </div>
      )}
    </div>
  )
}

export default GlobalSearch

