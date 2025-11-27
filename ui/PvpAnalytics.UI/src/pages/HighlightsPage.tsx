import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

interface FeaturedMatch {
  id: number
  matchId: number
  featuredAt: string
  reason?: string
  upvotes: number
  commentsCount: number
  match?: {
    id: number
    mapName: string
    gameMode: string
    duration: number
    createdOn: string
  }
}

const HighlightsPage = () => {
  const navigate = useNavigate()
  const [highlights, setHighlights] = useState<FeaturedMatch[]>([])
  const [loading, setLoading] = useState(false)
  const [period, setPeriod] = useState<string>('day')

  useEffect(() => {
    loadHighlights()
  }, [period])

  const loadHighlights = async () => {
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      const response = await axios.get(`${baseUrl}/highlights?period=${period}&limit=20`)
      setHighlights(response.data || [])
    } catch (error) {
      console.error('Error loading highlights:', error)
      setHighlights([])
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">Loading highlights...</div></div>
  }

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <h1 className="text-2xl sm:text-3xl font-bold">Match Highlights</h1>
        <select
          value={period}
          onChange={(e) => setPeriod(e.target.value)}
          className="px-4 py-2 border border-accent-muted/30 rounded-lg bg-background text-text text-sm sm:text-base"
        >
          <option value="day">Today</option>
          <option value="week">This Week</option>
          <option value="month">This Month</option>
        </select>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {highlights.map((highlight) => (
          <Card key={highlight.id}>
            <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2 mb-3">
              <h3 className="text-base sm:text-lg font-semibold">
                {highlight.reason || 'Featured Match'}
              </h3>
              <span className="text-xs sm:text-sm text-text-muted">
                {new Date(highlight.featuredAt).toLocaleDateString()}
              </span>
            </div>
            {highlight.match && (
              <div className="grid grid-cols-2 gap-2 text-sm mb-3">
                <p>Map: <span className="font-semibold">{highlight.match.mapName}</span></p>
                <p>Mode: <span className="font-semibold">{highlight.match.gameMode}</span></p>
                <p>Duration: <span className="font-semibold">{Math.floor(highlight.match.duration / 60)}m</span></p>
              </div>
            )}
            <div className="flex items-center gap-4 text-sm mb-3">
              <span>👍 {highlight.upvotes}</span>
              <span>💬 {highlight.commentsCount}</span>
            </div>
            <button
              onClick={() => navigate(`/matches/${highlight.matchId}`)}
              className="w-full px-4 py-2 bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors text-sm"
            >
              View Match
            </button>
          </Card>
        ))}
      </div>

      {highlights.length === 0 && !loading && (
        <div className="text-center text-text-muted py-8">
          No highlights available for this period.
        </div>
      )}
    </div>
  )
}

export default HighlightsPage

