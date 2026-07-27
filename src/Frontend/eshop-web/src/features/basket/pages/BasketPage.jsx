import { Link } from 'react-router-dom'
import { useEffect, useState } from 'react'
import ConfirmDialog from '../../../components/common/ConfirmDialog.jsx'
import ErrorMessage from '../../../components/common/ErrorMessage.jsx'
import Loading from '../../../components/common/Loading.jsx'
import SuccessMessage from '../../../components/common/SuccessMessage.jsx'
import { getProductById } from '../../catalog/services/catalogService.js'
import BasketItem from '../components/BasketItem.jsx'
import { useBasket } from '../hooks/useBasket.js'

const DELETE_BASKET_ERROR_MESSAGE = 'No fue posible eliminar el carrito.'
const UPDATE_ITEM_ERROR_MESSAGE = 'No fue posible actualizar el producto del carrito.'
const REMOVE_ITEM_ERROR_MESSAGE = 'No fue posible eliminar el producto del carrito.'

const priceFormatter = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
})

function BasketPage() {
  const { error, items, loading, totalPrice, clearBasket, updateItemQuantity, removeItem } = useBasket()
  const [clearingBasket, setClearingBasket] = useState(false)
  const [updatingProductId, setUpdatingProductId] = useState(null)
  const [removingProductId, setRemovingProductId] = useState(null)
  const [deleteError, setDeleteError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)
  const [stockByProductId, setStockByProductId] = useState({})
  const [stockUnavailableIds, setStockUnavailableIds] = useState(() => new Set())
  const [productToRemove, setProductToRemove] = useState(null)
  const [showClearDialog, setShowClearDialog] = useState(false)

  useEffect(() => {
    if (items.length === 0) {
      return undefined
    }

    const controller = new AbortController()
    const productIds = Array.from(new Set(items.map((item) => item.productId).filter(Boolean)))

    async function loadStocks() {
      const results = await Promise.allSettled(
        productIds.map((productId) => getProductById(productId, controller.signal)),
      )

      if (controller.signal.aborted) {
        return
      }

      const nextStockByProductId = {}
      const nextUnavailableIds = new Set()

      results.forEach((result, index) => {
        const productId = productIds[index]

        if (result.status === 'fulfilled' && Number.isInteger(result.value?.stock)) {
          nextStockByProductId[productId] = result.value.stock
          return
        }

        nextUnavailableIds.add(productId)
      })

      setStockByProductId(nextStockByProductId)
      setStockUnavailableIds(nextUnavailableIds)
    }

    loadStocks()

    return () => {
      controller.abort()
    }
  }, [items])

  async function handleConfirmClearBasket() {
    try {
      setClearingBasket(true)
      setDeleteError(null)
      setSuccessMessage(null)

      await clearBasket()
      setSuccessMessage('Carrito eliminado correctamente.')
    } catch {
      setDeleteError(DELETE_BASKET_ERROR_MESSAGE)
    } finally {
      setClearingBasket(false)
      setShowClearDialog(false)
    }
  }

  async function handleUpdateQuantity(item, quantity) {
    const stock = stockByProductId[item.productId]

    if (Number.isInteger(stock) && quantity > stock) {
      setDeleteError('La cantidad supera el stock disponible.')
      return
    }

    try {
      setUpdatingProductId(item.productId)
      setDeleteError(null)
      setSuccessMessage(null)

      await updateItemQuantity(item.productId, quantity)
    } catch {
      setDeleteError(UPDATE_ITEM_ERROR_MESSAGE)
    } finally {
      setUpdatingProductId(null)
    }
  }

  async function handleConfirmRemoveItem() {
    if (!productToRemove) {
      return
    }

    try {
      setRemovingProductId(productToRemove.productId)
      setDeleteError(null)
      setSuccessMessage(null)

      await removeItem(productToRemove.productId)
      setSuccessMessage('Producto eliminado del carrito correctamente.')
    } catch {
      setDeleteError(REMOVE_ITEM_ERROR_MESSAGE)
    } finally {
      setRemovingProductId(null)
      setProductToRemove(null)
    }
  }

  const hasItems = items.length > 0
  const currentError = error

  return (
    <section className="page-card basket-page">
      <div>
        <h1>Tu carrito</h1>
        <p>Consulta y administra los productos agregados al carrito.</p>
      </div>

      {loading && <Loading message="Cargando carrito..." />}

      {!loading && currentError && <ErrorMessage message={currentError} />}

      <SuccessMessage message={successMessage} />
      <ErrorMessage message={deleteError} />

      {!loading && !currentError && !hasItems && (
        <div className="basket-empty">
          <h2>Tu carrito está vacío</h2>
          <p>Explora el catálogo y agrega productos para ver aquí el resumen de tu compra.</p>
          <Link className="page-link" to="/">
            Volver al catálogo
          </Link>
        </div>
      )}

      {!loading && !currentError && hasItems && (
        <div className="basket">
          <ul className="basket__list" aria-label="Productos del carrito">
            {items.map((item) => (
              <li key={item.productId}>
                <BasketItem
                  item={item}
                  stock={stockByProductId[item.productId]}
                  stockUnavailable={stockUnavailableIds.has(item.productId)}
                  isUpdating={updatingProductId === item.productId}
                  isRemoving={removingProductId === item.productId}
                  onDecrease={() => handleUpdateQuantity(item, item.quantity - 1)}
                  onIncrease={() => handleUpdateQuantity(item, item.quantity + 1)}
                  onRemove={() => setProductToRemove(item)}
                />
              </li>
            ))}
          </ul>

          <aside className="basket__footer" aria-label="Resumen del carrito">
            <p className="basket__summary-label">Resumen del carrito</p>
            <p className="basket__total">{priceFormatter.format(totalPrice)}</p>
            <button
              className="basket__delete-button"
              type="button"
              disabled={clearingBasket}
              onClick={() => setShowClearDialog(true)}
            >
              {clearingBasket ? 'Vaciando...' : 'Vaciar carrito'}
            </button>
          </aside>
        </div>
      )}

      {productToRemove && (
        <ConfirmDialog
          title="Eliminar producto"
          message="¿Deseas eliminar este producto del carrito?"
          confirmLabel="Eliminar"
          loading={removingProductId === productToRemove.productId}
          onCancel={() => setProductToRemove(null)}
          onConfirm={handleConfirmRemoveItem}
        />
      )}

      {showClearDialog && (
        <ConfirmDialog
          title="Vaciar carrito"
          message="¿Deseas eliminar todos los productos del carrito?"
          confirmLabel="Vaciar carrito"
          loading={clearingBasket}
          onCancel={() => setShowClearDialog(false)}
          onConfirm={handleConfirmClearBasket}
        />
      )}
    </section>
  )
}

export default BasketPage
