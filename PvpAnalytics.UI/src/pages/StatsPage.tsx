import { useEffect, useMemo, useRef, useState } from 'react'
import { useShallow } from 'zustand/react/shallow'
import Card from '../components/Card/Card'
import SearchBar from '../components/SearchBar/SearchBar'
import ToggleGroup from '../components/ToggleGroup/ToggleGroup'
import Sparkline from '../components/Sparkline/Sparkline'
import WinRateList from '../components/WinRateList/WinRateList'
import MatchesTable from '../components/MatchesTable/MatchesTable'
import MatchHighlight from '../components/MatchHighlight/MatchHighlight'
import { mockPlayerStatistics } from '../mocks/playerStats'
import { useStatsStore } from '../store/statsStore'

const StatsPage = () => {
  const [activeTopTab, setActiveTopTab] = useState('rating')
  const [activeFilter, setActiveFilter] = useState('recent')
  const { data, loading, error } = useStatsStore(
    useShallow((state) => ({
      data: state.data,
      loading: state.loading,
      error: state.error,
    })),
  )
  const loadStats = useStatsStore((state) => state.loadStats)
  const hasRequestedInitialLoad = useRef(false)

  useEffect(() => {
    if (hasRequestedInitialLoad.current) {
      return
    }
    hasRequestedInitialLoad.current = true
    void loadStats()
  }, [loadStats])

  const stats = useMemo(() => data ?? mockPlayerStatistics, [data])

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)] lg:items-center">
        <SearchBar onSearch={(term) => void loadStats(term || undefined)} />
        <div className="flex justify-start lg:justify-end">
          <ToggleGroup
            options={[
              { id: 'rating', label: 'Top Player by Rating' },
              { id: 'dps', label: 'Top by DPS / HPS' },
              { id: 'recent', label: 'Recent Matches' },
            ]}
            activeId={activeTopTab}
            onChange={setActiveTopTab}
          />
        </div>
      </div>

      <section className="flex flex-col gap-6 rounded-3xl border border-accent-muted/30 bg-gradient-to-br from-background/80 to-surface/70 p-6 shadow-inner shadow-black/20 backdrop-blur">
        <div className="flex items-center gap-4">
          <div className="grid h-16 w-16 place-items-center rounded-2xl bg-gradient-to-br from-accent to-sky-400 text-xl font-bold text-white" aria-hidden>
            {stats.player.name.substring(0, 1)}
          </div>
          <div>
            <span className="block text-xs font-semibold uppercase tracking-[0.14em] text-text-muted">
              {stats.player.title}
            </span>
            <h1 className="mt-1 text-3xl font-semibold text-text">{stats.player.name}</h1>
          </div>
        </div>
        <div className="text-left lg:text-right">
          <span className="block text-xs font-semibold uppercase tracking-[0.12em] text-text-muted">Rating</span>
          <span className="mt-1 block text-4xl font-bold text-white">{stats.player.rating}</span>
        </div>
      </section>

      {error && (
        <div className="rounded-2xl border border-rose-400/40 bg-rose-500/10 px-4 py-3 text-sm text-rose-200">
          {error}
        </div>
      )}

      <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3" aria-busy={loading}>
        <Card title="Overview">
          <Sparkline values={stats.overviewTrend} />
        </Card>
        <Card>
          <WinRateList title="Winrate by Bracket" entries={stats.winRateByBracket} />
        </Card>
        <Card>
          <WinRateList title="Winrate by Map" entries={stats.winRateByMap} />
        </Card>
      </div>

      <Card
        title="Matches"
        actions={
          <ToggleGroup
            options={[
              { id: 'recent', label: 'Recent' },
              { id: 'favorites', label: 'Favorites' },
            ]}
            activeId={activeFilter}
            onChange={setActiveFilter}
          />
        }
      >
        <MatchesTable matches={stats.matches} />
      </Card>

      <Card title={`${stats.highlight.mode} • ${stats.highlight.map}`} subtitle={`${stats.highlight.result}`}>
        <MatchHighlight match={stats.highlight} />
      </Card>

      {loading && (
        <div className="rounded-2xl border border-accent-muted/40 border-dashed bg-accent-muted/10 px-4 py-3 text-center text-sm text-text">
          Loading latest data…
        </div>
      )}
    </div>
  )
}

export default StatsPage

