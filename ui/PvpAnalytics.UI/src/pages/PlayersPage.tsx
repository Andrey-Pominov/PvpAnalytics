import { useEffect, useState, useMemo } from 'react'
import axios from 'axios'
import Card from '../components/Card/Card'
import SearchBar from '../components/SearchBar/SearchBar'
import type { Player } from '../types/api'

const PlayersPage = () => {
  const [players, setPlayers] = useState<Player[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [searchTerm, setSearchTerm] = useState('')

  useEffect(() => {
    loadPlayers()
  }, [])

  const loadPlayers = async () => {
    setLoading(true)
    setError(null)
    try {
      const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
      const { data } = await axios.get<Player[]>(`${baseUrl}/players`)
      setPlayers(data)
    } catch (err) {
      console.error('Failed to load players', err)
      setError('Failed to load players. Please try again later.')
    } finally {
      setLoading(false)
    }
  }

  const filteredPlayers = useMemo(() => {
    if (!searchTerm.trim()) return players
    const term = searchTerm.toLowerCase()
    return players.filter(
      (p) =>
        p.name.toLowerCase().includes(term) ||
        p.realm.toLowerCase().includes(term) ||
        (p.class && p.class.toLowerCase().includes(term)) ||
        (p.faction && p.faction.toLowerCase().includes(term))
    )
  }, [players, searchTerm])

  const getClassColor = (className: string) => {
    const colors: Record<string, string> = {
      warrior: 'bg-red-500/20 text-red-300',
      paladin: 'bg-pink-500/20 text-pink-300',
      hunter: 'bg-green-500/20 text-green-300',
      rogue: 'bg-yellow-500/20 text-yellow-300',
      priest: 'bg-white/20 text-white',
      shaman: 'bg-blue-500/20 text-blue-300',
      mage: 'bg-cyan-500/20 text-cyan-300',
      warlock: 'bg-purple-500/20 text-purple-300',
      monk: 'bg-teal-500/20 text-teal-300',
      druid: 'bg-orange-500/20 text-orange-300',
      'death knight': 'bg-red-600/20 text-red-400',
      'demon hunter': 'bg-purple-600/20 text-purple-400',
      evoker: 'bg-emerald-500/20 text-emerald-300',
    }
    return colors[className.toLowerCase()] || 'bg-accent/20 text-accent'
  }

  const getFactionColor = (faction: string) => {
    if (faction.toLowerCase().includes('alliance')) {
      return 'bg-blue-500/20 text-blue-300'
    }
    if (faction.toLowerCase().includes('horde')) {
      return 'bg-red-500/20 text-red-300'
    }
    return 'bg-gray-500/20 text-gray-300'
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)] lg:items-center">
        <SearchBar
          placeholder="Search players by name, realm, class..."
          onSearch={setSearchTerm}
        />
        <div className="flex justify-start lg:justify-end">
          <div className="text-sm text-text-muted">
            {loading ? 'Loading...' : `${filteredPlayers.length} player${filteredPlayers.length !== 1 ? 's' : ''}`}
          </div>
        </div>
      </div>

      {error && (
        <div className="rounded-2xl border border-rose-400/40 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">
          {error}
        </div>
      )}

      <Card title={`Players (${filteredPlayers.length})`}>
        {loading ? (
          <div className="text-center py-12 text-text-muted">Loading players...</div>
        ) : filteredPlayers.length === 0 ? (
          <div className="text-center py-12 text-text-muted">
            {searchTerm ? 'No players found matching your search.' : 'No players found.'}
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {filteredPlayers.map((player) => (
              <div
                key={player.id}
                className="p-4 rounded-xl border border-accent-muted/30 bg-surface/50 hover:bg-surface/70 transition-colors"
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
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  )
}

export default PlayersPage

