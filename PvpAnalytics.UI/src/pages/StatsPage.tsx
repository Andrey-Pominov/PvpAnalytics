import { useEffect, useMemo, useState } from 'react'
import Card from '../components/Card/Card'
import SearchBar from '../components/SearchBar/SearchBar'
import ToggleGroup from '../components/ToggleGroup/ToggleGroup'
import Sparkline from '../components/Sparkline/Sparkline'
import WinRateList from '../components/WinRateList/WinRateList'
import MatchesTable from '../components/MatchesTable/MatchesTable'
import MatchHighlight from '../components/MatchHighlight/MatchHighlight'
import { mockPlayerStatistics } from '../mocks/playerStats'
import { useStatsStore } from '../store/statsStore'
import styles from './StatsPage.module.css'

const StatsPage = () => {
  const [activeTopTab, setActiveTopTab] = useState('rating')
  const [activeFilter, setActiveFilter] = useState('recent')
  const { data, loading, error } = useStatsStore((state) => ({
    data: state.data,
    loading: state.loading,
    error: state.error,
  }))
  const loadStats = useStatsStore((state) => state.loadStats)

  useEffect(() => {
    void useStatsStore.getState().loadStats()
  }, [])

  const stats = useMemo(() => data ?? mockPlayerStatistics, [data])

  return (
    <div className={styles.page}>
      <div className={styles.heroRow}>
        <SearchBar onSearch={(term) => void loadStats(term || undefined)} />
        <div className={styles.tabColumns}>
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

      <section className={styles.playerSummary}>
        <div className={styles.playerIdentity}>
          <div className={styles.avatarPlaceholder} aria-hidden>
            {stats.player.name.substring(0, 1)}
          </div>
          <div>
            <span className={styles.playerRole}>{stats.player.title}</span>
            <h1 className={styles.playerName}>{stats.player.name}</h1>
          </div>
        </div>
        <div className={styles.playerRating}>
          <span className={styles.ratingLabel}>Rating</span>
          <span className={styles.ratingValue}>{stats.player.rating}</span>
        </div>
      </section>

      {error && <div className={styles.errorBanner}>{error}</div>}

      <div className={styles.gridRow} aria-busy={loading}>
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

      {loading && <div className={styles.loadingOverlay}>Loading latest data…</div>}
    </div>
  )
}

export default StatsPage

