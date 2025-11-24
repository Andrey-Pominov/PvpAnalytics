import { useState } from 'react'
import axios from 'axios'
import Card from '../components/Card/Card'
import SearchBar from '../components/SearchBar/SearchBar'
import { mockOpponentScoutList } from '../mocks/opponentScouting'

const OpponentScoutingPage = () => {
  const [searchTerm, setSearchTerm] = useState('')
  const [results, setResults] = useState<any[]>([])
  const [loading, setLoading] = useState(false)

  const handleSearch = async (term?: string) => {
    const searchValue = term || searchTerm
    if (!searchValue.trim()) return
    
    setSearchTerm(searchValue)
    setLoading(true)
    try {
      const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
      
      if (baseUrl === 'mock') {
        await new Promise((resolve) => setTimeout(resolve, 500))
        setResults(mockOpponentScoutList.filter(p => 
          p.playerName.toLowerCase().includes(searchValue.toLowerCase())
        ))
        return
      }

      const { data } = await axios.get(`${baseUrl}/opponent-scouting/search`, {
        params: { name: searchValue }
      })
      setResults(data.length > 0 ? data : mockOpponentScoutList)
    } catch (error) {
      console.error('Error searching players, using mock data:', error)
      setResults(mockOpponentScoutList.filter(p => 
        p.playerName.toLowerCase().includes(searchValue.toLowerCase())
      ))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Opponent Scouting</h1>
      <div>
        <SearchBar
          onSearch={handleSearch}
          placeholder="Search player by name..."
        />
      </div>
      {loading && <p className="text-text-muted">Loading...</p>}
      {results.map((player) => (
        <Card key={player.playerId}>
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2">
            <div className="min-w-0 flex-1">
              <h3 className="text-lg sm:text-xl font-semibold truncate">{player.playerName} - {player.realm}</h3>
              <p className="text-sm sm:text-base text-text-muted">Class: {player.class} | Spec: {player.currentSpec}</p>
            </div>
            <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-4 text-sm sm:text-base">
              <p>Rating: <span className="font-semibold">{player.currentRating}</span></p>
              <p>Win Rate: <span className="font-semibold">{player.winRate}%</span></p>
            </div>
          </div>
        </Card>
      ))}
    </div>
  )
}

export default OpponentScoutingPage

