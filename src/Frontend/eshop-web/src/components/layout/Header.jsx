import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../../features/auth/hooks/useAuth.js'
import { useBasket } from '../../features/basket/hooks/useBasket.js'

function Header() {
  const navigate = useNavigate()
  const { isAuthenticated, logout, user } = useAuth()
  const { itemCount, loading } = useBasket()
  const isAdmin = user?.role === 'Admin'

  function handleLogout() {
    logout()
    navigate('/')
  }

  return (
    <header className="site-header">
      <div className="site-header__content">
        <Link className="site-header__brand" to="/" aria-label="Ir al catálogo de E-Shop">
          E-Shop
        </Link>
        <nav className="site-nav" aria-label="Navegación principal">
          <NavLink to="/" end>
            Catálogo
          </NavLink>
          {isAdmin && <NavLink to="/admin/products/new">Nuevo producto</NavLink>}
          {isAdmin && <NavLink to="/admin/orders">Gestión de órdenes</NavLink>}
          {isAuthenticated ? (
            <>
              <NavLink to="/basket">
                Carrito{' '}
                <span
                  className="site-nav__basket-count"
                  aria-label={`Productos en el carrito: ${loading ? 'cargando' : itemCount}`}
                >
                  {loading ? '...' : itemCount}
                </span>
              </NavLink>
              <NavLink to="/orders">Mis órdenes</NavLink>
              <span className="site-nav__user">{user?.userName}</span>
              <button className="site-nav__button" type="button" onClick={handleLogout}>
                Cerrar sesión
              </button>
            </>
          ) : (
            <>
              <NavLink to="/login">Iniciar sesión</NavLink>
              <NavLink to="/register">Registrarse</NavLink>
            </>
          )}
        </nav>
      </div>
    </header>
  )
}

export default Header
