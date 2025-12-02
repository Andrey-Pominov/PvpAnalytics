import { useState, useRef, type DragEvent } from 'react'
import axios from 'axios'
import Card from '../components/Card/Card'
import type { UploadResponse } from '../types/api'

const UploadPage = () => {
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [isDragging, setIsDragging] = useState(false)
  const [result, setResult] = useState<{ success: boolean; message: string; matchCount?: number } | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleFileChange = (selectedFile: File | null) => {
    if (!selectedFile) return

    const validExtensions = ['.lua', '.txt']
    const fileExtension = selectedFile.name.toLowerCase().substring(selectedFile.name.lastIndexOf('.'))
    
    if (!validExtensions.includes(fileExtension)) {
      setResult({
        success: false,
        message: 'Invalid file type. Please upload a .lua or .txt file.',
      })
      return
    }

    setFile(selectedFile)
    setResult(null)
  }

  const handleDragEnter = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(true)
  }

  const handleDragLeave = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(false)
  }

  const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    e.stopPropagation()
  }

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragging(false)

    const droppedFile = e.dataTransfer.files[0]
    if (droppedFile) {
      handleFileChange(droppedFile)
    }
  }

  const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files?.[0]) {
      handleFileChange(e.target.files[0])
    }
  }

  const handleUpload = async () => {
    if (!file) return

    setUploading(true)
    setResult(null)

    try {
      const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
      const formData = new FormData()
      formData.append('file', file)

      const { data } = await axios.post<UploadResponse>(`${baseUrl}/logs/upload`, formData, {
        timeout: 300000, // 5 minutes timeout for large files
      })

      const matchCount = Array.isArray(data) ? data.length : 1
      setResult({
        success: true,
        message: `Successfully uploaded and processed!`,
        matchCount,
      })
      setFile(null)
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    } catch (error: any) {
      console.error('Upload failed', error)
      const errorMessage =
        error.response?.data?.message ||
        error.message ||
        'Upload failed. Please check your file and try again.'
      setResult({
        success: false,
        message: errorMessage,
      })
    } finally {
      setUploading(false)
    }
  }

  const handleRemoveFile = () => {
    setFile(null)
    setResult(null)
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <Card title="Upload Combat Log">
        <div className="flex flex-col gap-6">
          {/* Drag and Drop Area */}
          <section
            aria-label="File upload drop zone. Drag and drop your combat log file here, or use the browse button to select a file."
            onDragEnter={handleDragEnter}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            className={`relative flex flex-col items-center justify-center rounded-2xl border-2 border-dashed p-12 transition-colors ${
              isDragging
                ? 'border-accent bg-accent/10'
                : 'border-accent-muted/50 bg-surface/30 hover:border-accent-muted/70 hover:bg-surface/40'
            }`}
          >
            <input
              ref={fileInputRef}
              type="file"
              accept=".lua,.txt"
              onChange={handleFileInputChange}
              className="hidden"
            />
            <div className="flex flex-col items-center gap-4 text-center">
              <div className="grid h-16 w-16 place-items-center rounded-full bg-gradient-to-br from-accent to-sky-400">
                <svg
                  className="h-8 w-8 text-white"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
                  />
                </svg>
              </div>
              <div>
                <p className="text-sm font-semibold text-text">
                  {isDragging ? 'Drop file here' : 'Drag and drop your file here'}
                </p>
                <p className="mt-1 text-xs text-text-muted">
                  or{' '}
                  <button
                    type="button"
                    onClick={() => fileInputRef.current?.click()}
                    className="text-accent hover:text-accent/80 underline"
                  >
                    browse
                  </button>{' '}
                  to upload
                </p>
                <p className="mt-2 text-xs text-text-muted">Supports .lua and .txt files</p>
              </div>
            </div>
          </section>

          {/* File Info */}
          {file && (
            <div className="flex items-center justify-between rounded-xl border border-accent-muted/30 bg-surface/50 p-4">
              <div className="flex items-center gap-3">
                <div className="grid h-10 w-10 place-items-center rounded-lg bg-accent/20">
                  <svg
                    className="h-5 w-5 text-accent"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                    xmlns="http://www.w3.org/2000/svg"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                    />
                  </svg>
                </div>
                <div>
                  <p className="text-sm font-semibold text-text">{file.name}</p>
                  <p className="text-xs text-text-muted">
                    {(file.size / 1024).toFixed(2)} KB
                  </p>
                </div>
              </div>
              <button
                onClick={handleRemoveFile}
                className="rounded-lg px-3 py-2 text-sm text-text-muted hover:bg-surface/70 hover:text-text transition-colors"
                disabled={uploading}
              >
                Remove
              </button>
            </div>
          )}

          {/* Upload Button */}
          <button
            onClick={handleUpload}
            disabled={!file || uploading}
            className="w-full rounded-xl bg-gradient-to-r from-accent to-sky-400 px-6 py-4 text-sm font-semibold text-white transition-all disabled:opacity-50 disabled:cursor-not-allowed hover:shadow-lg hover:-translate-y-0.5 focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-2 focus:ring-offset-background"
          >
            {uploading ? 'Uploading and processing...' : 'Upload & Process'}
          </button>

          {/* Result Message */}
          {result && (
            <div
              className={`rounded-xl border p-4 ${
                result.success
                  ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-200'
                  : 'border-rose-500/40 bg-rose-500/10 text-rose-200'
              }`}
            >
              <div className="flex items-start gap-3">
                <div className="flex-shrink-0">
                  {result.success ? (
                    <svg
                      className="h-5 w-5 text-emerald-300"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  ) : (
                    <svg
                      className="h-5 w-5 text-rose-300"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                      xmlns="http://www.w3.org/2000/svg"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                  )}
                </div>
                <div className="flex-1">
                  <p className="text-sm font-semibold">{result.message}</p>
                  {result.success && result.matchCount !== undefined && (
                    <p className="mt-1 text-xs opacity-80">
                      Processed {result.matchCount}{' '}
                      {result.matchCount === 1 ? 'match' : 'matches'} from the log file.
                    </p>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      </Card>
    </div>
  )
}

export default UploadPage

