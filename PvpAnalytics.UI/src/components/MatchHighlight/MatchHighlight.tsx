import type { MatchDetail } from '../../types/stats'
import styles from './MatchHighlight.module.css'

interface MatchHighlightProps {
  match: MatchDetail
}

const formatNumber = (value: number) =>
  value >= 1_000_000 ? `${(value / 1_000_000).toFixed(2)} M` : `${Math.round(value / 1000)} K`

const MatchHighlight = ({ match }: MatchHighlightProps) => {
  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <div>
          <h3>{match.mode}</h3>
          <p>
            {match.map} • {match.ratingDelta.toFixed(2)} rating • {match.result}
          </p>
        </div>
        <span className={`${styles.resultBadge} ${match.result === 'Victory' ? styles.victory : styles.defeat}`}>
          {match.result}
        </span>
      </header>
      <div className={styles.body}>
        {match.teams.map((team) => (
          <div key={team.name} className={styles.teamColumn}>
            <h4>{team.name}</h4>
            <div className={styles.playerRows}>
              {team.players.map((player) => (
                <div key={`${team.name}-${player.name}`} className={styles.playerRow}>
                  <div>
                    <span className={styles.playerName}>{player.name}</span>
                    <span className={styles.playerMeta}>
                      {player.specialization} {player.className}
                    </span>
                  </div>
                  <div className={styles.playerStats}>
                    <span>{formatNumber(player.damageDone)}</span>
                    <span>{formatNumber(player.healingDone)}</span>
                    <span>{player.crowdControl.toFixed(1)}s</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
      <footer className={styles.timeline}>
        {match.timeline.map((event) => (
          <div key={event.timestamp} className={styles.timelineItem}>
            <span className={styles.timelineTime}>{(event.timestamp / 60).toFixed(2)}</span>
            <span className={styles.timelineDescription}>{event.description}</span>
          </div>
        ))}
      </footer>
    </div>
  )
}

export default MatchHighlight

