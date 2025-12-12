/**
 * Export utilities for converting data to CSV and JSON formats
 */

/**
 * Converts an array of objects to CSV format
 * @param data Array of objects to convert
 * @param headers Optional custom headers. If not provided, uses object keys
 * @returns CSV string
 */
export function convertToCSV<T extends Record<string, unknown>>(
  data: T[],
  headers?: string[]
): string {
  if (data.length === 0) {
    return ''
  }

  // Use provided headers or extract from first object
  const csvHeaders = headers || Object.keys(data[0])

  // Escape CSV values (handle commas, quotes, newlines)
  const escapeCSV = (value: unknown): string => {
    if (value === null || value === undefined) {
      return ''
    }
    let str: string

    if (value instanceof Date) {
      str = value.toISOString()
    } else if (typeof value === 'object') {
      // For objects/arrays, serialize to JSON to avoid "[object Object]"
      try {
        str = JSON.stringify(value)
      } catch {
        // Fallback for circular references or non-serializable objects
        str = Array.isArray(value) ? '[Array]' : '[Object]'
      }
    } else {
      // Primitives (string, number, boolean, etc.) - convert to string
      str = String(value)
    }
    // If contains comma, quote, or newline, wrap in quotes and escape quotes
    if (str.includes(',') || str.includes('"') || str.includes('\n')) {
      return `"${str.replaceAll('"', '""')}"`
    }
    return str
  }

  // Build CSV rows
  const rows: string[] = []

  // Header row
  rows.push(csvHeaders.map(escapeCSV).join(','))

  // Data rows
  for (const item of data) {
    const row = csvHeaders.map((header) => escapeCSV(item[header]))
    rows.push(row.join(','))
  }

  return rows.join('\n')
}

/**
 * Converts an array of objects to JSON format
 * @param data Array of objects to convert
 * @returns JSON string (pretty-printed)
 */
export function convertToJSON<T>(data: T[]): string {
  return JSON.stringify(data, null, 2)
}

/**
 * Downloads data as a file
 * @param content File content as string
 * @param filename Name of the file to download
 * @param mimeType MIME type of the file (e.g., 'text/csv', 'application/json')
 */
export function downloadFile(content: string, filename: string, mimeType: string): void {
  const blob = new Blob([content], { type: mimeType })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

/**
 * Exports data as CSV file
 * @param data Array of objects to export
 * @param filename Name of the file (without extension)
 * @param headers Optional custom headers
 */
export function exportToCSV<T extends Record<string, unknown>>(
  data: T[],
  filename: string,
  headers?: string[]
): void {
  const csv = convertToCSV(data, headers)
  const fullFilename = filename.endsWith('.csv') ? filename : `${filename}.csv`
  downloadFile(csv, fullFilename, 'text/csv;charset=utf-8;')
}

/**
 * Exports data as JSON file
 * @param data Array of objects to export
 * @param filename Name of the file (without extension)
 */
export function exportToJSON<T>(data: T[], filename: string): void {
  const json = convertToJSON(data)
  const fullFilename = filename.endsWith('.json') ? filename : `${filename}.json`
  downloadFile(json, fullFilename, 'application/json;charset=utf-8')
}

