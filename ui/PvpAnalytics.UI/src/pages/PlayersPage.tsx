import { useEffect, useState, useMemo, type ReactNode } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'
import SearchBar from '../components/SearchBar/SearchBar'
import ExportButton from '../components/ExportButton/ExportButton'
import { getWoWClassColors, getFactionColors, getErrorStyles } from '../utils/themeColors'
import type { Player } from '../types/api'

const PlayersPage = () => {
  const navigate = useNavigate()
  const [players, setPlayers] = useState<Player[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [searchParams, setSearchParams] = useSearchParams()
  const [searchTerm, setSearchTerm] = useState(searchParams.get('search') || '')

  // Sync searchTerm to URL search params
  useEffect(() => {
    if (searchTerm.trim()) {
      setSearchParams({ search: searchTerm }, { replace: true })
    } else {
      setSearchParams({}, { replace: true })
    }
  }, [searchTerm, setSearchParams])

  useEffect(() => {
    const abortController = new AbortController()

    const loadPlayers = async () => {
      setLoading(true)
      setError(null)
      try {
        const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
        const { data } = await axios.get<Player[]>(`${baseUrl}/players`, {
          signal: abortController.signal,
        })
        if (abortController.signal.aborted) return
        setPlayers(data)
      } catch (err) {
        if (abortController.signal.aborted) return
        if (axios.isCancel(err) || (err as Error).name === 'CanceledError') return
        
        console.error('Failed to load players', err)
        setError('Failed to load players. Please try again later.')
      } finally {
        if (!abortController.signal.aborted) {
          setLoading(false)
        }
      }
    }

    loadPlayers()

    return () => {
      abortController.abort()
    }
  }, [])

  const filteredPlayers = useMemo(() => {
    if (!searchTerm.trim()) return players
    const term = searchTerm.toLowerCase()
    return players.filter(
      (p) =>
        p.name.toLowerCase().includes(term) ||
        p.realm.toLowerCase().includes(term) ||
        p.class?.toLowerCase().includes(term) ||
        p.faction?.toLowerCase().includes(term)
    )
  }, [players, searchTerm])

  const getClassColor = (className: string) => {
    const colors = getWoWClassColors(className)
    return `${colors.bg} ${colors.text}`
  }

  const getFactionColor = (faction: string) => {
    const colors = getFactionColors(faction)
    return `${colors.bg} ${colors.text}`
  }

  const playerCountLabel = useMemo(() => {
    const count = filteredPlayers.length
    const suffix = count === 1 ? '' : 's'
    return `${count} player${suffix}`
  }, [filteredPlayers])

  let playersContent: ReactNode
  if (loading) {
    playersContent = <div className="text-center py-12 text-text-muted">Loading players...</div>
  } else if (filteredPlayers.length === 0) {
    playersContent = (
      <div className="text-center py-12 text-text-muted">
        {searchTerm ? 'No players found matching your search.' : 'No players found.'}
      </div>
    )
  } else {
    playersContent = (
      <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-3">
        {filteredPlayers.map((player) => (
          <button
            key={player.id}
            type="button"
            onClick={() => navigate(`/players/${player.id}`)}
            aria-label={`View profile for ${player.name} from ${player.realm}`}
            className="w-full text-left p-4 rounded-xl border border-accent-muted/30 bg-surface/50 hover:bg-surface/70 transition-colors cursor-pointer focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-2 focus:ring-offset-background"
          >
            <div className="flex items-start gap-3">
              <div className="grid h-12 w-12 flex-shrink-0 place-items-center rounded-lg bg-gradient-to-br from-accent to-sky-400 text-lg font-bold text-white">
                {player.name.substring(0, 1).toUpperCase()}
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="font-semibold text-text truncate">{player.name}</h3>
                <p className="text-sm text-text-muted truncate">{player.realm}</p>
                <div className="mt-2 flex flex-wrap gap-2">
                  {player.class && (
                    <span className={`text-xs px-2 py-1 rounded ${getClassColor(player.class)}`}>
                      {player.class}
                    </span>
                  )}
                  {player.faction && (
                    <span className={`text-xs px-2 py-1 rounded ${getFactionColor(player.faction)}`}>
                      {player.faction}
                    </span>
                  )}
                </div>
              </div>
            </div>
          </button>
        ))}
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 sm:grid-cols-1 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)] lg:items-center">
        <SearchBar
          placeholder="Search players by name, realm, class..."
          onSearch={setSearchTerm}
        />
        <div className="flex justify-start lg:justify-end">
          <div className="text-sm text-text-muted">
            {loading ? 'Loading...' : playerCountLabel}
          </div>
        </div>
      </div>

      {error && (
        <div className={`rounded-2xl border px-4 py-3 text-sm ${getErrorStyles()}`}>
          {error}
        </div>
      )}

      <Card
        title={`Players (${filteredPlayers.length})`}
        actions={
          filteredPlayers.length > 0 ? (
            <ExportButton
              data={filteredPlayers as unknown as Record<string, unknown>[]}
              filename={`players-${new Date().toISOString().split('T')[0]}`}
              headers={['id', 'name', 'realm', 'class', 'faction']}
            />
          ) : undefined
        }
      >
        {playersContent}
      </Card>
    </div>
  )
}

export default PlayersPage

