import { useState, type ReactNode } from 'react'

interface TooltipProps {
  content: string
  children: ReactNode
  position?: 'top' | 'bottom' | 'left' | 'right'
}

const Tooltip = ({ content, children, position = 'top' }: TooltipProps) => {
  const [isVisible, setIsVisible] = useState(false)

  const positionClasses = {
    top: 'bottom-full left-1/2 -translate-x-1/2 mb-2',
    bottom: 'top-full left-1/2 -translate-x-1/2 mt-2',
    left: 'right-full top-1/2 -translate-y-1/2 mr-2',
    right: 'left-full top-1/2 -translate-y-1/2 ml-2',
  }

  return (
    <div
      className="relative inline-block"
      onMouseEnter={() => setIsVisible(true)}
      onMouseLeave={() => setIsVisible(false)}
    >
      {children}
      {isVisible && (
        <div
          className={`absolute z-50 rounded-lg bg-gray-900 px-3 py-2 text-xs text-white shadow-lg ${positionClasses[position]}`}
          role="tooltip"
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

