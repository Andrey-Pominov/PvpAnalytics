import { useEffect, useState, useMemo, type ReactNode } from 'react'
import axios from 'axios'
import Card from '../components/Card/Card'
import MatchesTable from '../components/MatchesTable/MatchesTable'
import ExportButton from '../components/ExportButton/ExportButton'
import { getErrorStyles } from '../utils/themeColors'
import type { Match } from '../types/api'
import type { MatchSummary } from '../types/stats'

const MatchesPage = () => {
  const [matches, setMatches] = useState<Match[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState<'all' | 'ranked' | 'unranked'>('all')

  useEffect(() => {
    const abortController = new AbortController()

    const loadMatches = async () => {
      setLoading(true)
      setError(null)
      try {
        const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
        const { data } = await axios.get<Match[]>(`${baseUrl}/matches`, {
          signal: abortController.signal,
        })
        if (abortController.signal.aborted) return
        setMatches(data)
      } catch (err) {
        if (abortController.signal.aborted) return
        if (axios.isCancel(err) || (err as Error).name === 'CanceledError') return
        
        console.error('Failed to load matches', err)
        setError('Failed to load matches. Please try again later.')
      } finally {
        if (!abortController.signal.aborted) {
          setLoading(false)
        }
      }
    }

    loadMatches()

    return () => {
      abortController.abort()
    }
  }, [])

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

  const filteredMatches = useMemo(() => {
    let filtered = matches
    if (filter === 'ranked') {
      filtered = filtered.filter((m) => m.isRanked)
    } else if (filter === 'unranked') {
      filtered = filtered.filter((m) => !m.isRanked)
    }
    return filtered.sort((a, b) => new Date(b.createdOn).getTime() - new Date(a.createdOn).getTime())
  }, [matches, filter])

  const transformedMatches: MatchSummary[] = useMemo(() => {
    return filteredMatches.map((m) => ({
      id: m.id.toString(),
      date: new Date(m.createdOn).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
      }),
      mode: gameModeNames[m.gameMode] || m.gameMode,
      map: arenaZoneNames[m.arenaZone] || 'Unknown',
      // result is optional - only set if available from API data
      duration: formatDuration(m.duration),
    }))
  }, [filteredMatches])

  // Export data without 'result' field to avoid empty column in CSV
  const exportData = useMemo(() => {
    return transformedMatches.map(({ id, date, mode, map, duration }) => ({
      id,
      date,
      mode,
      map,
      duration,
    }))
  }, [transformedMatches])

  const matchCountLabel = useMemo(() => {
    const count = filteredMatches.length
    const suffix = count === 1 ? '' : 'es'
    return `${count} match${suffix}`
  }, [filteredMatches])

  let matchesContent: ReactNode
  if (loading) {
    matchesContent = (
      <div className="text-center py-12 text-text-muted">Loading matches...</div>
    )
  } else if (filteredMatches.length === 0) {
    matchesContent = (
      <div className="text-center py-12 text-text-muted">No matches found.</div>
    )
  } else {
    matchesContent = <MatchesTable matches={transformedMatches} />
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
        <div className="text-sm text-text-muted">
          {loading ? 'Loading...' : matchCountLabel}
        </div>
        <div className="flex flex-wrap items-center gap-3 w-full sm:w-auto">
          <ExportButton data={exportData} filename="matches" disabled={loading || filteredMatches.length === 0} />
          <div className="flex flex-wrap gap-2">
          <button
            onClick={() => setFilter('all')}
            className={`px-4 py-2 rounded-lg text-sm font-semibold transition-colors ${
              filter === 'all'
                ? 'bg-gradient-to-r from-accent to-sky-400 text-white'
                : 'text-text-muted hover:text-text hover:bg-surface/50'
            }`}
          >
            All
          </button>
          <button
            onClick={() => setFilter('ranked')}
            className={`px-4 py-2 rounded-lg text-sm font-semibold transition-colors ${
              filter === 'ranked'
                ? 'bg-gradient-to-r from-accent to-sky-400 text-white'
                : 'text-text-muted hover:text-text hover:bg-surface/50'
            }`}
          >
            Ranked
          </button>
          <button
            onClick={() => setFilter('unranked')}
            className={`px-4 py-2 rounded-lg text-sm font-semibold transition-colors ${
              filter === 'unranked'
                ? 'bg-gradient-to-r from-accent to-sky-400 text-white'
                : 'text-text-muted hover:text-text hover:bg-surface/50'
            }`}
          >
            Unranked
          </button>
          </div>
        </div>
      </div>

      {error && (
        <div className={`rounded-2xl border px-4 py-3 text-sm ${getErrorStyles()}`}>
          {error}
        </div>
      )}

      <Card
        title={`Matches (${filteredMatches.length})`}
        actions={
          filteredMatches.length > 0 ? (
            <ExportButton
              data={exportData as unknown as Record<string, unknown>[]}
              filename={`matches-${new Date().toISOString().split('T')[0]}`}
              headers={['id', 'date', 'mode', 'map', 'duration']}
            />
          ) : undefined
        }
      >
        {matchesContent}
      </Card>
    </div>
  )
}

export default MatchesPage

