const AUTH_STORAGE_KEY = 'eshop_auth'

function isValidExpiration(expiresAtUtc) {
  const expiresAt = new Date(expiresAtUtc).getTime()

  return Number.isFinite(expiresAt) && expiresAt > Date.now()
}

export function getStoredAuth() {
  const storedAuth = sessionStorage.getItem(AUTH_STORAGE_KEY)

  if (!storedAuth) {
    return null
  }

  try {
    const authData = JSON.parse(storedAuth)

    if (!authData?.accessToken || !isValidExpiration(authData.expiresAtUtc)) {
      clearStoredAuth()
      return null
    }

    return {
      user: authData.user ?? null,
      accessToken: authData.accessToken,
      expiresAtUtc: authData.expiresAtUtc,
    }
  } catch {
    clearStoredAuth()
    return null
  }
}

export function storeAuth(authData) {
  sessionStorage.setItem(
    AUTH_STORAGE_KEY,
    JSON.stringify({
      user: authData.user,
      accessToken: authData.accessToken,
      expiresAtUtc: authData.expiresAtUtc,
    }),
  )
}

export function clearStoredAuth() {
  sessionStorage.removeItem(AUTH_STORAGE_KEY)
}

export function getStoredAccessToken() {
  return getStoredAuth()?.accessToken ?? null
}
