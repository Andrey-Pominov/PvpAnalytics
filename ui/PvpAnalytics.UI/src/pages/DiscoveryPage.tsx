import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

const DiscoveryPage = () => {
  const navigate = useNavigate()
  const [trendingPlayers, setTrendingPlayers] = useState<any[]>([])
  const [trendingTeams, setTrendingTeams] = useState<any[]>([])
  const [recentHighlights, setRecentHighlights] = useState<any[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    loadDiscoveryData()
  }, [])

  const loadDiscoveryData = async () => {
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      // Load trending players (simplified - would use actual trending algorithm)
      const playersResponse = await axios.get(`${baseUrl}/players?take=5`)
      setTrendingPlayers(playersResponse.data?.slice(0, 5) || [])

      // Load trending teams
      const teamsResponse = await axios.get(`${baseUrl}/teams/search?isPublic=true`)
      setTrendingTeams(teamsResponse.data?.slice(0, 5) || [])

      // Load recent highlights
      const highlightsResponse = await axios.get(`${baseUrl}/highlights?period=week&limit=5`)
      setRecentHighlights(highlightsResponse.data || [])
    } catch (error) {
      console.error('Error loading discovery data:', error)
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">Loading discovery data...</div></div>
  }

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Discover</h1>

      <div>
        <h2 className="text-xl font-semibold mb-4">Trending Players</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {trendingPlayers.map((player) => (
            <Card key={player.id}>
              <h3 className="text-base sm:text-lg font-semibold mb-2">{player.name}</h3>
              <div className="text-sm text-text-muted mb-3">
                {player.realm} • {player.class}
              </div>
              <button
                onClick={() => navigate(`/players/${player.id}`)}
                className="w-full px-4 py-2 bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors text-sm"
              >
                View Profile
              </button>
            </Card>
          ))}
        </div>
      </div>

      <div>
        <h2 className="text-xl font-semibold mb-4">Trending Teams</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {trendingTeams.map((team) => (
            <Card key={team.id}>
              <h3 className="text-base sm:text-lg font-semibold mb-2">{team.name}</h3>
              <div className="text-sm text-text-muted mb-3">
                {team.bracket} • Win Rate: {team.winRate.toFixed(1)}%
              </div>
              <button
                onClick={() => navigate(`/teams/${team.id}`)}
                className="w-full px-4 py-2 bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors text-sm"
              >
                View Team
              </button>
            </Card>
          ))}
        </div>
      </div>

      <div>
        <h2 className="text-xl font-semibold mb-4">Recent Highlights</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {recentHighlights.map((highlight) => (
            <Card key={highlight.id}>
              <h3 className="text-base sm:text-lg font-semibold mb-2">
                {highlight.reason || 'Featured Match'}
              </h3>
              <div className="text-sm text-text-muted mb-3">
                {new Date(highlight.featuredAt).toLocaleDateString()}
              </div>
              <button
                onClick={() => navigate(`/matches/${highlight.matchId}`)}
                className="w-full px-4 py-2 bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors text-sm"
              >
                View Match
              </button>
            </Card>
          ))}
        </div>
      </div>
    </div>
  )
}

export default DiscoveryPage

