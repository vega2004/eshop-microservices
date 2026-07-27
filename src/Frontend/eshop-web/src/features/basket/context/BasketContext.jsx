import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import axios from 'axios'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/hooks/useAuth.js'
import { addProductToBasket, deleteBasket, getBasket, storeBasket } from '../services/basketService.js'
import { BasketContext } from './basketContextValue.js'

const BASKET_ERROR_MESSAGE = 'No fue posible cargar el carrito. Verifica que Basket.API esté disponible.'

function isCanceledRequest(error) {
  return axios.isCancel(error) || error.code === 'ERR_CANCELED'
}

function isUnauthorized(error) {
  return error.response?.status === 401
}

function getBasketTotal(basket) {
  if (typeof basket?.totalPrice === 'number' && Number.isFinite(basket.totalPrice)) {
    return basket.totalPrice
  }

  return basket?.items?.reduce((total, item) => {
    const price = Number(item.price)
    const quantity = Number(item.quantity)

    if (!Number.isFinite(price) || !Number.isFinite(quantity)) {
      return total
    }

    return total + price * quantity
  }, 0) ?? 0
}

function getBasketItemCount(basket) {
  return basket?.items?.reduce((total, item) => {
    const quantity = Number(item.quantity)
    return Number.isFinite(quantity) ? total + quantity : total
  }, 0) ?? 0
}

function buildBasket(items) {
  return {
    items,
    totalPrice: getBasketTotal({ items }),
  }
}

export function BasketProvider({ children }) {
  const navigate = useNavigate()
  const location = useLocation()
  const locationRef = useRef(location)
  const { accessToken, isAuthenticated, initializing, logout } = useAuth()
  const [basket, setBasket] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const basketItems = useMemo(() => basket?.items ?? [], [basket])

  useEffect(() => {
    locationRef.current = location
  }, [location])

  const handleUnauthorized = useCallback(() => {
    setBasket(null)
    logout()
    navigate('/login', { replace: true, state: { from: locationRef.current } })
  }, [logout, navigate])

  const loadBasket = useCallback(
    async (signal) => {
      if (!isAuthenticated) {
        setBasket(null)
        setError(null)
        setLoading(false)
        return null
      }

      try {
        setLoading(true)
        setError(null)

        const currentBasket = await getBasket(signal)
        setBasket(currentBasket)

        return currentBasket
      } catch (requestError) {
        if (isCanceledRequest(requestError)) {
          return null
        }

        if (isUnauthorized(requestError)) {
          handleUnauthorized()
          throw requestError
        }

        if (requestError.response?.status === 404) {
          setBasket(null)
          return null
        }

        setError(BASKET_ERROR_MESSAGE)
        throw requestError
      } finally {
        if (!signal?.aborted) {
          setLoading(false)
        }
      }
    },
    [handleUnauthorized, isAuthenticated],
  )

  useEffect(() => {
    if (initializing) {
      return undefined
    }

    const controller = new AbortController()

    queueMicrotask(() => {
      if (!controller.signal.aborted) {
        loadBasket(controller.signal).catch(() => {})
      }
    })

    return () => {
      controller.abort()
    }
  }, [accessToken, initializing, loadBasket])

  const addProduct = useCallback(
    async (product, quantity = 1) => {
      try {
        setError(null)

        const updatedBasket = await addProductToBasket(product, quantity)
        setBasket(updatedBasket)

        return updatedBasket
      } catch (requestError) {
        if (isUnauthorized(requestError)) {
          handleUnauthorized()
        }

        throw requestError
      }
    },
    [handleUnauthorized],
  )

  const removeItem = useCallback(
    async (productId) => {
      const currentItems = basketItems
      const items = currentItems
        .filter((item) => item.productId !== productId)
        .map((item) => ({ ...item }))

      try {
        setError(null)

        if (items.length > 0) {
          const updatedBasket = buildBasket(items)
          await storeBasket(updatedBasket)
          setBasket(updatedBasket)

          return updatedBasket
        }

        await deleteBasket()
        setBasket(null)

        return null
      } catch (requestError) {
        if (isUnauthorized(requestError)) {
          handleUnauthorized()
        }

        throw requestError
      }
    },
    [basketItems, handleUnauthorized],
  )

  const updateItemQuantity = useCallback(
    async (productId, quantity) => {
      if (!Number.isInteger(quantity)) {
        throw new Error('La cantidad debe ser un entero.')
      }

      if (quantity <= 0) {
        return removeItem(productId)
      }

      const currentItems = basketItems
      const itemExists = currentItems.some((item) => item.productId === productId)

      if (!itemExists) {
        throw new Error('El producto no existe en el carrito.')
      }

      const items = currentItems.map((item) =>
        item.productId === productId
          ? {
              ...item,
              quantity,
            }
          : { ...item },
      )
      const updatedBasket = buildBasket(items)

      try {
        setError(null)
        await storeBasket(updatedBasket)
        setBasket(updatedBasket)

        return updatedBasket
      } catch (requestError) {
        if (isUnauthorized(requestError)) {
          handleUnauthorized()
        }

        throw requestError
      }
    },
    [basketItems, handleUnauthorized, removeItem],
  )

  const clearBasket = useCallback(async () => {
    try {
      setError(null)

      await deleteBasket()
      setBasket(null)
    } catch (requestError) {
      if (isUnauthorized(requestError)) {
        handleUnauthorized()
      }

      throw requestError
    }
  }, [handleUnauthorized])

  const value = useMemo(
    () => ({
      basket,
      items: basketItems,
      itemCount: getBasketItemCount(basket),
      totalPrice: getBasketTotal(basket),
      loading,
      error,
      addProduct,
      updateItemQuantity,
      removeItem,
      clearBasket,
      refreshBasket: loadBasket,
    }),
    [addProduct, basket, basketItems, clearBasket, error, loadBasket, loading, removeItem, updateItemQuantity],
  )

  return <BasketContext.Provider value={value}>{children}</BasketContext.Provider>
}
