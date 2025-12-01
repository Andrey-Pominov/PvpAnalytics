import { useState, useRef, useEffect } from 'react'
import { exportToCSV, exportToJSON } from '../../utils/exportUtils'
import { getSecureRandomId } from '../../utils/secureRandom'

interface ExportButtonProps<T extends Record<string, unknown>> {
  data: T[]
  filename: string
  headers?: string[]
  className?: string
  disabled?: boolean
}

const ExportButton = <T extends Record<string, unknown>>({
  data,
  filename,
  headers,
  className = '',
  disabled = false,
}: ExportButtonProps<T>) => {
  const [showMenu, setShowMenu] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)
  const menuId = getSecureRandomId('export-menu')

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        menuRef.current &&
        !menuRef.current.contains(event.target as Node) &&
        buttonRef.current &&
        !buttonRef.current.contains(event.target as Node)
      ) {
        setShowMenu(false)
      }
    }

    if (showMenu) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [showMenu])

  // Handle keyboard navigation
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (!showMenu) return

      if (event.key === 'Escape') {
        setShowMenu(false)
        buttonRef.current?.focus()
      }

      if (event.key === 'Tab' && !menuRef.current?.contains(event.target as Node)) {
        setShowMenu(false)
      }
    }

    if (showMenu) {
      document.addEventListener('keydown', handleKeyDown)
      return () => document.removeEventListener('keydown', handleKeyDown)
    }
  }, [showMenu])

  const handleExportCSV = () => {
    exportToCSV(data, filename, headers)
    setShowMenu(false)
  }

  const handleExportJSON = () => {
    exportToJSON(data, filename)
    setShowMenu(false)
  }

  if (data.length === 0 || disabled) {
    return null
  }

  return (
    <div className={`relative ${className}`}>
      <button
        ref={buttonRef}
        type="button"
        aria-expanded={showMenu}
        aria-controls={menuId}
        aria-haspopup="menu"
        onClick={() => setShowMenu(!showMenu)}
        className="flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold text-text-muted transition-colors hover:bg-surface/50 hover:text-text"
        aria-label="Export data"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          className="h-4 w-4"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          />
        </svg>
        Export
        <svg
          xmlns="http://www.w3.org/2000/svg"
          className={`h-3 w-3 transition-transform ${showMenu ? 'rotate-180' : ''}`}
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {showMenu && (
        <div
          ref={menuRef}
          id={menuId}
          role="menu"
          aria-label="Export options"
          className="absolute right-0 top-full z-20 mt-2 w-40 rounded-lg border border-accent-muted/40 bg-surface/95 shadow-lg backdrop-blur-lg"
        >
          <button
            type="button"
            role="menuitem"
            onClick={handleExportCSV}
            className="w-full rounded-t-lg px-4 py-2 text-left text-sm text-text transition-colors hover:bg-surface/70"
            tabIndex={0}
          >
            Export as CSV
          </button>
          <button
            type="button"
            role="menuitem"
            onClick={handleExportJSON}
            className="w-full rounded-b-lg px-4 py-2 text-left text-sm text-text transition-colors hover:bg-surface/70"
            tabIndex={0}
          >
            Export as JSON
          </button>
        </div>
      )}
    </div>
  )
}

export default ExportButton

