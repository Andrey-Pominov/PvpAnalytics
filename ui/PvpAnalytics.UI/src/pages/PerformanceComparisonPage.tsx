import { useState } from 'react'
import { useParams } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'
import { mockPerformanceComparison } from '../mocks/performanceComparison'

const PerformanceComparisonPage = () => {
  const { playerId } = useParams<{ playerId: string }>()
  const [spec, setSpec] = useState('')
  const [data, setData] = useState<any>(null)
  const [loading, setLoading] = useState(false)

  const handleCompare = async () => {
    if (!playerId || !spec) return
    
    setLoading(true)
    try {
      const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
      
      if (baseUrl === 'mock') {
        await new Promise((resolve) => setTimeout(resolve, 500))
        setData({ ...mockPerformanceComparison, spec })
        return
      }

      const { data } = await axios.get(`${baseUrl}/performance-comparison/${playerId}`, {
        params: { spec }
      })
      setData(data || { ...mockPerformanceComparison, spec })
    } catch (error) {
      console.error('Error loading comparison, using mock data:', error)
      setData({ ...mockPerformanceComparison, spec })
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Performance Comparison</h1>
      <div className="mb-6">
        <input
          type="text"
          value={spec}
          onChange={(e) => setSpec(e.target.value)}
          placeholder="Enter spec (e.g., Assassination)"
          className="px-4 py-2 border rounded mr-4"
        />
        <button
          onClick={handleCompare}
          className="px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
        >
          Compare
        </button>
      </div>
      {loading && <p>Loading...</p>}
      {data && (
        <div className="space-y-4">
          <Card>
            <h2 className="text-xl font-semibold mb-4">Your Performance</h2>
            <div className="grid grid-cols-2 gap-4">
              <p>Win Rate: {data.playerMetrics.winRate}%</p>
              <p>Current Rating: {data.playerMetrics.currentRating}</p>
              <p>Avg Damage: {data.playerMetrics.averageDamage.toLocaleString()}</p>
              <p>Avg Healing: {data.playerMetrics.averageHealing.toLocaleString()}</p>
            </div>
          </Card>
          <Card>
            <h2 className="text-xl font-semibold mb-4">Top Players Average</h2>
            <div className="grid grid-cols-2 gap-4">
              <p>Win Rate: {data.topPlayerMetrics.averageWinRate}%</p>
              <p>Avg Rating: {data.topPlayerMetrics.averageRating}</p>
              <p>Avg Damage: {data.topPlayerMetrics.averageDamage.toLocaleString()}</p>
              <p>Avg Healing: {data.topPlayerMetrics.averageHealing.toLocaleString()}</p>
            </div>
          </Card>
          {data.percentiles && (
            <Card>
              <h2 className="text-xl font-semibold mb-4">Percentile Rankings</h2>
              <div className="grid grid-cols-2 gap-4">
                <p>Win Rate: {data.percentiles.winRatePercentile}th percentile</p>
                <p>Rating: {data.percentiles.ratingPercentile}th percentile</p>
                <p>Damage: {data.percentiles.damagePercentile}th percentile</p>
                <p>Healing: {data.percentiles.healingPercentile}th percentile</p>
              </div>
            </Card>
          )}
        </div>
      )}
    </div>
  )
}

export default PerformanceComparisonPage

