import { useState, useEffect } from 'react'
import axios from 'axios'
import Card from '../components/Card/Card'

interface Profile {
  userId: string
  displayName?: string
  bio?: string
  avatarUrl?: string
  isProfilePublic: boolean
  showStatsToFriendsOnly: boolean
}

const ProfileSettingsPage = () => {
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [formData, setFormData] = useState<Partial<Profile>>({})

  useEffect(() => {
    loadProfile()
  }, [])

  const loadProfile = async () => {
    setLoading(true)
    const baseUrl = import.meta.env.VITE_AUTH_API_BASE_URL || 'http://localhost:8081/api'
    
    try {
      const token = localStorage.getItem('accessToken')
      const response = await axios.get(`${baseUrl}/profile`, {
        headers: { Authorization: `Bearer ${token}` }
      })
      setFormData(response.data)
    } catch (error) {
      console.error('Error loading profile:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleSave = async () => {
    setSaving(true)
    const baseUrl = import.meta.env.VITE_AUTH_API_BASE_URL || 'http://localhost:8081/api'
    
    try {
      const token = localStorage.getItem('accessToken')
      await axios.put(`${baseUrl}/profile`, formData, {
        headers: { Authorization: `Bearer ${token}` }
      })
      alert('Profile updated successfully!')
    } catch (error) {
      console.error('Error updating profile:', error)
      alert('Failed to update profile')
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return <div className="flex flex-col gap-6 p-4 sm:p-6"><div className="text-text-muted">Loading profile...</div></div>
  }

  return (
    <div className="flex flex-col gap-6 p-4 sm:p-6">
      <h1 className="text-2xl sm:text-3xl font-bold">Profile Settings</h1>

      <Card>
        <h2 className="text-xl font-semibold mb-4">Public Profile</h2>
        <div className="flex flex-col gap-4">
          <div>
            <label className="block text-sm font-semibold mb-2">Display Name</label>
            <input
              type="text"
              value={formData.displayName || ''}
              onChange={(e) => setFormData({ ...formData, displayName: e.target.value })}
              className="w-full px-4 py-2 border border-accent-muted/30 rounded-lg bg-background text-text"
              placeholder="Your display name"
            />
          </div>
          <div>
            <label className="block text-sm font-semibold mb-2">Bio</label>
            <textarea
              value={formData.bio || ''}
              onChange={(e) => setFormData({ ...formData, bio: e.target.value })}
              className="w-full px-4 py-2 border border-accent-muted/30 rounded-lg bg-background text-text"
              placeholder="Tell us about yourself..."
              rows={4}
            />
          </div>
          <div>
            <label className="block text-sm font-semibold mb-2">Avatar URL</label>
            <input
              type="url"
              value={formData.avatarUrl || ''}
              onChange={(e) => setFormData({ ...formData, avatarUrl: e.target.value })}
              className="w-full px-4 py-2 border border-accent-muted/30 rounded-lg bg-background text-text"
              placeholder="https://example.com/avatar.jpg"
            />
          </div>
        </div>
      </Card>

      <Card>
        <h2 className="text-xl font-semibold mb-4">Privacy Settings</h2>
        <div className="flex flex-col gap-4">
          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              checked={formData.isProfilePublic ?? true}
              onChange={(e) => setFormData({ ...formData, isProfilePublic: e.target.checked })}
              className="w-5 h-5"
            />
            <span className="text-sm sm:text-base">Make profile public</span>
          </label>
          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              checked={formData.showStatsToFriendsOnly ?? false}
              onChange={(e) => setFormData({ ...formData, showStatsToFriendsOnly: e.target.checked })}
              className="w-5 h-5"
            />
            <span className="text-sm sm:text-base">Show stats to friends only</span>
          </label>
        </div>
      </Card>

      <button
        onClick={handleSave}
        disabled={saving}
        className="px-6 py-3 bg-accent text-white rounded-lg hover:bg-accent/90 transition-colors disabled:opacity-50 text-sm sm:text-base"
      >
        {saving ? 'Saving...' : 'Save Changes'}
      </button>
    </div>
  )
}

export default ProfileSettingsPage

