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

  if (loading) return <div className="p-6">Loading...</div>

  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Team Compositions</h1>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {data.map((team, idx) => (
          <Card key={idx}>
            <h3 className="text-lg font-semibold mb-2">{team.composition}</h3>
            <p>Matches: {team.totalMatches}</p>
            <p>Win Rate: <span className={team.winRate > 50 ? 'text-green-600' : 'text-red-600'}>{team.winRate}%</span></p>
            <p>Synergy Score: {team.synergyScore}</p>
            <p>Peak Rating: {team.peakRating}</p>
            <div className="mt-2">
              <p className="text-sm font-semibold">Members:</p>
              {team.members?.map((member: any, mIdx: number) => (
                <p key={mIdx} className="text-sm">{member.playerName} ({member.class})</p>
              ))}
            </div>
          </Card>
        ))}
      </div>
    </div>
  )
}

export default TeamCompositionPage

