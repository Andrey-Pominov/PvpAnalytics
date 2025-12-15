import Card from '../Card/Card'
import Tooltip from '../Tooltip/Tooltip'
import { getSuccessColors, getWarningColors, getErrorColors } from '../../utils/themeColors'
import type { ForecastResult } from '../../utils/statisticsUtils'

interface ForecastCardProps {
  forecast: ForecastResult
  title?: string
  currentValue: number
  unit?: string
  className?: string
}

const ForecastCard = ({
  forecast,
  title = 'Forecast',
  currentValue,
  unit = '',
  className = '',
}: ForecastCardProps) => {
  const confidenceColors = {
    high: getSuccessColors().text,
    medium: getWarningColors().text,
    low: getErrorColors().text,
  }

  const confidenceBgColors = {
    high: getSuccessColors().bg,
    medium: getWarningColors().bg,
    low: getErrorColors().bg,
  }

  const trendIcons = {
    increasing: '↑',
    decreasing: '↓',
    stable: '→',
  }

  const trendColors = {
    increasing: getSuccessColors().text,
    decreasing: getErrorColors().text,
    stable: 'text-text-muted',
  }

  const change = forecast.projectedValue - currentValue
  const changePercent = currentValue > 0 ? ((change / currentValue) * 100).toFixed(1) : '0'

  return (
    <Card className={className}>
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <div className="mb-2 flex items-center gap-2">
            <h3 className="text-sm font-semibold uppercase tracking-[0.08em] text-text-muted">{title}</h3>
            <Tooltip content={`Confidence: ${forecast.confidence}. Based on historical trend analysis.`}>
              <span
                className={`rounded-full px-2 py-1 text-xs font-semibold ${confidenceColors[forecast.confidence]} ${confidenceBgColors[forecast.confidence]}`}
              >
                {forecast.confidence.toUpperCase()}
              </span>
            </Tooltip>
          </div>
          <div className="flex items-baseline gap-2">
            <span className="text-2xl font-bold text-text">
              {forecast.projectedValue.toFixed(0)}
              {unit}
            </span>
            <span
              className={`flex items-center gap-1 rounded-full px-2 py-1 text-xs font-semibold ${trendColors[forecast.trend]}`}
            >
              <span>{trendIcons[forecast.trend]}</span>
              <span>
                {change > 0 ? '+' : ''}
                {change.toFixed(0)}
                {unit} ({changePercent}%)
              </span>
            </span>
          </div>
          <div className="mt-3 space-y-1 text-xs text-text-muted">
            <div className="flex items-center justify-between">
              <span>Current:</span>
              <span className="font-semibold text-text">
                {currentValue.toFixed(0)}
                {unit}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <span>Projected (7 days):</span>
              <span className="font-semibold text-text">
                {forecast.projectedValue.toFixed(0)}
                {unit}
              </span>
            </div>
            {forecast.daysToTarget !== undefined && forecast.targetValue !== undefined && (
              <div className="mt-2 rounded-lg bg-accent/10 px-2 py-1 text-accent">
                <span className="font-semibold">
                  {forecast.daysToTarget} day
                  {forecast.daysToTarget === 1 ? '' : 's'} to reach{' '}
                  {forecast.targetValue}
                  {unit}
                </span>
              </div>
            )}
          </div>
        </div>
      </div>
    </Card>
  )
}

export default ForecastCard

