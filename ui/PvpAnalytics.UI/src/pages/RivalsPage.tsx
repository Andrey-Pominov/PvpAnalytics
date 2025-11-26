import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

interface Rival {
  id: number
  opponentPlayerId: number
  opponentPlayerName: string
  realm: string
  class: string
  spec?: string
  notes?: string
  intensityScore: number
  createdAt: string
  matchesPlayed: number
  wins: number
  losses: number
  winRate: number
}

const RivalsPage = () => {
  const navigate = useNavigate()
  const [rivals, setRivals] = useState<Rival[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    loadRivals()
  }, [])

  const loadRivals = async () => {
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      const token = localStorage.getItem('accessToken')
      const response = await axios.get(`${baseUrl}/rivals`, {
        headers: { Authorization: `Bearer ${token}` }
      })
      setRivals(response.data || [])
    } catch (error) {
      console.error('Error loading rivals:', error)
      setRivals([])
    } finally {
      setLoading(false)
    }
  }

  const handleRemove = async (rivalId: number) => {
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      const token = localStorage.getItem('accessToken')
      await axios.delete(`${baseUrl}/rivals/${rivalId}`, {
        headers: { Authorization: `Bearer ${token}` }
      })
      setRivals(rivals.filter(r => r.id !== rivalId))
    } catch (error) {
      console.error('Error removing rival:', error)
      alert('Failed to remove rival')
    }
  }

  if (loading) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">Loading rivals...</div></div>
  }

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Rivals</h1>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {rivals.map((rival) => (
          <Card key={rival.id}>
            <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2 mb-3">
              <div>
                <h3 className="text-base sm:text-lg font-semibold">{rival.opponentPlayerName}</h3>
                <p className="text-xs sm:text-sm text-text-muted">Intensity: {rival.intensityScore}/10</p>
              </div>
              <button
                onClick={() => handleRemove(rival.id)}
                className="text-red-600 hover:text-red-700 text-sm"
              >
                Remove
              </button>
            </div>
            <div className="grid grid-cols-2 gap-2 text-sm mb-3">
              <p>Realm: <span className="font-semibold">{rival.realm}</span></p>
              <p>Class: <span className="font-semibold">{rival.class}</span></p>
              {rival.spec && <p>Spec: <span className="font-semibold">{rival.spec}</span></p>}
              <p>Matches: <span className="font-semibold">{rival.matchesPlayed}</span></p>
              <p>Win Rate: <span className={`font-semibold ${rival.winRate > 50 ? 'text-green-600' : 'text-red-600'}`}>{rival.winRate.toFixed(1)}%</span></p>
            </div>
            {rival.notes && (
              <div className="mb-3">
                <p className="text-xs sm:text-sm text-text-muted italic">"{rival.notes}"</p>
              </div>
            )}
            <div className="flex gap-2">
              <button
                onClick={() => navigate(`/players/${rival.opponentPlayerId}`)}
                className="flex-1 px-4 py-2 bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors text-sm"
              >
                View Profile
              </button>
              <button
                onClick={() => navigate(`/players/${rival.opponentPlayerId}/matchup`)}
                className="flex-1 px-4 py-2 bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors text-sm"
              >
                Matchup Stats
              </button>
            </div>
          </Card>
        ))}
      </div>

      {rivals.length === 0 && !loading && (
        <div className="text-center text-text-muted py-8">
          No rivals tracked yet. Add players as rivals to track your matchups against them!
        </div>
      )}
    </div>
  )
}

export default RivalsPage

