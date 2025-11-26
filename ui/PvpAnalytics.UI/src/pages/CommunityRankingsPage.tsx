import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

interface RankingEntry {
  rank: number
  playerId?: number
  playerName?: string
  teamId?: number
  teamName?: string
  score: number
}

interface CommunityRanking {
  rankingType: string
  period: string
  scope?: string
  entries: RankingEntry[]
  lastUpdated: string
}

const CommunityRankingsPage = () => {
  const navigate = useNavigate()
  const [rankings, setRankings] = useState<CommunityRanking | null>(null)
  const [loading, setLoading] = useState(false)
  const [rankingType, setRankingType] = useState<string>('MostWatched')
  const [period, setPeriod] = useState<string>('weekly')

  useEffect(() => {
    loadRankings()
  }, [rankingType, period])

  const loadRankings = async () => {
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      const response = await axios.get(`${baseUrl}/community-rankings/${rankingType}?period=${period}`)
      setRankings(response.data)
    } catch (error) {
      console.error('Error loading rankings:', error)
      setRankings(null)
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">Loading rankings...</div></div>
  }

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Community Rankings</h1>

      <div className="flex flex-col sm:flex-row gap-4">
        <select
          value={rankingType}
          onChange={(e) => setRankingType(e.target.value)}
          className="px-4 py-2 border border-accent-muted/30 rounded-lg bg-background text-text text-sm sm:text-base"
        >
          <option value="MostWatched">Most Watched</option>
          <option value="TopWinrate">Top Win Rate</option>
          <option value="MostDiscussed">Most Discussed</option>
        </select>
        <select
          value={period}
          onChange={(e) => setPeriod(e.target.value)}
          className="px-4 py-2 border border-accent-muted/30 rounded-lg bg-background text-text text-sm sm:text-base"
        >
          <option value="daily">Daily</option>
          <option value="weekly">Weekly</option>
          <option value="monthly">Monthly</option>
        </select>
      </div>

      {rankings && (
        <>
          <div className="text-sm text-text-muted">
            Last Updated: {new Date(rankings.lastUpdated).toLocaleString()}
          </div>

          <div className="overflow-x-auto">
            <table className="w-full border-collapse">
              <thead>
                <tr className="border-b border-accent-muted/30">
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Rank</th>
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Name</th>
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Type</th>
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Score</th>
                </tr>
              </thead>
              <tbody>
                {rankings.entries.map((entry) => (
                  <tr
                    key={entry.rank}
                    className="border-b border-accent-muted/10 hover:bg-surface/50 cursor-pointer transition-colors"
                    onClick={() => {
                      if (entry.playerId) navigate(`/players/${entry.playerId}`)
                      else if (entry.teamId) navigate(`/teams/${entry.teamId}`)
                    }}
                  >
                    <td className="p-2 sm:p-4 text-sm sm:text-base font-semibold">#{entry.rank}</td>
                    <td className="p-2 sm:p-4 text-sm sm:text-base">{entry.playerName || entry.teamName || 'N/A'}</td>
                    <td className="p-2 sm:p-4 text-sm sm:text-base">{entry.playerId ? 'Player' : 'Team'}</td>
                    <td className="p-2 sm:p-4 text-sm sm:text-base font-semibold">{entry.score.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {rankings.entries.length === 0 && (
            <div className="text-center text-text-muted py-8">
              No rankings available.
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default CommunityRankingsPage

