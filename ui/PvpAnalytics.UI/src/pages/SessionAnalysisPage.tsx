import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

const SessionAnalysisPage = () => {
  const { playerId } = useParams<{ playerId: string }>()
  const [data, setData] = useState<any>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!playerId) return
    
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'

    axios
      .get(`${baseUrl}/session-analysis/${playerId}`)
      .then(response => setData(response.data))
      .catch(error => {
        console.error('Error loading session analysis:', error)
        setData(null)
      })
      .finally(() => setLoading(false))
  }, [playerId])

  if (loading) return <div className="p-6">Loading...</div>
  if (!data) return <div className="p-6">No data available</div>

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Session Analysis</h1>
      <Card className="mb-6">
        <h2 className="text-xl font-semibold mb-4">Summary</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <p>Total Sessions: {data.summary.totalSessions}</p>
          <p>Avg Win Rate: {data.summary.averageWinRate}%</p>
          <p>Net Rating Change: <span className={data.summary.netRatingChange > 0 ? 'text-green-600' : 'text-red-600'}>{data.summary.netRatingChange}</span></p>
          <p>Avg Matches/Session: {data.summary.averageMatchesPerSession}</p>
        </div>
      </Card>
      <div>
        <h2 className="text-xl font-semibold mb-4">Recent Sessions</h2>
        <div className="space-y-2">
          {data.sessions?.slice(0, 10).map((session: any, idx: number) => (
            <Card key={`${idx}-${session.startTime}`}>
              <div className="flex flex-col sm:flex-row sm:justify-between gap-2 sm:gap-4">
                <div className="flex-1 min-w-0">
                  <p className="font-semibold text-sm sm:text-base">{new Date(session.startTime).toLocaleString()}</p>
                  <p className="text-xs sm:text-sm text-text-muted">Matches: {session.matchCount} | Win Rate: {session.winRate}%</p>
                </div>
                <div className="text-left sm:text-right flex-shrink-0">
                  <p className="text-sm sm:text-base">Rating: {session.ratingStart} → {session.ratingEnd}</p>
                  <p className={`text-sm sm:text-base ${session.ratingChange > 0 ? 'text-green-600' : 'text-red-600'}`}>
                    {session.ratingChange > 0 ? '+' : ''}{session.ratingChange}
                  </p>
                </div>
              </div>
            </Card>
          ))}
        </div>
      </div>
    </div>
  )
}

export default SessionAnalysisPage

