import { useState, useMemo } from 'react'
import type { TimelineEvent } from '../../types/api'

interface TimelineProps {
  events: TimelineEvent[]
  matchDuration: number
}

type FilterType = 'all' | 'cooldowns' | 'cc' | 'kills'

const Timeline = ({ events }: TimelineProps) => {
  const [filter, setFilter] = useState<FilterType>('all')

  const filteredEvents = useMemo(() => {
    switch (filter) {
      case 'cooldowns':
        return events.filter((e) => e.isCooldown)
      case 'cc':
        return events.filter((e) => e.isCC)
      case 'kills':
        // Kills are high damage events near the end or that result in significant damage
        return events.filter((e) => e.damageDone && e.damageDone > 50000)
      default:
        return events
    }
  }, [events, filter])

  const formatTimestamp = (seconds: number): string => {
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins}:${secs.toString().padStart(2, '0')}`
  }

  const getEventIcon = (event: TimelineEvent): string => {
    if (event.isCooldown) return '🛡️'
    if (event.isCC) return '🌀'
    if (event.damageDone && event.damageDone > 50000) return '⚔️'
    if (event.healingDone && event.healingDone > 30000) return '💚'
    return '📋'
  }

  const getEventColor = (event: TimelineEvent): string => {
    if (event.isCooldown) return 'border-blue-500/40 bg-blue-500/10'
    if (event.isCC) return 'border-purple-500/40 bg-purple-500/10'
    if (event.damageDone && event.damageDone > 50000) return 'border-red-500/40 bg-red-500/10'
    if (event.healingDone && event.healingDone > 30000) return 'border-green-500/40 bg-green-500/10'
    return 'border-accent-muted/40 bg-surface/50'
  }

  const getEventBadge = (event: TimelineEvent): string | null => {
    if (event.isCooldown) return 'Cooldown'
    if (event.isCC) return 'CC'
    if (event.damageDone && event.damageDone > 50000) return 'High Damage'
    return null
  }

  return (
    <div className="flex flex-col gap-4">
      {/* Filter Toggles */}
      <div className="flex flex-wrap gap-2">
        {(['all', 'cooldowns', 'cc', 'kills'] as FilterType[]).map((filterType) => {
          let label: string
          if (filterType === 'all') {
            label = 'All Events'
          } else if (filterType === 'cooldowns') {
            label = 'Cooldowns/Defensives'
          } else if (filterType === 'cc') {
            label = 'Crowd Control'
          } else {
            label = 'Kills'
          }

          return (
          <button
            key={filterType}
            onClick={() => setFilter(filterType)}
            className={`rounded-lg px-4 py-2 text-sm font-semibold transition-colors ${
              filter === filterType
                ? 'bg-gradient-to-r from-accent to-sky-400 text-white'
                : 'text-text-muted hover:text-text hover:bg-surface/50'
            }`}
          >
            {label}
          </button>
        )})}
      </div>

      {/* Timeline Events */}
      <div className="space-y-3">
        {filteredEvents.length === 0 ? (
          <div className="text-center py-12 text-text-muted">No events found for this filter.</div>
        ) : (
          filteredEvents.map((event, index) => (
            <div
              key={`${index}-${event.timestamp}`}
              className={`flex items-start gap-4 rounded-lg border p-4 transition-colors hover:border-accent-muted/60 ${getEventColor(
                event
              )}`}
            >
              {/* Timeline Indicator */}
              <div className="flex flex-col items-center gap-2">
                <div className="text-xs font-semibold text-text-muted">{formatTimestamp(event.timestamp)}</div>
                <div className="h-full w-0.5 bg-accent-muted/30" />
              </div>

              {/* Event Content */}
              <div className="flex-1">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="text-lg">{getEventIcon(event)}</span>
                      <span className="font-semibold text-text">{event.ability}</span>
                      {getEventBadge(event) && (() => {
                        let badgeClasses = 'bg-red-500/20 text-red-200'
                        if (event.isCooldown) {
                          badgeClasses = 'bg-blue-500/20 text-blue-200'
                        } else if (event.isCC) {
                          badgeClasses = 'bg-purple-500/20 text-purple-200'
                        }

                        return (
                          <span
                            className={`rounded-full px-2 py-1 text-xs font-semibold ${badgeClasses}`}
                          >
                            {getEventBadge(event)}
                          </span>
                        )
                      })()}
                    </div>

                    <div className="mt-1 text-sm text-text-muted">
                      {event.sourcePlayerName && (
                        <span className="font-semibold text-text">{event.sourcePlayerName}</span>
                      )}
                      {event.targetPlayerName && (
                        <>
                          <span className="mx-2">→</span>
                          <span className="font-semibold text-text">{event.targetPlayerName}</span>
                        </>
                      )}
                    </div>

                    {/* Event Details */}
                    <div className="mt-2 flex flex-wrap gap-4 text-xs text-text-muted">
                      {event.damageDone !== null && (
                        <span>
                          <span className="font-semibold text-red-300">{event.damageDone.toLocaleString()}</span>{' '}
                          damage
                        </span>
                      )}
                      {event.healingDone !== null && (
                        <span>
                          <span className="font-semibold text-green-300">{event.healingDone.toLocaleString()}</span>{' '}
                          healing
                        </span>
                      )}
                      {event.crowdControl && (
                        <span>
                          <span className="font-semibold text-purple-300">{event.crowdControl}</span>
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  )
}

export default Timeline

