import type { MatchSummary } from '../../types/stats'
import styles from './MatchesTable.module.css'

interface MatchesTableProps {
  matches: MatchSummary[]
}

const MatchesTable = ({ matches }: MatchesTableProps) => {
  return (
    <div className={styles.tableWrapper}>
      <table className={styles.table}>
        <thead>
          <tr>
            <th>Date</th>
            <th>Mode</th>
            <th>Map</th>
            <th>Result</th>
            <th>Duration</th>
          </tr>
        </thead>
        <tbody>
          {matches.map((match) => (
            <tr key={match.id}>
              <td>{match.date}</td>
              <td>{match.mode}</td>
              <td>{match.map}</td>
              <td>
                <span
                  className={`${styles.resultBadge} ${
                    match.result === 'Victory' ? styles.victory : styles.defeat
                  }`}
                >
                  {match.result}
                </span>
              </td>
              <td>{match.duration}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export default MatchesTable

