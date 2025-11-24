import { useState } from 'react'

interface ComparisonToggleProps {
  enabled: boolean
  onToggle: (enabled: boolean) => void
  currentPeriod?: string
  comparisonPeriod?: string
}

const ComparisonToggle = ({
  enabled,
  onToggle,
  currentPeriod = 'Last 7 days',
  comparisonPeriod = 'Previous 7 days',
}: ComparisonToggleProps) => {
  return (
    <div className="flex items-center gap-3">
      <label className="flex items-center gap-2 text-sm text-text-muted">
        <input
          type="checkbox"
          checked={enabled}
          onChange={(e) => onToggle(e.target.checked)}
          className="h-4 w-4 rounded border-accent-muted bg-surface text-accent focus:ring-2 focus:ring-accent"
        />
        <span>Compare</span>
      </label>
      {enabled && (
        <div className="flex items-center gap-2 text-xs text-text-muted">
          <div className="flex items-center gap-1">
            <div className="h-0.5 w-8 bg-accent" />
            <span>{currentPeriod}</span>
          </div>
          <span>vs</span>
          <div className="flex items-center gap-1">
            <div className="h-0.5 w-8 border-b-2 border-dashed border-gray-500" />
            <span>{comparisonPeriod}</span>
          </div>
        </div>
      )}
    </div>
  )
}

export default ComparisonToggle

