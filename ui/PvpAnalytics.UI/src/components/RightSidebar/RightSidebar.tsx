import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useRecentlyViewedStore, type RecentlyViewedItem } from '../../store/recentlyViewedStore'
import { getWoWClassColor, getFactionColor } from '../../utils/themeColors'

// Format time ago in real-time
const formatTimeAgo = (timestamp: number): string => {
  const seconds = Math.floor((Date.now() - timestamp) / 1000)
  
  if (seconds < 5) return 'just now'
  if (seconds < 60) return `${seconds}s ago`
  
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days}d ago`
  
  return new Date(timestamp).toLocaleDateString()
}


interface RecentlyViewedItemProps {
  item: RecentlyViewedItem
  onRemove: (id: number) => void
}

const RecentlyViewedItemCard = ({ item, onRemove }: RecentlyViewedItemProps) => {
  const [timeAgo, setTimeAgo] = useState(() => formatTimeAgo(item.viewedAt))

  // Update time ago every second for real-time display
  useEffect(() => {
    const interval = setInterval(() => {
      setTimeAgo(formatTimeAgo(item.viewedAt))
    }, 1000)

    return () => clearInterval(interval)
  }, [item.viewedAt])

  return (
    <div className="group relative flex items-center gap-3 rounded-lg border border-accent-muted/30 bg-surface/50 p-3 transition-colors hover:bg-surface/70">
      <Link
        to={`/players/${item.id}`}
        className="flex flex-1 items-center gap-3 min-w-0"
      >
        {/* Avatar */}
        <div className="grid h-10 w-10 flex-shrink-0 place-items-center rounded-lg bg-gradient-to-br from-accent to-sky-400 text-sm font-bold text-white">
          {item.name.substring(0, 1).toUpperCase()}
        </div>
        
        {/* Info */}
        <div className="flex-1 min-w-0">
          <h4 className={`font-semibold truncate ${getWoWClassColor(item.class)}`}>
            {item.name}
          </h4>
          <div className="flex items-center gap-2 text-xs">
            <span className="text-text-muted truncate">{item.realm}</span>
            {item.faction && (
              <span className={getFactionColor(item.faction)}>
                {item.faction.toLowerCase().includes('alliance') ? '⚔️' : '🔥'}
              </span>
            )}
          </div>
        </div>
      </Link>
      
      {/* Time ago - Real-time */}
      <div className="flex flex-col items-end gap-1">
        <span className="text-xs text-accent font-medium whitespace-nowrap">
          {timeAgo}
        </span>
        {/* Remove button - shows on hover */}
        <button
          type="button"
          onClick={(e) => {
            e.preventDefault()
            e.stopPropagation()
            onRemove(item.id)
          }}
          className="opacity-0 group-hover:opacity-100 text-text-muted hover:text-[var(--color-error-text)] transition-opacity"
          aria-label={`Remove ${item.name} from recently viewed`}
        >
          <svg className="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
    </div>
  )
}

interface RightSidebarProps {
  isOpen: boolean
  onToggle: () => void
}

const RightSidebar = ({ isOpen, onToggle }: RightSidebarProps) => {
  const { items, removeItem, clearAll } = useRecentlyViewedStore()

  return (
    <>
      {/* Toggle button - always visible on xl screens */}
      <button
        type="button"
        onClick={onToggle}
        className={`hidden xl:flex fixed top-[77px] z-50 items-center justify-center p-2 rounded-lg border border-accent-muted/30 bg-background/95 backdrop-blur-sm text-text-muted hover:text-text hover:bg-surface/50 transition-all duration-300 ${
          isOpen ? 'right-[21rem]' : 'right-4'
        }`}
        aria-label={isOpen ? 'Close recently viewed sidebar' : 'Open recently viewed sidebar'}
      >
        <svg
          className={`h-5 w-5 transition-transform duration-300 ${isOpen ? '' : 'rotate-180'}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 5l7 7-7 7M5 5l7 7-7 7" />
        </svg>
      </button>

      {/* Sidebar */}
      <aside
        className={`sidebar hidden xl:flex fixed right-0 top-[73px] bottom-0 w-80 z-40 border-l backdrop-blur-sm flex-col transition-transform duration-300 ${
          isOpen ? 'translate-x-0' : 'translate-x-full'
        }`}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-accent-muted/30">
          <div>
            <h2 className="text-lg font-semibold text-text">Recently Viewed</h2>
            <p className="text-xs text-text-muted">Characters you've checked</p>
          </div>
          {items.length > 0 && (
            <button
              type="button"
              onClick={clearAll}
              className="text-xs text-text-muted hover:text-[var(--color-error-text)] transition-colors"
            >
              Clear all
            </button>
          )}
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-4">
          {items.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full text-center">
              <svg className="h-12 w-12 text-text-muted/50 mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p className="text-text-muted text-sm">No recently viewed characters</p>
              <p className="text-text-muted/70 text-xs mt-1">
                View a player profile to see them here
              </p>
            </div>
          ) : (
            <div className="flex flex-col gap-3">
              {items.map((item) => (
                <RecentlyViewedItemCard
                  key={item.id}
                  item={item}
                  onRemove={removeItem}
                />
              ))}
            </div>
          )}
        </div>

        {/* Footer with count */}
        {items.length > 0 && (
          <div className="p-4 border-t border-accent-muted/30">
            <p className="text-xs text-text-muted text-center">
              {items.length} character{items.length == 1 ? '' : 's'} in history
            </p>
          </div>
        )}
      </aside>
    </>
  )
}

export default RightSidebar

