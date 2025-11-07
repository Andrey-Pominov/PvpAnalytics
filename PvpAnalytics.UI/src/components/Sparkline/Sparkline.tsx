import styles from './Sparkline.module.css'

interface SparklineProps {
  values: number[]
  stroke?: string
}

const Sparkline = ({ values, stroke = '#6c8bff' }: SparklineProps) => {
  if (values.length === 0) {
    return <div className={styles.empty}>No data</div>
  }

  const max = Math.max(...values)
  const min = Math.min(...values)
  const range = max - min || 1

  const points = values
    .map((value, index) => {
      const x = (index / (values.length - 1 || 1)) * 100
      const y = 100 - ((value - min) / range) * 100
      return `${x},${y}`
    })
    .join(' ')

  return (
    <svg className={styles.sparkline} viewBox="0 0 100 100" preserveAspectRatio="none">
      <polyline points={points} stroke={stroke} fill="none" />
    </svg>
  )
}

export default Sparkline

