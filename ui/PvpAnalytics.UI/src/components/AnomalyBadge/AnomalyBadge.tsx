import Tooltip from '../Tooltip/Tooltip'
import type { AnomalyResult } from '../../utils/statisticsUtils'

interface AnomalyBadgeProps {
  anomaly: AnomalyResult<unknown>
  label?: string
  className?: string
}

const AnomalyBadge = ({ anomaly, label, className = '' }: AnomalyBadgeProps) => {
  if (!anomaly.isAnomaly) {
    return null
  }

  const deviationText = `${anomaly.deviation.toFixed(1)}σ`
  const direction = anomaly.zScore > 0 ? 'above' : 'below'
  const tooltipContent = `Anomaly detected: ${deviationText} ${direction} average. This value is statistically unusual.`

  return (
    <Tooltip content={tooltipContent}>
      <span
        className={`inline-flex items-center gap-1 rounded-full px-2 py-1 text-xs font-semibold ${
          anomaly.zScore > 0
            ? 'bg-amber-500/20 text-amber-200'
            : 'bg-blue-500/20 text-blue-200'
        } ${className}`}
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          className="h-3 w-3"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
          />
        </svg>
        {label || 'Anomaly'}
      </span>
    </Tooltip>
  )
}

export default AnomalyBadge

