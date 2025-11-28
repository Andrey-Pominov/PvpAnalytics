import { useState, useRef, useEffect, type ReactNode } from 'react'

interface TooltipProps {
  content: string
  children: ReactNode
  position?: 'top' | 'bottom' | 'left' | 'right'
}

const Tooltip = ({ content, children, position = 'top' }: TooltipProps) => {
  const [isVisible, setIsVisible] = useState(false)
  const tooltipId = useRef(`tooltip-${Math.random().toString(36).slice(2, 11)}`)

  const positionClasses = {
    top: 'bottom-full left-1/2 -translate-x-1/2 mb-2',
    bottom: 'top-full left-1/2 -translate-x-1/2 mt-2',
    left: 'right-full top-1/2 -translate-y-1/2 mr-2',
    right: 'left-full top-1/2 -translate-y-1/2 ml-2',
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      setIsVisible(false)
      return
    }

    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault()
      setIsVisible((prev) => !prev)
    }
  }

  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isVisible) {
        setIsVisible(false)
      }
    }

    if (isVisible) {
      document.addEventListener('keydown', handleEscape)
      return () => {
        document.removeEventListener('keydown', handleEscape)
      }
    }
  }, [isVisible])

  return (
    <div className="relative inline-block">
      <div
        className="inline-block border-0 bg-transparent p-0 text-inherit cursor-inherit"
        tabIndex={0}
        aria-describedby={isVisible ? tooltipId.current : undefined}
        onMouseEnter={() => setIsVisible(true)}
        onMouseLeave={() => setIsVisible(false)}
        onFocus={() => setIsVisible(true)}
        onBlur={() => setIsVisible(false)}
        onTouchStart={() => setIsVisible(true)}
        onTouchEnd={() => setIsVisible(false)}
        onKeyDown={handleKeyDown}
      >
        {children}
      </div>
      {isVisible && (
        <div
          id={tooltipId.current}
          className={`absolute z-50 rounded-lg bg-gray-900 px-3 py-2 text-xs text-white shadow-lg ${positionClasses[position]}`}
          role="tooltip"
          aria-live="polite"
        >
          {content}
          <div
            className={`absolute ${position === 'top' ? 'top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-gray-900' : position === 'bottom' ? 'bottom-full left-1/2 -translate-x-1/2 border-4 border-transparent border-b-gray-900' : position === 'left' ? 'left-full top-1/2 -translate-y-1/2 border-4 border-transparent border-l-gray-900' : 'right-full top-1/2 -translate-y-1/2 border-4 border-transparent border-r-gray-900'}`}
          />
        </div>
      )}
    </div>
  )
}

export default Tooltip

