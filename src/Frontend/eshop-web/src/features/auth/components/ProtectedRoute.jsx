import { Navigate, Outlet, useLocation } from 'react-router-dom'
import Loading from '../../../components/common/Loading.jsx'
import { useAuth } from '../hooks/useAuth.js'

function ProtectedRoute({ children }) {
  const location = useLocation()
  const { initializing, isAuthenticated } = useAuth()

  if (initializing) {
    return <Loading message="Verificando sesión..." />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return children ?? <Outlet />
}

export default ProtectedRoute
