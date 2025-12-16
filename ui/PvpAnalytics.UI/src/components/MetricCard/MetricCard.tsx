import type { ReactNode } from 'react'

interface MetricCardProps {
  title: string
  value: string | number
  subtitle?: string | ReactNode
  trend?: 'up' | 'down' | 'neutral'
  trendValue?: string
  tooltip?: string
  icon?: ReactNode
  className?: string
}

const MetricCard = ({
  title,
  value,
  subtitle,
  trend,
  trendValue,
  tooltip,
  icon,
  className = '',
}: MetricCardProps) => {
  const trendColors = {
    up: 'text-[var(--color-success-text)]',
    down: 'text-[var(--color-error-text)]',
    neutral: 'text-text-muted',
  }

  const trendBgColors = {
    up: 'bg-[var(--color-success-bg)]',
    down: 'bg-[var(--color-error-bg)]',
    neutral: 'bg-surface/50',
  }

  let trendIcon = ''
  if (trend === 'up') {
    trendIcon = '↑'
  } else if (trend === 'down') {
    trendIcon = '↓'
  }

  return (
    <div
      className={`group relative rounded-2xl border border-accent-muted/40 bg-surface/90 p-4 sm:p-6 shadow-card backdrop-blur-lg transition-all hover:border-accent-muted/60 hover:shadow-lg ${className}`}
      title={tooltip}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="mb-2 flex items-center gap-2">
            {icon && <div className="text-accent">{icon}</div>}
            <h3 className="text-sm font-semibold uppercase tracking-[0.08em] text-text-muted">{title}</h3>
          </div>
          <div className="flex items-baseline gap-2 flex-wrap">
            <span className="text-2xl sm:text-3xl font-bold text-text">{value}</span>
            {trend && trendValue && (
              <span
                className={`flex items-center gap-1 rounded-full px-2 py-1 text-xs font-semibold ${trendColors[trend]} ${trendBgColors[trend]}`}
              >
                <span>{trendIcon}</span>
                <span>{trendValue}</span>
              </span>
            )}
          </div>
          {subtitle && (
            <div className="mt-2 text-xs text-text-muted">
              {typeof subtitle === 'string' ? <p>{subtitle}</p> : subtitle}
            </div>
          )}
        </div>
      </div>
      {tooltip && (
        <div className="absolute right-2 top-2 opacity-0 transition-opacity group-hover:opacity-100">
          <div className="grid h-5 w-5 place-items-center rounded-full bg-accent/20 text-xs text-accent">?</div>
        </div>
      )}
    </div>
  )
}

export default MetricCard

