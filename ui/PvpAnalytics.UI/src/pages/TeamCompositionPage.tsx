import { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'
import { mockPlayerTeams } from '../mocks/teamComposition'

const TeamCompositionPage = () => {
  const { playerId } = useParams<{ playerId: string }>()
  const [data, setData] = useState<any[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!playerId) return
    
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    if (baseUrl === 'mock') {
      setTimeout(() => {
        setData(mockPlayerTeams)
        setLoading(false)
      }, 500)
      return
    }

    axios.get(`${baseUrl}/team-composition/player/${playerId}/teams`)
      .then(response => setData(response.data.length > 0 ? response.data : mockPlayerTeams))
      .catch(error => {
        console.error('Error loading team compositions, using mock data:', error)
        setData(mockPlayerTeams)
      })
      .finally(() => setLoading(false))
  }, [playerId])

  if (loading) return <div className="flex flex-col gap-6"><div className="text-text-muted">Loading...</div></div>

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Team Compositions</h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {data.map((team, idx) => (
          <Card key={idx}>
            <h3 className="text-base sm:text-lg font-semibold mb-2 truncate">{team.composition}</h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-sm">
              <p>Matches: <span className="font-semibold">{team.totalMatches}</span></p>
              <p>Win Rate: <span className={`font-semibold ${team.winRate > 50 ? 'text-green-600' : 'text-red-600'}`}>{team.winRate}%</span></p>
              <p>Synergy Score: <span className="font-semibold">{team.synergyScore}</span></p>
              <p>Peak Rating: <span className="font-semibold">{team.peakRating}</span></p>
            </div>
            <div className="mt-3">
              <p className="text-xs sm:text-sm font-semibold mb-1">Members:</p>
              <div className="flex flex-wrap gap-2">
                {team.members?.map((member: any, mIdx: number) => (
                  <span key={mIdx} className="text-xs sm:text-sm px-2 py-1 bg-surface/50 rounded">{member.playerName} ({member.class})</span>
                ))}
              </div>
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}

export default TeamCompositionPage

