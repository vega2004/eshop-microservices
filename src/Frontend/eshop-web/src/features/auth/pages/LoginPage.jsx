import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import ErrorMessage from '../../../components/common/ErrorMessage.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { useState } from 'react'

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function getLoginErrorMessage(error) {
  if (error.response?.status === 401) {
    return 'Correo electrónico o contraseña incorrectos.'
  }

  if (error.response?.status === 400) {
    return 'Revisa los datos ingresados.'
  }

  return 'No fue posible iniciar sesión. Verifica que Auth.API esté disponible.'
}

function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setError(null)

    if (!email.trim() || !emailPattern.test(email.trim()) || !password) {
      setError('Revisa los datos ingresados.')
      return
    }

    try {
      setSubmitting(true)
      await login({ email: email.trim(), password })

      const redirectTo = location.state?.from?.pathname ?? '/'
      navigate(redirectTo, { replace: true })
    } catch (requestError) {
      setError(getLoginErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="auth-page">
      <div className="auth-card">
        <h1>Iniciar sesión</h1>
        <p>Accede de forma segura para administrar tu carrito de compras.</p>

        <ErrorMessage message={error} />

        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          <div className="auth-form__field">
            <label htmlFor="login-email">Correo electrónico</label>
            <input
              id="login-email"
              type="email"
              value={email}
              placeholder="correo@ejemplo.com"
              autoComplete="email"
              disabled={submitting}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>

          <div className="auth-form__field">
            <label htmlFor="login-password">Contraseña</label>
            <input
              id="login-password"
              type="password"
              value={password}
              placeholder="Tu contraseña"
              autoComplete="current-password"
              disabled={submitting}
              onChange={(event) => setPassword(event.target.value)}
            />
          </div>

          <button className="auth-form__button" type="submit" disabled={submitting}>
            {submitting ? 'Iniciando sesión...' : 'Iniciar sesión'}
          </button>
        </form>

        <div className="auth-links">
          <Link to="/register">Crear una cuenta</Link>
          <Link to="/">Volver al catálogo</Link>
        </div>
      </div>
    </section>
  )
}

export default LoginPage
