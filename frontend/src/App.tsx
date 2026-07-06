import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './components/AppLayout'
import ProtectedRoute from './components/ProtectedRoute'
import { AuthProvider } from './context/AuthContext'
import LandingPage from './pages/LandingPage'
import LoginPage from './pages/LoginPage'
import PropertiesPage from './pages/PropertiesPage'
import RequestDetailPage from './pages/RequestDetailPage'
import RequestsPage from './pages/RequestsPage'
import RoleDashboardPage from './pages/RoleDashboardPage'
import TenantRegistrationPage from './pages/TenantRegistrationPage'
import TenantRegistrationsPage from './pages/TenantRegistrationsPage'
import UsersPage from './pages/UsersPage'

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="welcome" element={<LandingPage />} />
          <Route path="login" element={<LoginPage />} />
          <Route path="register" element={<TenantRegistrationPage />} />
          <Route
            element={
              <ProtectedRoute>
                <AppLayout />
              </ProtectedRoute>
            }
          >
            <Route index element={<RoleDashboardPage />} />
            <Route path="requests" element={<RequestsPage />} />
            <Route path="requests/:id" element={<RequestDetailPage />} />
            <Route
              path="properties"
              element={
                <ProtectedRoute allowedRoles={['AdminOwner', 'PropertyManager']}>
                  <PropertiesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="users"
              element={
                <ProtectedRoute allowedRoles={['AdminOwner']}>
                  <UsersPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="tenant-registrations"
              element={
                <ProtectedRoute allowedRoles={['AdminOwner', 'PropertyManager']}>
                  <TenantRegistrationsPage />
                </ProtectedRoute>
              }
            />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}

export default App
