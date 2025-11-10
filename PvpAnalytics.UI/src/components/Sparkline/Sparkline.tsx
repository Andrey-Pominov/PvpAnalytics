interface SparklineProps {
  values: number[]
  stroke?: string
}

const Sparkline = ({ values, stroke = '#6c8bff' }: SparklineProps) => {
  if (values.length === 0) {
    return <div className="text-sm text-text-muted">No data</div>
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
    <svg className="h-20 w-full" viewBox="0 0 100 100" preserveAspectRatio="none">
      <polyline
        points={points}
        stroke={stroke}
        strokeWidth={3}
        strokeLinecap="round"
        strokeLinejoin="round"
        fill="none"
        style={{ filter: 'drop-shadow(0 6px 16px rgba(108, 139, 255, 0.35))' }}
      />
    </svg>
  )
}

export default Sparkline

