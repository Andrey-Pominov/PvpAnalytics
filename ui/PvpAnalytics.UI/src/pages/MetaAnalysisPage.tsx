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

  if (loading) return <div className="p-6">Loading...</div>
  if (!data) return <div className="p-6">No data available</div>

  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Meta Analysis</h1>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {data.compositions?.slice(0, 10).map((comp: any, idx: number) => (
          <Card key={idx}>
            <h3 className="text-lg font-semibold mb-2">#{comp.rank} {comp.composition}</h3>
            <p>Matches: {comp.totalMatches}</p>
            <p>Win Rate: <span className={comp.winRate > 50 ? 'text-green-600' : 'text-red-600'}>{comp.winRate}%</span></p>
            <p>Popularity: {comp.popularity}%</p>
            <p>Avg Rating: {comp.averageRating}</p>
          </Card>
        ))}
      </div>
    </div>
  )
}

export default MetaAnalysisPage

