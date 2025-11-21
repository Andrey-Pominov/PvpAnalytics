import { useState } from 'react'
import { exportToCSV, exportToJSON } from '../../utils/exportUtils'

interface ExportButtonProps<T extends Record<string, unknown>> {
  data: T[]
  filename: string
  headers?: string[]
  className?: string
}

const ExportButton = <T extends Record<string, unknown>>({
  data,
  filename,
  headers,
  className = '',
}: ExportButtonProps<T>) => {
  const [showMenu, setShowMenu] = useState(false)

  const handleExportCSV = () => {
    exportToCSV(data, filename, headers)
    setShowMenu(false)
  }

  const handleExportJSON = () => {
    exportToJSON(data, filename)
    setShowMenu(false)
  }

  if (data.length === 0) {
    return null
  }

  return (
    <div className={`relative ${className}`}>
      <button
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
        <>
          {/* Backdrop to close menu */}
          <div
            className="fixed inset-0 z-10"
            onClick={() => setShowMenu(false)}
            aria-hidden="true"
          />
          {/* Menu */}
          <div className="absolute right-0 top-full z-20 mt-2 w-40 rounded-lg border border-accent-muted/40 bg-surface/95 shadow-lg backdrop-blur-lg">
            <button
              onClick={handleExportCSV}
              className="w-full rounded-t-lg px-4 py-2 text-left text-sm text-text transition-colors hover:bg-surface/70"
            >
              Export as CSV
            </button>
            <button
              onClick={handleExportJSON}
              className="w-full rounded-b-lg px-4 py-2 text-left text-sm text-text transition-colors hover:bg-surface/70"
            >
              Export as JSON
            </button>
          </div>
        </>
      )}
    </div>
  )
}

export default ExportButton

