import { useState } from 'react'

interface SparklineProps {
  values: number[]
  comparisonValues?: number[]
  stroke?: string
  comparisonStroke?: string
  showComparison?: boolean
}

const Sparkline = ({
  values,
  comparisonValues,
  stroke,
  comparisonStroke,
  showComparison = false,
}: SparklineProps) => {
  // Use CSS variables if stroke colors not provided
  const mainStroke = stroke || 'var(--sparkline-stroke)'
  const compStroke = comparisonStroke || 'var(--sparkline-comparison)'
  const [hoveredIndex, setHoveredIndex] = useState<number | null>(null)

  if (values.length === 0) {
    return <div className="text-sm text-text-muted">No data</div>
  }

  const max = Math.max(...values, ...(comparisonValues || []))
  const min = Math.min(...values, ...(comparisonValues || []))
  const range = max - min || 1

  const points = values
    .map((value, index) => {
      const x = (index / (values.length - 1 || 1)) * 100
      const y = 100 - ((value - min) / range) * 100
      return `${x},${y}`
    })
    .join(' ')

  const comparisonPoints =
    comparisonValues && comparisonValues.length > 0
      ? comparisonValues
          .map((value, index) => {
            const x = (index / (comparisonValues.length - 1 || 1)) * 100
            const y = 100 - ((value - min) / range) * 100
            return `${x},${y}`
          })
          .join(' ')
      : null

  return (
    <div className="relative">
      <svg
        className="h-20 w-full"
        viewBox="0 0 100 100"
        preserveAspectRatio="none"
        onMouseLeave={() => setHoveredIndex(null)}
      >
        {/* Comparison line (dashed, behind main line) */}
        {showComparison && comparisonPoints && (
          <polyline
            points={comparisonPoints}
            stroke={compStroke}
            strokeWidth={2}
            strokeDasharray="4 4"
            strokeLinecap="round"
            strokeLinejoin="round"
            fill="none"
            opacity={0.6}
          />
        )}
        {/* Main line */}
        <polyline
          points={points}
          stroke={mainStroke}
          strokeWidth={3}
          strokeLinecap="round"
          strokeLinejoin="round"
          fill="none"
          style={{ filter: 'drop-shadow(0 6px 16px rgba(108, 139, 255, 0.35))' }}
        />
        {/* Hover points */}
        {values.map((value, index) => {
          const x = (index / (values.length - 1 || 1)) * 100
          const y = 100 - ((value - min) / range) * 100
          return (
            <circle
              key={`${index}-${value}`}
              cx={x}
              cy={y}
              r={hoveredIndex === index ? 4 : 0}
              fill={mainStroke}
              className="transition-all"
              onMouseEnter={() => setHoveredIndex(index)}
              style={{ cursor: 'pointer' }}
            />
          )
        })}
      </svg>
      {hoveredIndex !== null && (
        <div className="absolute bottom-full left-1/2 mb-2 -translate-x-1/2 rounded px-2 py-1 text-xs" style={{ backgroundColor: 'var(--sparkline-tooltip-bg)', color: 'var(--sparkline-tooltip-text)' }}>
          {values[hoveredIndex]}
        </div>
      )}
    </div>
  )
}

export default Sparkline

