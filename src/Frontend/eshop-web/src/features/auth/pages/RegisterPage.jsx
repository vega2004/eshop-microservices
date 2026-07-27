import { Link, Navigate, useNavigate } from 'react-router-dom'
import ErrorMessage from '../../../components/common/ErrorMessage.jsx'
import { useAuth } from '../hooks/useAuth.js'
import { useState } from 'react'

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/

function getRegisterErrorMessage(error) {
  if (error.response?.status === 409) {
    return 'El nombre de usuario o correo electrónico ya está registrado.'
  }

  if (error.response?.status === 400) {
    return 'Revisa los datos del registro.'
  }

  return 'No fue posible crear la cuenta. Verifica que Auth.API esté disponible.'
}

function RegisterPage() {
  const navigate = useNavigate()
  const { isAuthenticated, register } = useAuth()
  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  function validateForm() {
    const trimmedUserName = userName.trim()
    const trimmedEmail = email.trim()

    return (
      trimmedUserName.length >= 3 &&
      trimmedUserName.length <= 50 &&
      trimmedEmail.length > 0 &&
      trimmedEmail.length <= 200 &&
      emailPattern.test(trimmedEmail) &&
      passwordPattern.test(password) &&
      password === confirmPassword
    )
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setError(null)

    if (!validateForm()) {
      setError('Revisa los datos del registro.')
      return
    }

    try {
      setSubmitting(true)
      await register({
        userName: userName.trim(),
        email: email.trim(),
        password,
      })
      navigate('/', { replace: true })
    } catch (requestError) {
      setError(getRegisterErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="auth-page">
      <div className="auth-card">
        <h1>Crear cuenta</h1>
        <p>Crea una cuenta para guardar tus productos y comprar con mayor facilidad.</p>

        <ErrorMessage message={error} />

        <form className="auth-form" onSubmit={handleSubmit} noValidate>
          <div className="auth-form__field">
            <label htmlFor="register-username">Nombre de usuario</label>
            <input
              id="register-username"
              type="text"
              value={userName}
              placeholder="Tu nombre de usuario"
              autoComplete="username"
              disabled={submitting}
              onChange={(event) => setUserName(event.target.value)}
            />
          </div>

          <div className="auth-form__field">
            <label htmlFor="register-email">Correo electrónico</label>
            <input
              id="register-email"
              type="email"
              value={email}
              placeholder="correo@ejemplo.com"
              autoComplete="email"
              disabled={submitting}
              onChange={(event) => setEmail(event.target.value)}
            />
          </div>

          <div className="auth-form__field">
            <label htmlFor="register-password">Contraseña</label>
            <input
              id="register-password"
              type="password"
              value={password}
              placeholder="Crea una contraseña segura"
              autoComplete="new-password"
              disabled={submitting}
              onChange={(event) => setPassword(event.target.value)}
            />
          </div>

          <div className="auth-form__field">
            <label htmlFor="register-confirm-password">Confirmar contraseña</label>
            <input
              id="register-confirm-password"
              type="password"
              value={confirmPassword}
              placeholder="Repite tu contraseña"
              autoComplete="new-password"
              disabled={submitting}
              onChange={(event) => setConfirmPassword(event.target.value)}
            />
          </div>

          <p className="auth-form__hint">
            La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula y un número.
          </p>

          <button className="auth-form__button" type="submit" disabled={submitting}>
            {submitting ? 'Creando cuenta...' : 'Crear cuenta'}
          </button>
        </form>

        <div className="auth-links">
          <Link to="/login">Ya tengo una cuenta</Link>
          <Link to="/">Volver al catálogo</Link>
        </div>
      </div>
    </section>
  )
}

export default RegisterPage
