import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

interface LeaderboardEntry {
  teamId: number
  teamName: string
  rating?: number
  totalMatches: number
  wins: number
  losses: number
  winRate: number
  rank: number
  lastMatchDate: string
  memberNames: string[]
}

interface Leaderboard {
  bracket: string
  region?: string
  entries: LeaderboardEntry[]
  totalTeams: number
  lastUpdated: string
}

const TeamLeaderboardPage = () => {
  const navigate = useNavigate()
  const [leaderboard, setLeaderboard] = useState<Leaderboard | null>(null)
  const [loading, setLoading] = useState(false)
  const [bracket, setBracket] = useState<string>('2v2')
  const [region, setRegion] = useState<string>('')

  useEffect(() => {
    loadLeaderboard()
  }, [bracket, region])

  const loadLeaderboard = async () => {
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      const params = new URLSearchParams()
      if (region) params.append('region', region)
      params.append('limit', '100')
      
      const response = await axios.get(`${baseUrl}/team-leaderboards/${bracket}?${params.toString()}`)
      setLeaderboard(response.data)
    } catch (error) {
      console.error('Error loading leaderboard:', error)
      setLeaderboard(null)
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">Loading leaderboard...</div></div>
  }

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Team Leaderboards</h1>

      <div className="flex flex-col sm:flex-row gap-4">
        <select
          value={bracket}
          onChange={(e) => setBracket(e.target.value)}
          className="px-4 py-2 border border-accent-muted/30 rounded-lg bg-background text-text text-sm sm:text-base"
        >
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

      {leaderboard && (
        <>
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 text-sm text-text-muted">
            <p>Total Teams: {leaderboard.totalTeams}</p>
            <p>Last Updated: {new Date(leaderboard.lastUpdated).toLocaleString()}</p>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full border-collapse">
              <thead>
                <tr className="border-b border-accent-muted/30">
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Rank</th>
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Team</th>
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Rating</th>
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Matches</th>
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Win Rate</th>
                  <th className="text-left p-2 sm:p-4 text-sm sm:text-base font-semibold">Members</th>
                </tr>
              </thead>
              <tbody>
                {leaderboard.entries.map((entry) => (
                  <tr
                    key={entry.teamId}
                    className="border-b border-accent-muted/10 hover:bg-surface/50 cursor-pointer transition-colors"
                    onClick={() => navigate(`/teams/${entry.teamId}`)}
                  >
                    <td className="p-2 sm:p-4 text-sm sm:text-base font-semibold">#{entry.rank}</td>
                    <td className="p-2 sm:p-4 text-sm sm:text-base">{entry.teamName}</td>
                    <td className="p-2 sm:p-4 text-sm sm:text-base">{entry.rating ?? 'N/A'}</td>
                    <td className="p-2 sm:p-4 text-sm sm:text-base">{entry.totalMatches}</td>
                    <td className={`p-2 sm:p-4 text-sm sm:text-base font-semibold ${entry.winRate > 50 ? 'text-green-600' : 'text-red-600'}`}>
                      {entry.winRate.toFixed(1)}%
                    </td>
                    <td className="p-2 sm:p-4 text-sm sm:text-base">
                      <div className="flex flex-wrap gap-1">
                        {entry.memberNames.slice(0, 3).map((name, idx) => (
                          <span key={idx} className="text-xs px-2 py-1 bg-surface/50 rounded">
                            {name}
                          </span>
                        ))}
                        {entry.memberNames.length > 3 && (
                          <span className="text-xs text-text-muted">+{entry.memberNames.length - 3}</span>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {leaderboard.entries.length === 0 && (
            <div className="text-center text-text-muted py-8">
              No teams found in this bracket.
            </div>
          )}
        </>
      )}
    </div>
  )
}

export default TeamLeaderboardPage

