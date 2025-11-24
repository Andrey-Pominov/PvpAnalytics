import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import MainLayout from './layouts/MainLayout'
import StatsPage from './pages/StatsPage'
import PlayersPage from './pages/PlayersPage'
import PlayerProfilePage from './pages/PlayerProfilePage'
import MatchesPage from './pages/MatchesPage'
import UploadPage from './pages/UploadPage'
import CustomMetricsPage from './pages/CustomMetricsPage'
import ReportBuilderPage from './pages/ReportBuilderPage'
import MatchDetailPage from './pages/MatchDetailPage'

const App = () => {
  return (
    <BrowserRouter>
      <MainLayout>
        <Routes>
          <Route path="/" element={<StatsPage />} />
          <Route path="/players" element={<PlayersPage />} />
          <Route path="/players/:id" element={<PlayerProfilePage />} />
          <Route path="/matches" element={<MatchesPage />} />
          <Route path="/matches/:id" element={<MatchDetailPage />} />
          <Route path="/upload" element={<UploadPage />} />
          <Route path="/metrics" element={<CustomMetricsPage />} />
          <Route path="/reports" element={<ReportBuilderPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </MainLayout>
    </BrowserRouter>
  )
}

export default App
