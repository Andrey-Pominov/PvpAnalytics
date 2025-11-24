import { useState } from 'react'
import axios from 'axios'
import Card from '../components/Card/Card'
import { mockMatchupAnalytics } from '../mocks/matchupAnalytics'

const MatchupAnalyticsPage = () => {
  const [class1, setClass1] = useState('')
  const [class2, setClass2] = useState('')
  const [data, setData] = useState<any>(null)
  const [loading, setLoading] = useState(false)

  const handleSearch = async () => {
    if (!class1 || !class2) return
    
    setLoading(true)
    try {
      const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
      
      if (baseUrl === 'mock') {
        await new Promise((resolve) => setTimeout(resolve, 500))
        setData({ ...mockMatchupAnalytics, class1, class2 })
        return
      }

      const { data } = await axios.get(`${baseUrl}/matchup-analytics`, {
        params: { class1, class2 }
      })
      setData(data || { ...mockMatchupAnalytics, class1, class2 })
    } catch (error) {
      console.error('Error loading matchup, using mock data:', error)
      setData({ ...mockMatchupAnalytics, class1, class2 })
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Matchup Analytics</h1>
      <div className="mb-6 flex gap-4">
        <input
          type="text"
          value={class1}
          onChange={(e) => setClass1(e.target.value)}
          placeholder="Class 1"
          className="px-4 py-2 border rounded"
        />
        <input
          type="text"
          value={class2}
          onChange={(e) => setClass2(e.target.value)}
          placeholder="Class 2"
          className="px-4 py-2 border rounded"
        />
        <button
          onClick={handleSearch}
          className="px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
        >
          Search
        </button>
      </div>
      {loading && <p>Loading...</p>}
      {data && (
        <Card>
          <h2 className="text-xl font-semibold mb-4">{data.class1} vs {data.class2}</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p>Total Matches: {data.totalMatches}</p>
              <p>{data.class1} Wins: {data.winsForClass1} ({data.winRateForClass1}%)</p>
              <p>{data.class2} Wins: {data.winsForClass2} ({data.winRateForClass2}%)</p>
            </div>
            <div>
              <p>Avg Match Duration: {data.averageMatchDuration}s</p>
              {data.stats && (
                <>
                  <p>Avg Damage ({data.class1}): {data.stats.averageDamageClass1}</p>
                  <p>Avg Damage ({data.class2}): {data.stats.averageDamageClass2}</p>
                </>
              )}
            </div>
          </div>
        </Card>
      )}
    </div>
  )
}

export default MatchupAnalyticsPage

