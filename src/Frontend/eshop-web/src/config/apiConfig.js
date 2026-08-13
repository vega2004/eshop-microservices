const requiredApiEnv = {
  VITE_CATALOG_API_URL: import.meta.env.VITE_CATALOG_API_URL,
  VITE_BASKET_API_URL: import.meta.env.VITE_BASKET_API_URL,
  VITE_AUTH_API_URL: import.meta.env.VITE_AUTH_API_URL,
  VITE_ORDERS_API_URL: import.meta.env.VITE_ORDERS_API_URL,
  VITE_TICKETS_API_URL: import.meta.env.VITE_TICKETS_API_URL,
}

function getRequiredEnv(name) {
  const value = requiredApiEnv[name]

  if (!value) {
    throw new Error(
      `Application configuration error: missing required environment variable ${name}.`,
    )
  }

  return value
}

export const CATALOG_API_URL = getRequiredEnv('VITE_CATALOG_API_URL')
export const BASKET_API_URL = getRequiredEnv('VITE_BASKET_API_URL')
export const AUTH_API_URL = getRequiredEnv('VITE_AUTH_API_URL')
export const ORDERS_API_URL = getRequiredEnv('VITE_ORDERS_API_URL')
export const TICKETS_API_URL = getRequiredEnv('VITE_TICKETS_API_URL')

export const apiConfig = {
  catalogApiUrl: CATALOG_API_URL,
  basketApiUrl: BASKET_API_URL,
  authApiUrl: AUTH_API_URL,
  ordersApiUrl: ORDERS_API_URL,
  ticketsApiUrl: TICKETS_API_URL,
}
