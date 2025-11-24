import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'
import { mockRatingProgression } from '../mocks/ratingProgression'

const RatingProgressionPage = () => {
  const { playerId } = useParams<{ playerId: string }>()
  const [data, setData] = useState<any>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!playerId) return
    
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    if (baseUrl === 'mock') {
      setTimeout(() => {
        setData(mockRatingProgression)
        setLoading(false)
      }, 500)
      return
    }

    axios.get(`${baseUrl}/rating-progression/${playerId}`)
      .then(response => setData(response.data || mockRatingProgression))
      .catch(error => {
        console.error('Error loading rating progression, using mock data:', error)
        setData(mockRatingProgression)
      })
      .finally(() => setLoading(false))
  }, [playerId])

  if (loading) return <div className="flex flex-col gap-6"><div className="text-text-muted">Loading...</div></div>
  if (!data) return <div className="flex flex-col gap-6"><div className="text-text-muted">No data available</div></div>

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Rating Progression</h1>
      <Card>
        <h2 className="text-xl font-semibold mb-4">{data.playerName}</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <p>Current Rating: <span className="font-bold">{data.summary.currentRating}</span></p>
            <p>Peak Rating: <span className="font-bold">{data.summary.peakRating}</span></p>
            <p>Average Rating: <span className="font-bold">{data.summary.averageRating}</span></p>
          </div>
          <div>
            <p>Net Change: <span className="font-bold">{data.summary.netRatingChange}</span></p>
            <p>Total Gain: <span className="font-bold text-green-600">{data.summary.totalRatingGain}</span></p>
            <p>Total Loss: <span className="font-bold text-red-600">{data.summary.totalRatingLoss}</span></p>
          </div>
        </div>
        <div className="mt-6">
          <h3 className="text-lg font-semibold mb-2">Rating History</h3>
          <div className="space-y-2">
            {data.dataPoints.slice(-10).map((point: any, idx: number) => (
              <div key={idx} className="flex flex-col sm:flex-row sm:justify-between gap-2 p-2 bg-surface/50 rounded">
                <span className="text-sm">{new Date(point.matchDate).toLocaleDateString()}</span>
                <span className={`text-sm ${point.ratingChange > 0 ? 'text-green-600' : 'text-red-600'}`}>
                  {point.ratingBefore} → {point.ratingAfter} ({point.ratingChange > 0 ? '+' : ''}{point.ratingChange})
                </span>
              </div>
            ))}
          </div>
        </div>
      </Card>
    </div>
  )
}

export default RatingProgressionPage

