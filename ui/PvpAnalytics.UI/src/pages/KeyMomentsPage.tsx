import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'
import { mockKeyMoments } from '../mocks/keyMoments'

const KeyMomentsPage = () => {
  const { matchId } = useParams<{ matchId: string }>()
  const [data, setData] = useState<any>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!matchId) return
    
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    if (baseUrl === 'mock') {
      setTimeout(() => {
        setData(mockKeyMoments)
        setLoading(false)
      }, 500)
      return
    }

    axios.get(`${baseUrl}/key-moments/match/${matchId}`)
      .then(response => setData(response.data || mockKeyMoments))
      .catch(error => {
        console.error('Error loading key moments, using mock data:', error)
        setData(mockKeyMoments)
      })
      .finally(() => setLoading(false))
  }, [matchId])

  if (loading) return <div className="p-6">Loading...</div>
  if (!data) return <div className="p-6">No data available</div>

  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Key Moments</h1>
      <Card className="mb-4">
        <p>Match Date: {new Date(data.matchDate).toLocaleString()}</p>
      </Card>
      <div className="space-y-2">
        {data.moments?.map((moment: any, idx: number) => (
          <Card key={idx} className={moment.isCritical ? 'border-2 border-red-500' : ''}>
            <div className="flex justify-between">
              <div>
                <p className="font-semibold">{moment.eventType.toUpperCase()}</p>
                <p>{moment.description}</p>
                {moment.ability && <p className="text-sm text-gray-600">Ability: {moment.ability}</p>}
              </div>
              <div className="text-right">
                <p className="text-sm">{moment.timestamp}s</p>
                {moment.damageDone && <p className="text-red-600">{moment.damageDone.toLocaleString()} damage</p>}
                {moment.healingDone && <p className="text-green-600">{moment.healingDone.toLocaleString()} healing</p>}
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}

export default KeyMomentsPage

