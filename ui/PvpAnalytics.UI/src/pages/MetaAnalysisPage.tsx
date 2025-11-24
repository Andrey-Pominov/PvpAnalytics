import { useState, useEffect } from 'react'
import axios from 'axios'
import Card from '../components/Card/Card'
import { mockMetaAnalysis } from '../mocks/metaAnalysis'

const MetaAnalysisPage = () => {
  const [data, setData] = useState<any>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    if (baseUrl === 'mock') {
      setTimeout(() => {
        setData(mockMetaAnalysis)
        setLoading(false)
      }, 500)
      return
    }

    axios.get(`${baseUrl}/meta-analysis`)
      .then(response => setData(response.data || mockMetaAnalysis))
      .catch(error => {
        console.error('Error loading meta analysis, using mock data:', error)
        setData(mockMetaAnalysis)
      })
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <div className="flex flex-col gap-6"><div className="text-text-muted">Loading...</div></div>
  if (!data) return <div className="flex flex-col gap-6"><div className="text-text-muted">No data available</div></div>

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Meta Analysis</h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {data.compositions?.slice(0, 10).map((comp: any, idx: number) => (
          <Card key={idx}>
            <h3 className="text-base sm:text-lg font-semibold mb-2 truncate">#{comp.rank} {comp.composition}</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-sm">
              <p>Matches: <span className="font-semibold">{comp.totalMatches}</span></p>
              <p>Win Rate: <span className={`font-semibold ${comp.winRate > 50 ? 'text-green-600' : 'text-red-600'}`}>{comp.winRate}%</span></p>
              <p>Popularity: <span className="font-semibold">{comp.popularity}%</span></p>
              <p>Avg Rating: <span className="font-semibold">{comp.averageRating}</span></p>
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}

export default MetaAnalysisPage

