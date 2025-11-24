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
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Opponent Scouting</h1>
      <div className="mb-6">
        <SearchBar
          onSearch={handleSearch}
          placeholder="Search player by name..."
        />
      </div>
      {loading && <p>Loading...</p>}
      {results.map((player) => (
        <Card key={player.playerId} className="mb-4">
          <h3 className="text-xl font-semibold">{player.playerName} - {player.realm}</h3>
          <p>Class: {player.class} | Spec: {player.currentSpec}</p>
          <p>Rating: {player.currentRating} | Win Rate: {player.winRate}%</p>
        </Card>
      ))}
    </div>
  )
}

export default OpponentScoutingPage

