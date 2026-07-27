import { Navigate, Outlet, useLocation } from 'react-router-dom'
import Loading from '../../../components/common/Loading.jsx'
import { useAuth } from '../hooks/useAuth.js'

function RoleProtectedRoute({ allowedRoles, children }) {
  const location = useLocation()
  const { initializing, isAuthenticated, user } = useAuth()

  if (initializing) {
    return <Loading message="Verificando permisos..." />
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  if (!allowedRoles.includes(user?.role)) {
    return <Navigate to="/" replace />
  }

  return children ?? <Outlet />
}

export default RoleProtectedRoute
