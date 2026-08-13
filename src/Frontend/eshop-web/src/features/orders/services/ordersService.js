import axios from 'axios'
import { ORDERS_API_URL } from '../../../config/apiConfig.js'

const ordersApi = axios.create({
  baseURL: ORDERS_API_URL,
})

export async function createOrder(accessToken, idempotencyKey, signal) {
  const response = await ordersApi.post(
    '/api/orders',
    {},
    {
      signal,
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'Idempotency-Key': idempotencyKey,
      },
    },
  )

  return response.data
}

export async function getOrdersByCustomer(customerId, accessToken, signal) {
  const response = await ordersApi.get(`/api/orders/customer/${customerId}`, {
    signal,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  return Array.isArray(response.data) ? response.data : []
}

export async function getOrderById(orderId, accessToken, signal) {
  const response = await ordersApi.get(`/api/orders/${orderId}`, {
    signal,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  return response.data
}

export async function getAllOrders(accessToken, search = '', signal) {
  const params = search.trim() ? { search: search.trim() } : undefined
  const response = await ordersApi.get('/api/orders', {
    params,
    signal,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  return Array.isArray(response.data) ? response.data : []
}

export async function updateOrderStatus(orderId, status, accessToken, signal) {
  const response = await ordersApi.patch(
    `/api/orders/${orderId}/status`,
    { status },
    {
      signal,
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    },
  )

  return response.data
}

export function getOrderStatusLabel(status) {
  if (status === 0 || status === 'Pending') {
    return 'Pendiente'
  }

  if (status === 1 || status === 'Confirmed') {
    return 'Confirmada'
  }

  if (status === 2 || status === 'Cancelled') {
    return 'Cancelada'
  }

  return 'Desconocido'
}

export function getOrderErrorMessage(error) {
  const status = error.response?.status
  const detail = error.response?.data?.detail || error.response?.data?.message

  if (!error.response) {
    return 'No fue posible comunicarse con el servicio de órdenes.'
  }

  if (status === 400) {
    return detail || 'No fue posible generar la orden con los datos actuales.'
  }

  if (status === 401) {
    return 'Tu sesión no es válida. Inicia sesión nuevamente.'
  }

  if (status === 403) {
    return 'No tienes autorización para realizar esta operación.'
  }

  if (status === 409) {
    return detail || 'La orden no puede procesarse por un conflicto de estado.'
  }

  return 'No fue posible completar la operación. Intenta nuevamente.'
}

export function getOrdersQueryErrorMessage(error) {
  const status = error.response?.status

  if (!error.response) {
    return 'No fue posible comunicarse con el servicio de órdenes.'
  }

  if (status === 401) {
    return 'Tu sesión no es válida. Inicia sesión nuevamente.'
  }

  if (status === 403) {
    return 'No tienes autorización para realizar esta operación.'
  }

  if (status === 404) {
    return 'La orden no fue encontrada.'
  }

  return 'No fue posible completar la operación. Intenta nuevamente.'
}

export function getOrderStatusUpdateErrorMessage(error) {
  const status = error.response?.status

  if (!error.response) {
    return 'No fue posible comunicarse con el servicio de órdenes.'
  }

  if (status === 401) {
    return 'Tu sesión no es válida. Inicia sesión nuevamente.'
  }

  if (status === 403) {
    return 'No tienes autorización para realizar esta operación.'
  }

  if (status === 404) {
    return 'La orden no fue encontrada.'
  }

  if (status === 409) {
    return 'No es posible realizar esta transición de estado.'
  }

  return 'No fue posible completar la operación. Intenta nuevamente.'
}

export function getTicketErrorMessage(error) {
  const status = error.response?.status

  if (!error.response) {
    return 'No fue posible comunicarse con el servicio de órdenes.'
  }

  if (status === 401) {
    return 'Tu sesión no es válida. Inicia sesión nuevamente.'
  }

  if (status === 403) {
    return 'No tienes autorización para realizar esta operación.'
  }

  if (status === 404) {
    return 'La orden no fue encontrada.'
  }

  return 'No fue posible completar la operación. Intenta nuevamente.'
}
