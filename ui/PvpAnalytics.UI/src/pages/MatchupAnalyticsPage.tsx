import { useState } from 'react'
import axios from 'axios'
import Card from '../components/Card/Card'

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

      const { data } = await axios.get(`${baseUrl}/matchup-analytics`, {
        params: { class1, class2 },
      })
      setData(data)
    } catch (error) {
      console.error('Error loading matchup:', error)
      setData(null)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Matchup Analytics</h1>
      <div className="flex flex-col sm:flex-row gap-4">
        <input
          type="text"
          value={class1}
          onChange={(e) => setClass1(e.target.value)}
          placeholder="Class 1"
          className="flex-1 px-4 py-2 border rounded"
        />
        <input
          type="text"
          value={class2}
          onChange={(e) => setClass2(e.target.value)}
          placeholder="Class 2"
          className="flex-1 px-4 py-2 border rounded"
        />
        <button
          onClick={handleSearch}
          className="px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 whitespace-nowrap"
        >
          Search
        </button>
      </div>
      {loading && <p>Loading...</p>}
      {data && (
        <Card>
          <h2 className="text-xl font-semibold mb-4">{data.class1} vs {data.class2}</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
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

