import { Link } from 'react-router-dom'
import { useEffect, useRef, useState } from 'react'
import ConfirmDialog from '../../../components/common/ConfirmDialog.jsx'
import ErrorMessage from '../../../components/common/ErrorMessage.jsx'
import Loading from '../../../components/common/Loading.jsx'
import SuccessMessage from '../../../components/common/SuccessMessage.jsx'
import { getProductById } from '../../catalog/services/catalogService.js'
import { useAuth } from '../../auth/hooks/useAuth.js'
import {
  createOrder,
  getOrderErrorMessage,
  getOrderStatusLabel,
  getTicketErrorMessage,
} from '../../orders/services/ordersService.js'
import { getOrderTicket, openTicketPdf } from '../../orders/services/ticketsService.js'
import BasketItem from '../components/BasketItem.jsx'
import { useBasket } from '../hooks/useBasket.js'

const DELETE_BASKET_ERROR_MESSAGE = 'No fue posible eliminar el carrito.'
const UPDATE_ITEM_ERROR_MESSAGE = 'No fue posible actualizar el producto del carrito.'
const REMOVE_ITEM_ERROR_MESSAGE = 'No fue posible eliminar el producto del carrito.'
const TAX_RATE = 0.16

const priceFormatter = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
})

function BasketPage() {
  const { error, items, loading, totalPrice, clearBasket, updateItemQuantity, removeItem } = useBasket()
  const { accessToken, isAuthenticated } = useAuth()
  const [clearingBasket, setClearingBasket] = useState(false)
  const [creatingOrder, setCreatingOrder] = useState(false)
  const [ticketLoading, setTicketLoading] = useState(false)
  const [updatingProductId, setUpdatingProductId] = useState(null)
  const [removingProductId, setRemovingProductId] = useState(null)
  const [deleteError, setDeleteError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)
  const [orderError, setOrderError] = useState(null)
  const [createdOrder, setCreatedOrder] = useState(null)
  const [stockByProductId, setStockByProductId] = useState({})
  const [stockUnavailableIds, setStockUnavailableIds] = useState(() => new Set())
  const [productToRemove, setProductToRemove] = useState(null)
  const [showClearDialog, setShowClearDialog] = useState(false)
  const idempotencyKeyRef = useRef(null)

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
      setOrderError(null)

      await clearBasket()
      setSuccessMessage('Carrito eliminado correctamente.')
      setCreatedOrder(null)
      idempotencyKeyRef.current = null
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
      setOrderError(null)
      setCreatedOrder(null)

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
      setOrderError(null)
      setCreatedOrder(null)

      await removeItem(productToRemove.productId)
      setSuccessMessage('Producto eliminado del carrito correctamente.')
    } catch {
      setDeleteError(REMOVE_ITEM_ERROR_MESSAGE)
    } finally {
      setRemovingProductId(null)
      setProductToRemove(null)
    }
  }

  async function handleCreateOrder() {
    if (!accessToken) {
      setOrderError('Inicia sesión para realizar la compra.')
      return
    }

    if (!hasItems) {
      setOrderError('Agrega productos al carrito antes de comprar.')
      return
    }

    if (!idempotencyKeyRef.current) {
      idempotencyKeyRef.current = `order-${crypto.randomUUID()}`
    }

    try {
      setCreatingOrder(true)
      setOrderError(null)
      setSuccessMessage(null)

      const order = await createOrder(accessToken, idempotencyKeyRef.current)
      setCreatedOrder(order)
      setSuccessMessage('Compra realizada correctamente.')
      idempotencyKeyRef.current = null
    } catch (requestError) {
      setOrderError(getOrderErrorMessage(requestError))
    } finally {
      setCreatingOrder(false)
    }
  }

  async function handleViewCreatedOrderTicket() {
    if (!createdOrder || !accessToken) {
      return
    }

    try {
      setTicketLoading(true)
      setOrderError(null)

      const ticket = await getOrderTicket(createdOrder.id, accessToken)
      openTicketPdf(ticket, createdOrder.orderNumber)
    } catch (requestError) {
      setOrderError(getTicketErrorMessage(requestError))
    } finally {
      setTicketLoading(false)
    }
  }

  const hasItems = items.length > 0
  const currentError = error
  const subtotal = Number.isFinite(totalPrice) ? totalPrice : 0
  const tax = subtotal * TAX_RATE
  const orderTotal = subtotal + tax
  const canCreateOrder = hasItems && isAuthenticated && Boolean(accessToken) && !creatingOrder

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
      <ErrorMessage message={orderError} />

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
            <dl className="basket__totals">
              <div>
                <dt>Subtotal</dt>
                <dd>{priceFormatter.format(subtotal)}</dd>
              </div>
              <div>
                <dt>Impuestos (16%)</dt>
                <dd>{priceFormatter.format(tax)}</dd>
              </div>
              <div className="basket__totals-grand-total">
                <dt>Total</dt>
                <dd>{priceFormatter.format(orderTotal)}</dd>
              </div>
            </dl>
            <button
              className="basket__checkout-button"
              type="button"
              disabled={!canCreateOrder}
              onClick={handleCreateOrder}
            >
              {creatingOrder ? 'Procesando...' : 'Realizar compra'}
            </button>
            <button
              className="basket__delete-button"
              type="button"
              disabled={clearingBasket || creatingOrder}
              onClick={() => setShowClearDialog(true)}
            >
              {clearingBasket ? 'Vaciando...' : 'Vaciar carrito'}
            </button>
          </aside>
        </div>
      )}

      {createdOrder && (
        <article className="order-confirmation" aria-label="Confirmación de orden">
          <div>
            <p className="order-confirmation__label">Orden creada</p>
            <h2>Compra realizada correctamente</h2>
          </div>

          <dl className="order-confirmation__summary">
            <div>
              <dt>Folio</dt>
              <dd>{createdOrder.orderNumber || 'Folio pendiente'}</dd>
            </div>
            <div>
              <dt>Fecha</dt>
              <dd>{new Date(createdOrder.createdAt).toLocaleString('es-MX')}</dd>
            </div>
            <div>
              <dt>Estado</dt>
              <dd>{getOrderStatusLabel(createdOrder.status)}</dd>
            </div>
            <div>
              <dt>Subtotal</dt>
              <dd>{priceFormatter.format(Number(createdOrder.subtotal) || 0)}</dd>
            </div>
            <div>
              <dt>Impuestos</dt>
              <dd>{priceFormatter.format(Number(createdOrder.tax) || 0)}</dd>
            </div>
            <div>
              <dt>Total</dt>
              <dd>{priceFormatter.format(Number(createdOrder.total) || 0)}</dd>
            </div>
          </dl>

          <ul className="order-confirmation__items" aria-label="Productos de la orden">
            {(createdOrder.items ?? []).map((item) => (
              <li key={item.productId}>
                <span>{item.productName}</span>
                <span>
                  {item.quantity} x {priceFormatter.format(Number(item.unitPrice) || 0)} ={' '}
                  {priceFormatter.format(Number(item.lineTotal) || 0)}
                </span>
              </li>
            ))}
          </ul>

          <div className="order-confirmation__actions">
            <Link className="page-link" to="/orders">
              Ver mis órdenes
            </Link>
            <button
              className="order-card__button"
              type="button"
              disabled={ticketLoading}
              onClick={handleViewCreatedOrderTicket}
            >
              {ticketLoading ? 'Generando...' : 'Ver ticket PDF'}
            </button>
          </div>
        </article>
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
