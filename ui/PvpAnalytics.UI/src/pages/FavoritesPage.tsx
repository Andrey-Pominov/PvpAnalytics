import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import axios from 'axios'
import Card from '../components/Card/Card'

interface FavoritePlayer {
  id: number
  targetPlayerId: number
  playerName: string
  realm: string
  class: string
  spec?: string
  createdAt: string
}

const FavoritesPage = () => {
  const navigate = useNavigate()
  const [favorites, setFavorites] = useState<FavoritePlayer[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    loadFavorites()
  }, [])

  const loadFavorites = async () => {
    setLoading(true)
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      const token = localStorage.getItem('accessToken')
      const response = await axios.get(`${baseUrl}/favorites`, {
        headers: { Authorization: `Bearer ${token}` }
      })
      setFavorites(response.data || [])
    } catch (error) {
      console.error('Error loading favorites:', error)
      setFavorites([])
    } finally {
      setLoading(false)
    }
  }

  const handleRemove = async (playerId: number) => {
    const baseUrl = import.meta.env.VITE_ANALYTICS_API_BASE_URL || 'http://localhost:8080/api'
    
    try {
      const token = localStorage.getItem('accessToken')
      await axios.delete(`${baseUrl}/favorites/${playerId}`, {
        headers: { Authorization: `Bearer ${token}` }
      })
      setFavorites(favorites.filter(f => f.targetPlayerId !== playerId))
    } catch (error) {
      console.error('Error removing favorite:', error)
      alert('Failed to remove favorite')
    }
  }

  if (loading) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">Loading favorites...</div></div>
  }

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Favorite Players</h1>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {favorites.map((favorite) => (
          <Card key={favorite.id}>
            <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2 mb-3">
              <h3 className="text-base sm:text-lg font-semibold">{favorite.playerName}</h3>
              <button
                onClick={() => handleRemove(favorite.targetPlayerId)}
                className="text-red-600 hover:text-red-700 text-sm"
              >
                Remove
              </button>
            </div>
            <div className="grid grid-cols-2 gap-2 text-sm mb-3">
              <p>Realm: <span className="font-semibold">{favorite.realm}</span></p>
              <p>Class: <span className="font-semibold">{favorite.class}</span></p>
              {favorite.spec && <p>Spec: <span className="font-semibold">{favorite.spec}</span></p>}
            </div>
            <button
              onClick={() => navigate(`/players/${favorite.targetPlayerId}`)}
              className="w-full px-4 py-2 bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors text-sm"
            >
              View Profile
            </button>
          </Card>
        ))}
      </div>

      {favorites.length === 0 && !loading && (
        <div className="text-center text-text-muted py-8">
          No favorite players yet. Add players to your favorites to track them easily!
        </div>
      )}
    </div>
  )
}

export default FavoritesPage

