import { useEffect, useState, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'
import ExportButton from '../components/ExportButton/ExportButton'
import AnomalyBadge from '../components/AnomalyBadge/AnomalyBadge'
import ForecastCard from '../components/ForecastCard/ForecastCard'
import type { Player, PlayerStats, PlayerMatch } from '../types/api'
import { mockPlayerStats, mockPlayerMatches } from '../mocks/playerStats'
import { mockPlayers } from '../mocks/players'
import { detectWinRateAnomaly, detectAnomaliesInData, generateForecast } from '../utils/statisticsUtils'

const PlayerProfilePage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [player, setPlayer] = useState<Player | null>(null)
  const [stats, setStats] = useState<PlayerStats | null>(null)
  const [matches, setMatches] = useState<PlayerMatch[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return

    const abortController = new AbortController()
    const playerId = parseInt(id, 10)

    const loadPlayerData = async () => {
      setLoading(true)
      setError(null)
      try {
        const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'

        if (baseUrl === 'mock') {
          await new Promise((resolve) => setTimeout(resolve, 500))
          if (abortController.signal.aborted) return

          const mockPlayer = mockPlayers.find((p) => p.id === playerId)
          if (!mockPlayer) {
            setError('Player not found')
            return
          }

          setPlayer(mockPlayer)
          setStats(mockPlayerStats[playerId] || null)
          setMatches(mockPlayerMatches[playerId] || [])
          return
        }

        // Fetch player
        const playerResponse = await axios.get<Player>(`${baseUrl}/players/${playerId}`, {
          signal: abortController.signal,
        })
        if (abortController.signal.aborted) return
        setPlayer(playerResponse.data)

        // Fetch stats
        try {
          const statsResponse = await axios.get<PlayerStats>(`${baseUrl}/players/${playerId}/stats`, {
            signal: abortController.signal,
          })
          if (!abortController.signal.aborted) {
            setStats(statsResponse.data)
          }
        } catch (err) {
          if (!abortController.signal.aborted && !axios.isCancel(err)) {
            console.warn('Failed to load player stats', err)
            // Use mock stats as fallback
            setStats(mockPlayerStats[playerId] || null)
          }
        }

        // Fetch matches
        try {
          const matchesResponse = await axios.get<PlayerMatch[]>(`${baseUrl}/players/${playerId}/matches`, {
            signal: abortController.signal,
          })
          if (!abortController.signal.aborted) {
            setMatches(matchesResponse.data)
          }
        } catch (err) {
          if (!abortController.signal.aborted && !axios.isCancel(err)) {
            console.warn('Failed to load player matches', err)
            // Use mock matches as fallback
            setMatches(mockPlayerMatches[playerId] || [])
          }
        }
      } catch (err) {
        if (abortController.signal.aborted) return
        if (axios.isCancel(err) || (err as Error).name === 'CanceledError') return

        if (axios.isAxiosError(err) && err.response?.status === 404) {
          setError('Player not found')
        } else {
          console.error('Failed to load player data', err)
          setError('Failed to load player data. Please try again later.')
        }
      } finally {
        if (!abortController.signal.aborted) {
          setLoading(false)
        }
      }
    }

    loadPlayerData()

    return () => {
      abortController.abort()
    }
  }, [id])

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

  const gameModeNames: Record<string, string> = {
    TwoVsTwo: '2v2',
    ThreeVsThree: '3v3',
    Skirmish: 'Skirmish',
    Rbg: 'RBG',
    Shuffle: 'Solo Shuffle',
  }

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins}:${secs.toString().padStart(2, '0')}`
  }

  // Detect win rate anomaly
  const winRateAnomaly = useMemo(() => {
    if (!stats || stats.totalMatches === 0) return null
    const winRateDecimal = stats.winRate / 100
    return detectWinRateAnomaly(winRateDecimal, stats.totalMatches)
  }, [stats])

  // Detect rating anomalies in match history
  const ratingAnomalies = useMemo(() => {
    if (matches.length < 5) return []
    return detectAnomaliesInData(matches, (m) => m.ratingAfter)
  }, [matches])

  // Generate rating forecast from match history
  const ratingForecast = useMemo(() => {
    if (matches.length < 5) return null
    // Sort matches by date (oldest first) and extract ratings
    const sortedMatches = [...matches].sort(
      (a, b) => new Date(a.createdOn).getTime() - new Date(b.createdOn).getTime()
    )
    const ratings = sortedMatches.map((m) => m.ratingAfter)
    const currentRating = ratings[ratings.length - 1]
    // Target: next rating milestone
    const nextMilestone = Math.ceil(currentRating / 100) * 100
    return generateForecast(ratings, 7, nextMilestone)
  }, [matches])

  if (loading) {
    return (
      <div className="flex flex-col gap-6">
        <div className="text-center py-12 text-text-muted">Loading player profile...</div>
      </div>
    )
  }

  if (error || !player) {
    return (
      <div className="flex flex-col gap-6">
        <div className="rounded-2xl border border-rose-400/40 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">
          {error || 'Player not found'}
        </div>
        <button
          onClick={() => navigate('/players')}
          className="w-fit rounded-xl bg-gradient-to-r from-accent to-sky-400 px-6 py-3 text-sm font-semibold text-white transition-all hover:shadow-lg"
        >
          Back to Players
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6">
      {/* Header with Back Button */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate('/players')}
          className="rounded-lg px-3 py-2 text-sm text-text-muted hover:bg-surface/50 hover:text-text transition-colors"
        >
          ← Back to Players
        </button>
      </div>

      {/* Player Header */}
      <Card>
        <div className="flex flex-col sm:flex-row items-start gap-4 sm:gap-6">
          <div className="grid h-16 w-16 sm:h-20 sm:w-20 flex-shrink-0 place-items-center rounded-xl bg-gradient-to-br from-accent to-sky-400 text-xl sm:text-2xl font-bold text-white">
            {player.name.substring(0, 1).toUpperCase()}
          </div>
          <div className="flex-1 min-w-0 w-full sm:w-auto">
            <h1 className="text-xl sm:text-2xl font-bold text-text truncate">{player.name}</h1>
            <p className="mt-1 text-base sm:text-lg text-text-muted">{player.realm}</p>
            <div className="mt-3 flex flex-wrap gap-2">
              {player.class && (
                <span className={`text-xs sm:text-sm px-2 sm:px-3 py-1 rounded ${getClassColor(player.class)}`}>
                  {player.class}
                </span>
              )}
              {player.faction && (
                <span className={`text-xs sm:text-sm px-2 sm:px-3 py-1 rounded ${getFactionColor(player.faction)}`}>
                  {player.faction}
                </span>
              )}
            </div>
          </div>
        </div>
      </Card>

      {/* Statistics Cards */}
      {stats && (
        <div className="grid gap-4 grid-cols-1 sm:grid-cols-2 lg:grid-cols-4">
          {ratingForecast && (
            <div className="md:col-span-2 lg:col-span-4">
              <ForecastCard
                forecast={ratingForecast}
                title="Rating Forecast"
                currentValue={matches.length > 0 ? matches[matches.length - 1].ratingAfter : 0}
              />
            </div>
          )}
          <Card className="p-4">
            <div className="flex items-center justify-between">
              <div className="text-sm text-text-muted">Win Rate</div>
              {winRateAnomaly && <AnomalyBadge anomaly={winRateAnomaly} />}
            </div>
            <div className="mt-1 text-2xl font-bold text-text">{stats.winRate.toFixed(1)}%</div>
            <div className="mt-1 text-xs text-text-muted">
              {stats.wins}W - {stats.losses}L
            </div>
          </Card>
          <Card className="p-4">
            <div className="text-sm text-text-muted">Total Matches</div>
            <div className="mt-1 text-2xl font-bold text-text">{stats.totalMatches}</div>
          </Card>
          <Card className="p-4">
            <div className="text-sm text-text-muted">Avg. Duration</div>
            <div className="mt-1 text-2xl font-bold text-text">
              {formatDuration(Math.round(stats.averageMatchDuration))}
            </div>
          </Card>
          <Card className="p-4">
            <div className="text-sm text-text-muted">Favorite Mode</div>
            <div className="mt-1 text-xl font-semibold text-text">
              {stats.favoriteGameMode ? gameModeNames[stats.favoriteGameMode] || stats.favoriteGameMode : 'N/A'}
            </div>
            {stats.favoriteSpec && (
              <div className="mt-1 text-xs text-text-muted">as {stats.favoriteSpec}</div>
            )}
          </Card>
        </div>
      )}

      {/* Match History */}
      <Card
        title="Match History"
        actions={
          matches.length > 0 ? (
            <ExportButton
              data={matches.map((m) => ({
                matchId: m.matchId,
                date: new Date(m.createdOn).toLocaleDateString('en-US', {
                  month: 'short',
                  day: 'numeric',
                  year: 'numeric',
                }),
                mode: gameModeNames[m.gameMode] || m.gameMode,
                map: m.mapName,
                result: m.isWinner ? 'Victory' : 'Defeat',
                ratingBefore: m.ratingBefore,
                ratingAfter: m.ratingAfter,
                ratingChange: m.ratingAfter - m.ratingBefore,
                duration: formatDuration(m.duration),
                isRanked: m.isRanked,
                spec: m.spec || 'N/A',
              }))}
              filename={`player-${player.name}-matches-${new Date().toISOString().split('T')[0]}`}
            />
          ) : undefined
        }
      >
        {matches.length === 0 ? (
          <div className="text-center py-12 text-text-muted">No matches found for this player.</div>
        ) : (
          <div className="overflow-x-auto -mx-6 sm:mx-0">
            <table className="min-w-full sm:min-w-[560px] w-full border-collapse text-xs sm:text-sm text-text">
              <thead className="text-xs font-semibold uppercase tracking-[0.08em] text-text-muted/70">
                <tr>
                  <th className="px-3 py-3 text-left">Date</th>
                  <th className="px-3 py-3 text-left">Mode</th>
                  <th className="px-3 py-3 text-left">Map</th>
                  <th className="px-3 py-3 text-left">Result</th>
                  <th className="px-3 py-3 text-left">Rating</th>
                  <th className="px-3 py-3 text-left">Duration</th>
                </tr>
              </thead>
              <tbody>
                {matches.map((match) => (
                  <tr
                    key={match.matchId}
                    onClick={() => navigate(`/matches/${match.matchId}`)}
                    className="border-t border-white/10 transition-colors hover:bg-white/5 cursor-pointer"
                  >
                    <td className="px-3 py-3">
                      {new Date(match.createdOn).toLocaleDateString('en-US', {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                      })}
                    </td>
                    <td className="px-3 py-3">{gameModeNames[match.gameMode] || match.gameMode}</td>
                    <td className="px-3 py-3">{match.mapName}</td>
                    <td className="px-3 py-3">
                      <span
                        className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${
                          match.isWinner
                            ? 'bg-emerald-500/20 text-emerald-200'
                            : 'bg-rose-500/20 text-rose-200'
                        }`}
                      >
                        {match.isWinner ? 'Victory' : 'Defeat'}
                      </span>
                    </td>
                    <td className="px-3 py-3">
                      <div className="flex items-center gap-2">
                        <span className="text-text-muted">{match.ratingBefore}</span>
                        <span className={match.ratingAfter > match.ratingBefore ? 'text-emerald-300' : 'text-rose-300'}>
                          →
                        </span>
                        <span>{match.ratingAfter}</span>
                        {ratingAnomalies.find((a) => a.value.matchId === match.matchId)?.isAnomaly && (
                          <AnomalyBadge
                            anomaly={ratingAnomalies.find((a) => a.value.matchId === match.matchId)!}
                            label=""
                          />
                        )}
                      </div>
                    </td>
                    <td className="px-3 py-3">{formatDuration(match.duration)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  )
}

export default PlayerProfilePage

