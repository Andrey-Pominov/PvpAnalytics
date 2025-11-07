import type { WinRateEntry } from '../../types/stats'
import styles from './WinRateList.module.css'

interface WinRateListProps {
  title: string
  entries: WinRateEntry[]
}

const WinRateList = ({ title, entries }: WinRateListProps) => {
  return (
    <div className={styles.container}>
      <h3 className={styles.title}>{title}</h3>
      <ul className={styles.list}>
        {entries.map((entry) => (
          <li key={entry.label} className={styles.item}>
            <span>{entry.label}</span>
            <span className={styles.value}>{entry.value}%</span>
            <div className={styles.barWrapper}>
              <div className={styles.bar} style={{ width: `${entry.value}%` }} />
            </div>
          </li>
        ))}
      </ul>
    </div>
  )
}

export default WinRateList

