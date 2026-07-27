import axios from 'axios'
import { BASKET_API_URL } from '../../../config/apiConfig.js'
import { getStoredAccessToken } from '../../auth/services/authStorage.js'

const basketApi = axios.create({
  baseURL: BASKET_API_URL,
})

basketApi.interceptors.request.use((config) => {
  const accessToken = getStoredAccessToken()

  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }

  return config
})

export async function getBasket(signal) {
  const response = await basketApi.get('/basket', { signal })

  return normalizeBasket(response.data.cart)
}

export async function storeBasket(cart, signal) {
  const response = await basketApi.post(
    '/basket',
    {
      cart: {
        items: cart.items,
      },
    },
    { signal },
  )

  return response.data
}

export async function deleteBasket(signal) {
  const response = await basketApi.delete('/basket', { signal })

  return response.data
}

function getBasketTotal(items) {
  return items.reduce((total, item) => {
    const price = Number(item.price)
    const quantity = Number(item.quantity)

    if (!Number.isFinite(price) || !Number.isFinite(quantity)) {
      return total
    }

    return total + price * quantity
  }, 0)
}

function normalizeBasket(cart) {
  const items = Array.isArray(cart?.items) ? cart.items : []
  const totalPrice =
    typeof cart?.totalPrice === 'number' && Number.isFinite(cart.totalPrice)
      ? cart.totalPrice
      : getBasketTotal(items)

  return {
    ...cart,
    items,
    totalPrice,
  }
}

export async function addProductToBasket(product, quantity = 1, signal) {
  if (!Number.isInteger(quantity) || quantity <= 0) {
    throw new Error('La cantidad debe ser un entero mayor que cero.')
  }

  let cart

  try {
    cart = await getBasket(signal)
  } catch (error) {
    if (error.response?.status !== 404) {
      throw error
    }

    cart = {
      items: [],
    }
  }

  const items = Array.isArray(cart.items) ? [...cart.items] : []
  const existingItemIndex = items.findIndex((item) => item.productId === product.id)
  const stock = product.stock
  const hasValidStock = Number.isInteger(stock) && stock >= 0

  if (existingItemIndex >= 0) {
    const existingItem = items[existingItemIndex]
    const currentQuantity = Number.isInteger(existingItem.quantity) ? existingItem.quantity : 0
    const nextQuantity = currentQuantity + quantity

    if (hasValidStock && nextQuantity > stock) {
      throw new Error('La cantidad solicitada supera el stock disponible.')
    }

    items[existingItemIndex] = {
      ...existingItem,
      quantity: nextQuantity,
    }
  } else {
    if (hasValidStock && quantity > stock) {
      throw new Error('La cantidad solicitada supera el stock disponible.')
    }

    items.push({
      quantity,
      color: 'Estándar',
      price: Number(product.price),
      productId: product.id,
      productName: product.name,
    })
  }

  const updatedCart = {
    items,
    totalPrice: getBasketTotal(items),
  }

  await storeBasket(updatedCart, signal)

  return normalizeBasket(updatedCart)
}
