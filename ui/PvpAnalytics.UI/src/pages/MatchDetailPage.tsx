import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'
import Timeline from '../components/Timeline/Timeline'
import type { MatchDetailDto } from '../types/api'

const MatchDetailPage = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [matchDetail, setMatchDetail] = useState<MatchDetailDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<'overview' | 'timeline'>('overview')

  useEffect(() => {
    if (!id) return

    const abortController = new AbortController()
    const matchId = Number.parseInt(id, 10)

    const loadMatchDetail = async () => {
      setLoading(true)
      setError(null)

      try {
        await loadApiData(abortController, matchId)
      } catch (err) {
        handleLoadError(err, abortController, matchId)
      } finally {
        finalizeLoading(abortController)
      }
    }

    const loadApiData = async (
      abortController: AbortController,
      matchId: number
    ): Promise<void> => {
      const baseUrl = getBaseUrl()
      const { data } = await axios.get<MatchDetailDto>(`${baseUrl}/matches/${matchId}/detail`, {
        signal: abortController.signal,
      })
      
      if (abortController.signal.aborted) return
      setMatchDetail(data)
    }

    const handleLoadError = (
      err: unknown,
      abortController: AbortController,
      matchId: number
    ): void => {
      if (abortController.signal.aborted) return
      if (isCanceledError(err)) return

      if (isNotFoundError(err)) {
        handleNotFoundError(matchId)
      } else {
        handleGenericError(err, matchId)
      }
    }

    const isCanceledError = (err: unknown): boolean => {
      if (axios.isCancel(err)) {
        return true
      }
      if (err instanceof Error && err.name === 'CanceledError') {
        return true
      }
      if (typeof err === 'object' && err !== null && 'name' in err && (err as any).name === 'CanceledError') {
        return true
      }
      return false
    }

    const isNotFoundError = (err: unknown): boolean => {
      return axios.isAxiosError(err) && err.response?.status === 404
    }

    const handleNotFoundError = (matchId: number): void => {
      console.warn(`Match ${matchId} not found`)
      setError('Match not found')
    }

    const handleGenericError = (err: unknown, matchId: number): void => {
      console.error('Failed to load match detail', matchId, err)
      setError('Failed to load match detail. Please try again later.')
    }

    const finalizeLoading = (abortController: AbortController): void => {
      if (!abortController.signal.aborted) {
        setLoading(false)
      }
    }

    const getBaseUrl = (): string => {
      return import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    }

    loadMatchDetail()

    return () => {
      abortController.abort()
    }
  }, [id])

  const formatDuration = (seconds: number): string => {
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins}:${secs.toString().padStart(2, '0')}`
  }

  const gameModeNames: Record<string, string> = {
    TwoVsTwo: '2v2',
    ThreeVsThree: '3v3',
    Skirmish: 'Skirmish',
    Rbg: 'RBG',
    Shuffle: 'Solo Shuffle',
  }

  const arenaZoneNames: Record<number, string> = {
    0: 'Unknown',
    2759: 'Blood Ring',
    617: 'Dalaran Arena',
    618: 'Ring of Valor',
    572: 'Ruins of Lordaeron',
    559: 'Nagrand Arena',
    6178: 'Mugambala',
    1505: "The Tiger's Peak",
    1504: "Tol'viron Arena",
    1825: 'Black Rook Hold Arena',
    3963: 'Maldraxxus Coliseum',
  }

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

  if (loading) {
    return (
      <div className="flex flex-col gap-6">
        <div className="text-center py-12 text-text-muted">Loading match details...</div>
      </div>
    )
  }

  if (error || !matchDetail) {
    return (
      <div className="flex flex-col gap-6">
        <div className="rounded-2xl border border-rose-400/40 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">
          {error || 'Match not found'}
        </div>
        <button
          onClick={() => navigate(-1)}
          className="w-fit rounded-xl bg-gradient-to-r from-accent to-sky-400 px-6 py-3 text-sm font-semibold text-white transition-all hover:shadow-lg"
        >
          Go Back
        </button>
      </div>
    )
  }

  const { basicInfo, teams } = matchDetail
  const winningTeam = teams.find((t) => t.isWinner)
  // const losingTeam = teams.find((t) => !t.isWinner)

  return (
    <div className="flex flex-col gap-6">
      {/* Header with Back Button */}
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate(-1)}
          className="rounded-lg px-3 py-2 text-sm text-text-muted hover:bg-surface/50 hover:text-text transition-colors"
        >
          ← Back
        </button>
      </div>

      {/* Match Header */}
      <Card>
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-text">
              {arenaZoneNames[basicInfo.arenaZone] || 'Unknown Arena'}
            </h1>
            <div className="mt-2 flex flex-wrap items-center gap-4 text-sm text-text-muted">
              <span>{gameModeNames[basicInfo.gameMode] || basicInfo.gameMode}</span>
              <span>•</span>
              <span>{formatDuration(basicInfo.duration)}</span>
              <span>•</span>
              <span>{new Date(basicInfo.createdOn).toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit',
              })}</span>
              {basicInfo.isRanked && (
                <>
                  <span>•</span>
                  <span className="rounded-full bg-emerald-500/20 px-2 py-1 text-xs font-semibold text-emerald-200">
                    Ranked
                  </span>
                </>
              )}
            </div>
          </div>
          {winningTeam && (
            <div className="rounded-lg bg-emerald-500/20 px-4 py-2">
              <div className="text-xs font-semibold uppercase text-emerald-200">Winner</div>
              <div className="text-lg font-bold text-emerald-100">{winningTeam.teamName}</div>
            </div>
          )}
        </div>
      </Card>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-accent-muted/40">
        <button
          onClick={() => setActiveTab('overview')}
          className={`px-4 py-2 text-sm font-semibold transition-colors ${
            activeTab === 'overview'
              ? 'border-b-2 border-accent text-text'
              : 'text-text-muted hover:text-text'
          }`}
        >
          Overview
        </button>
        <button
          onClick={() => setActiveTab('timeline')}
          className={`px-4 py-2 text-sm font-semibold transition-colors ${
            activeTab === 'timeline'
              ? 'border-b-2 border-accent text-text'
              : 'text-text-muted hover:text-text'
          }`}
        >
          Timeline
        </button>
      </div>

      {/* Overview Tab */}
      {activeTab === 'overview' && (
        <div className="flex flex-col gap-6">
          {/* Team Comparison */}
          <div className="grid gap-6 grid-cols-1 sm:grid-cols-2">
            {teams.map((team) => (
              <Card key={team.teamName} title={team.teamName}>
                <div className="space-y-4">
                  {/* Team Stats */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 rounded-lg bg-surface/50 p-4">
                    <div>
                      <div className="text-xs text-text-muted">Total Damage</div>
                      <div className="text-lg font-bold text-red-300">{team.totalDamage.toLocaleString()}</div>
                    </div>
                    <div>
                      <div className="text-xs text-text-muted">Total Healing</div>
                      <div className="text-lg font-bold text-green-300">{team.totalHealing.toLocaleString()}</div>
                    </div>
                  </div>

                  {/* Participants */}
                  <div className="space-y-3">
                    {team.participants.map((participant) => (
                      <div
                        key={participant.playerId}
                        className="rounded-lg border border-accent-muted/40 bg-surface/50 p-4"
                      >
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-2">
                              <h3 className="font-semibold text-text">{participant.playerName}</h3>
                              <span className="text-sm text-text-muted">{participant.realm}</span>
                              {participant.isWinner && (
                                <span className="rounded-full bg-emerald-500/20 px-2 py-1 text-xs font-semibold text-emerald-200">
                                  Winner
                                </span>
                              )}
                            </div>
                            <div className="mt-2 flex flex-wrap gap-2">
                              <span className={`text-xs px-2 py-1 rounded ${getClassColor(participant.class)}`}>
                                {participant.class}
                              </span>
                              {participant.spec && (
                                <span className="text-xs px-2 py-1 rounded bg-accent/20 text-accent">
                                  {participant.spec}
                                </span>
                              )}
                            </div>
                          </div>
                          <div className="text-right">
                            <div className="text-xs text-text-muted">Rating</div>
                            <div className="flex items-center gap-1 text-sm">
                              <span className="text-text-muted">{participant.ratingBefore}</span>
                              {(() => {
                                let ratingColor = 'text-text-muted'
                                if (participant.ratingAfter > participant.ratingBefore) {
                                  ratingColor = 'text-emerald-300'
                                } else if (participant.ratingAfter < participant.ratingBefore) {
                                  ratingColor = 'text-rose-300'
                                }

                                return (
                                  <span className={ratingColor}>
                                    →
                                  </span>
                                )
                              })()}
                              <span className="font-semibold text-text">{participant.ratingAfter}</span>
                            </div>
                          </div>
                        </div>
                        <div className="mt-3 grid grid-cols-3 gap-2 text-xs">
                          <div>
                            <div className="text-text-muted">Damage</div>
                            <div className="font-semibold text-red-300">{participant.totalDamage.toLocaleString()}</div>
                          </div>
                          <div>
                            <div className="text-text-muted">Healing</div>
                            <div className="font-semibold text-green-300">
                              {participant.totalHealing.toLocaleString()}
                            </div>
                          </div>
                          <div>
                            <div className="text-text-muted">CC</div>
                            <div className="font-semibold text-purple-300">{participant.totalCC}</div>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </Card>
            ))}
          </div>
        </div>
      )}

      {/* Timeline Tab */}
      {activeTab === 'timeline' && (
        <Card title="Match Timeline">
          <Timeline events={matchDetail.timelineEvents} />
        </Card>
      )}
    </div>
  )
}

export default MatchDetailPage

