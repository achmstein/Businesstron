import { Navigate, Route, Routes } from 'react-router-dom'
import RequireAuth from './auth/RequireAuth'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import SearchesPage from './pages/SearchesPage'
import SearchDetailPage from './pages/SearchDetailPage'
import KeywordsPage from './pages/KeywordsPage'
import UsersPage from './pages/UsersPage'
import SettingsPage from './pages/SettingsPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <RequireAuth>
            <Layout />
          </RequireAuth>
        }
      >
        <Route path="/" element={<Navigate to="/searches" replace />} />
        <Route path="/searches" element={<SearchesPage />} />
        <Route path="/searches/:id" element={<SearchDetailPage />} />
        <Route path="/keywords" element={<KeywordsPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
