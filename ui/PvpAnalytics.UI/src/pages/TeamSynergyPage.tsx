import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

interface PartnerSynergy {
  player1Id: number
  player1Name: string
  player2Id: number
  player2Name: string
  matchesTogether: number
  winsTogether: number
  winRateTogether: number
  averageRatingTogether: number
  synergyScore: number
}

interface TeamSynergy {
  teamId: number
  teamName: string
  partnerSynergies: PartnerSynergy[]
  mapWinRates: Record<string, number>
  compositionWinRates: Record<string, number>
  overallSynergyScore: number
}

const TeamSynergyPage = () => {
  const { teamId } = useParams<{ teamId: string }>()
  const navigate = useNavigate()
  const [synergy, setSynergy] = useState<TeamSynergy | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (!teamId) return
    loadSynergy()
  }, [teamId])

  const loadSynergy = async () => {
    if (!teamId) return
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      const response = await axios.get(`${baseUrl}/team-synergy/team/${teamId}`)
      setSynergy(response.data)
    } catch (error) {
      console.error('Error loading team synergy:', error)
      setSynergy(null)
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">Loading synergy data...</div></div>
  }

  if (!synergy) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">No synergy data available</div></div>
  }

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <h1 className="text-2xl sm:text-3xl font-bold">Team Synergy: {synergy.teamName}</h1>
        <button
          onClick={() => navigate('/teams')}
          className="px-4 py-2 bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors text-sm sm:text-base"
        >
          Back to Teams
        </button>
      </div>

      <Card>
        <h2 className="text-xl font-semibold mb-4">Overall Synergy Score</h2>
        <div className="text-4xl font-bold text-accent">{synergy.overallSynergyScore.toFixed(1)}</div>
      </Card>

      <div>
        <h2 className="text-xl font-semibold mb-4">Partner Synergies</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {synergy.partnerSynergies.map((partner, idx) => (
            <Card key={`${idx}-${partner.averageRatingTogether}`}>
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mb-3">
                <div>
                  <p className="font-semibold text-sm sm:text-base">
                    {partner.player1Name} & {partner.player2Name}
                  </p>
                </div>
                <span className="text-sm font-semibold text-accent">
                  Score: {partner.synergyScore.toFixed(1)}
                </span>
              </div>
              <div className="grid grid-cols-2 gap-2 text-sm">
                <p>Matches: <span className="font-semibold">{partner.matchesTogether}</span></p>
                <p>Win Rate: <span className={`font-semibold ${partner.winRateTogether > 50 ? 'text-green-600' : 'text-red-600'}`}>{partner.winRateTogether.toFixed(1)}%</span></p>
                <p>Wins: <span className="font-semibold">{partner.winsTogether}</span></p>
                <p>Avg Rating: <span className="font-semibold">{partner.averageRatingTogether.toFixed(0)}</span></p>
              </div>
              <div className="mt-3 flex gap-2">
                <button
                  onClick={() => navigate(`/players/${partner.player1Id}`)}
                  className="flex-1 px-3 py-1 bg-accent/10 text-accent rounded hover:bg-accent/20 transition-colors text-xs sm:text-sm"
                >
                  View {partner.player1Name}
                </button>
                <button
                  onClick={() => navigate(`/players/${partner.player2Id}`)}
                  className="flex-1 px-3 py-1 bg-accent/10 text-accent rounded hover:bg-accent/20 transition-colors text-xs sm:text-sm"
                >
                  View {partner.player2Name}
                </button>
              </div>
            </Card>
          ))}
        </div>
      </div>

      {Object.keys(synergy.mapWinRates).length > 0 && (
        <div>
          <h2 className="text-xl font-semibold mb-4">Map Win Rates</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {Object.entries(synergy.mapWinRates).map(([map, winRate]) => (
              <Card key={map}>
                <p className="font-semibold mb-2">{map}</p>
                <p className={`text-2xl font-bold ${winRate > 50 ? 'text-green-600' : 'text-red-600'}`}>
                  {winRate.toFixed(1)}%
                </p>
              </Card>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

export default TeamSynergyPage

