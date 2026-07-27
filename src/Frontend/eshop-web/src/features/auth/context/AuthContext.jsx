import { useCallback, useMemo, useState } from 'react'
import { AuthContext } from './authContextValue.js'
import { loginUser, registerUser } from '../services/authService.js'
import { clearStoredAuth, getStoredAuth, storeAuth } from '../services/authStorage.js'

function isExpired(expiresAtUtc) {
  const expiresAt = new Date(expiresAtUtc).getTime()

  return !Number.isFinite(expiresAt) || expiresAt <= Date.now()
}

export function AuthProvider({ children }) {
  const [authData, setAuthData] = useState(() => getStoredAuth())

  const setAuthState = useCallback((authData) => {
    storeAuth(authData)
    setAuthData(authData)
  }, [])

  const login = useCallback(async (credentials) => {
    const authData = await loginUser(credentials)
    setAuthState(authData)

    return authData
  }, [setAuthState])

  const register = useCallback(async (credentials) => {
    const authData = await registerUser(credentials)
    setAuthState(authData)

    return authData
  }, [setAuthState])

  const logout = useCallback(() => {
    clearStoredAuth()
    setAuthData(null)
  }, [])

  const user = authData?.user ?? null
  const accessToken = authData?.accessToken ?? null
  const expiresAtUtc = authData?.expiresAtUtc ?? null
  const isAuthenticated = Boolean(accessToken) && !isExpired(expiresAtUtc)

  const value = useMemo(
    () => ({
      user,
      accessToken,
      expiresAtUtc,
      isAuthenticated,
      initializing: false,
      login,
      register,
      logout,
    }),
    [user, accessToken, expiresAtUtc, isAuthenticated, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
