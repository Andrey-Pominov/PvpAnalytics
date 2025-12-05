import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

interface Team {
  id: number
  name: string
  bracket?: string
  region?: string
  rating?: number
  isPublic: boolean
  totalMatches: number
  wins: number
  losses: number
  winRate: number
  members: Array<{
    id: number
    playerId: number
    playerName: string
    realm: string
    class: string
    spec?: string
    role?: string
    isPrimary: boolean
  }>
}

const TeamsPage = () => {
  const navigate = useNavigate()
  const [teams, setTeams] = useState<Team[]>([])
  const [loading, setLoading] = useState(false)
  const [bracket, setBracket] = useState<string>('')
  const [region, setRegion] = useState<string>('')

  useEffect(() => {
    loadTeams().then(() => {
      // Promise resolved
    }).catch((error) => {
      console.error('Error in useEffect loadTeams:', error);
    });
  }, [bracket, region])

  const loadTeams = async () => {
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      const params = new URLSearchParams()
      if (bracket) params.append('bracket', bracket)
      if (region) params.append('region', region)
      params.append('isPublic', 'true')
      
      const response = await axios.get(`${baseUrl}/teams/search?${params.toString()}`)
      setTeams(response.data || [])
    } catch (error) {
      console.error('Error loading teams:', error)
      setTeams([])
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">Loading teams...</div></div>
  }

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <h1 className="text-2xl sm:text-3xl font-bold">Teams</h1>
        <button
          onClick={() => navigate('/teams/create')}
          className="px-4 py-2 bg-accent text-white rounded-lg hover:bg-accent/90 transition-colors text-sm sm:text-base"
        >
          Create Team
        </button>
      </div>

      <div className="flex flex-col sm:flex-row gap-4">
        <select
          value={bracket}
          onChange={(e) => setBracket(e.target.value)}
          className="px-4 py-2 border border-accent-muted/30 rounded-lg bg-background text-text text-sm sm:text-base"
        >
          <option value="">All Brackets</option>
          <option value="2v2">2v2</option>
          <option value="3v3">3v3</option>
          <option value="RBG">RBG</option>
        </select>
        <input
          type="text"
          value={region}
          onChange={(e) => setRegion(e.target.value)}
          placeholder="Filter by region..."
          className="px-4 py-2 border border-accent-muted/30 rounded-lg bg-background text-text text-sm sm:text-base flex-1"
        />
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {teams.map((team) => (
          <Card key={team.id}>
            <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2 mb-3">
              <h3 className="text-base sm:text-lg font-semibold truncate">{team.name}</h3>
              {team.rating && (
                <span className="text-sm font-semibold text-accent">Rating: {team.rating}</span>
              )}
            </div>
            <div className="grid grid-cols-2 gap-2 text-sm mb-3">
              <p>Matches: <span className="font-semibold">{team.totalMatches}</span></p>
              <p>Win Rate: <span className={`font-semibold ${team.winRate > 50 ? 'text-green-600' : 'text-red-600'}`}>{team.winRate.toFixed(1)}%</span></p>
              {team.bracket && <p>Bracket: <span className="font-semibold">{team.bracket}</span></p>}
              {team.region && <p>Region: <span className="font-semibold">{team.region}</span></p>}
            </div>
            <div className="mt-3">
              <p className="text-xs sm:text-sm font-semibold mb-1">Members:</p>
              <div className="flex flex-wrap gap-2">
                {team.members.map((member) => (
                  <button
                    key={member.id}
                    type="button"
                    onClick={() => navigate(`/players/${member.playerId}`)}
                    aria-label={`View profile for ${member.playerName}`}
                    className="text-xs sm:text-sm px-2 py-1 bg-surface/50 rounded cursor-pointer hover:bg-surface/70 transition-colors focus:outline-none focus:ring-2 focus:ring-accent focus:ring-offset-2 focus:ring-offset-background"
                  >
                    {member.playerName} ({member.class})
                  </button>
                ))}
              </div>
            </div>
            <button
              onClick={() => navigate(`/teams/${team.id}`)}
              className="mt-4 w-full px-4 py-2 bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors text-sm"
            >
              View Details
            </button>
          </Card>
        ))}
      </div>

      {teams.length === 0 && !loading && (
        <div className="text-center text-text-muted py-8">
          No teams found. Create a team to get started!
        </div>
      )}
    </div>
  )
}

export default TeamsPage

