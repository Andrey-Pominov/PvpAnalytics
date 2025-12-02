import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

const KeyMomentsPage = () => {
  const { matchId } = useParams<{ matchId: string }>()
  const [data, setData] = useState<any>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!matchId) return
    
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'

    axios
      .get(`${baseUrl}/key-moments/match/${matchId}`)
      .then(response => setData(response.data))
      .catch(error => {
        console.error('Error loading key moments:', error)
        setData(null)
      })
      .finally(() => setLoading(false))
  }, [matchId])

  if (loading) return <div className="p-6">Loading...</div>
  if (!data) return <div className="p-6">No data available</div>

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Key Moments</h1>
      <Card>
        <p className="text-sm sm:text-base">Match Date: {new Date(data.matchDate).toLocaleString()}</p>
      </Card>
      <div className="space-y-2">
        {data.moments?.map((moment: any, idx: number) => (
          <Card key={`${idx}-${moment.timestamp}`} className={moment.isCritical ? 'border-2 border-red-500' : ''}>
            <div className="flex flex-col sm:flex-row sm:justify-between gap-2 sm:gap-4">
              <div className="flex-1 min-w-0">
                <p className="font-semibold text-sm sm:text-base">{moment.eventType.toUpperCase()}</p>
                <p className="text-xs sm:text-sm">{moment.description}</p>
                {moment.ability && <p className="text-xs text-text-muted mt-1">Ability: {moment.ability}</p>}
              </div>
              <div className="text-left sm:text-right flex-shrink-0">
                <p className="text-xs sm:text-sm font-semibold">{moment.timestamp}s</p>
                {moment.damageDone && <p className="text-xs sm:text-sm text-red-600">{moment.damageDone.toLocaleString()} damage</p>}
                {moment.healingDone && <p className="text-xs sm:text-sm text-green-600">{moment.healingDone.toLocaleString()} healing</p>}
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}

export default KeyMomentsPage

