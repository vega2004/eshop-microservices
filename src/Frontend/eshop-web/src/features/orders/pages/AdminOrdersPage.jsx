import axios from 'axios'
import { useEffect, useState } from 'react'
import ErrorMessage from '../../../components/common/ErrorMessage.jsx'
import Loading from '../../../components/common/Loading.jsx'
import SuccessMessage from '../../../components/common/SuccessMessage.jsx'
import { useAuth } from '../../auth/hooks/useAuth.js'
import {
  getAllOrders,
  getOrderById,
  getOrderStatusLabel,
  getOrderStatusUpdateErrorMessage,
  getOrdersQueryErrorMessage,
  getTicketErrorMessage,
  updateOrderStatus,
} from '../services/ordersService.js'
import { getOrderTicket, openTicketPdf } from '../services/ticketsService.js'

const priceFormatter = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
})

function formatPrice(value) {
  const amount = Number(value)

  return Number.isFinite(amount) ? priceFormatter.format(amount) : priceFormatter.format(0)
}

function formatDate(value) {
  const date = new Date(value)

  return Number.isFinite(date.getTime()) ? date.toLocaleString('es-MX') : 'Fecha no disponible'
}

function getItemCount(order) {
  return (order.items ?? []).reduce((total, item) => {
    const quantity = Number(item.quantity)

    return Number.isFinite(quantity) ? total + quantity : total
  }, 0)
}

function isPending(order) {
  return order.status === 0 || order.status === 'Pending'
}

function isCanceledRequest(error) {
  return axios.isCancel(error) || error.code === 'ERR_CANCELED'
}

function AdminOrdersPage() {
  const { accessToken } = useAuth()
  const [orders, setOrders] = useState([])
  const [selectedOrder, setSelectedOrder] = useState(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [appliedSearch, setAppliedSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [detailLoadingId, setDetailLoadingId] = useState(null)
  const [ticketLoadingId, setTicketLoadingId] = useState(null)
  const [statusLoadingId, setStatusLoadingId] = useState(null)
  const [error, setError] = useState(null)
  const [detailError, setDetailError] = useState(null)
  const [ticketError, setTicketError] = useState(null)
  const [statusError, setStatusError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  useEffect(() => {
    const controller = new AbortController()

    async function loadOrders() {
      try {
        setLoading(true)
        setError(null)

        const allOrders = await getAllOrders(accessToken, appliedSearch, controller.signal)
        setOrders(allOrders)
      } catch (requestError) {
        if (!isCanceledRequest(requestError)) {
          setError(getOrdersQueryErrorMessage(requestError))
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }

    loadOrders()

    return () => {
      controller.abort()
    }
  }, [accessToken, appliedSearch])

  function handleSearchSubmit(event) {
    event.preventDefault()
    setAppliedSearch(searchTerm.trim())
    setSelectedOrder(null)
  }

  function handleClearSearch() {
    setSearchTerm('')
    setAppliedSearch('')
    setSelectedOrder(null)
  }

  async function handleViewDetail(orderId) {
    try {
      setDetailLoadingId(orderId)
      setDetailError(null)

      const order = await getOrderById(orderId, accessToken)
      setSelectedOrder(order)
    } catch (requestError) {
      setDetailError(getOrdersQueryErrorMessage(requestError))
    } finally {
      setDetailLoadingId(null)
    }
  }

  async function handleViewTicket(order) {
    try {
      setTicketLoadingId(order.id)
      setTicketError(null)

      const ticket = await getOrderTicket(order.id, accessToken)
      openTicketPdf(ticket, order.orderNumber)
    } catch (requestError) {
      setTicketError(getTicketErrorMessage(requestError))
    } finally {
      setTicketLoadingId(null)
    }
  }

  async function handleUpdateStatus(order, status) {
    try {
      setStatusLoadingId(order.id)
      setStatusError(null)
      setSuccessMessage(null)

      const updatedOrder = await updateOrderStatus(order.id, status, accessToken)
      setOrders((currentOrders) =>
        currentOrders.map((currentOrder) =>
          currentOrder.id === updatedOrder.id ? updatedOrder : currentOrder,
        ),
      )
      setSelectedOrder((currentOrder) =>
        currentOrder?.id === updatedOrder.id ? updatedOrder : currentOrder,
      )
      setSuccessMessage(
        status === 'Confirmed' ? 'Orden confirmada correctamente.' : 'Orden cancelada correctamente.',
      )
    } catch (requestError) {
      setStatusError(getOrderStatusUpdateErrorMessage(requestError))
    } finally {
      setStatusLoadingId(null)
    }
  }

  return (
    <section className="page-card orders-page">
      <div>
        <h1>Gestión de órdenes</h1>
        <p>Consulta y valida las órdenes registradas por los clientes.</p>
      </div>

      <form className="orders-filters" onSubmit={handleSearchSubmit}>
        <label className="orders-search">
          <span>Buscar por folio, usuario, correo o cliente...</span>
          <input
            type="search"
            value={searchTerm}
            placeholder="ORD-20260812-A7F3K2, cliente o correo"
            onChange={(event) => setSearchTerm(event.target.value)}
          />
        </label>
        <div className="orders-admin-actions">
          <button className="order-card__button" type="submit">
            Buscar
          </button>
          <button className="orders-status-filters__button" type="button" onClick={handleClearSearch}>
            Limpiar
          </button>
        </div>
      </form>

      {loading && <Loading message="Cargando órdenes..." />}
      {!loading && error && <ErrorMessage message={error} />}
      <ErrorMessage message={detailError} />
      <ErrorMessage message={ticketError} />
      <ErrorMessage message={statusError} />
      <SuccessMessage message={successMessage} />

      {!loading && !error && orders.length === 0 && (
        <div className="orders-empty">
          <h2>
            {appliedSearch
              ? 'No se encontraron órdenes con los criterios indicados.'
              : 'No hay órdenes registradas.'}
          </h2>
        </div>
      )}

      {!loading && !error && orders.length > 0 && (
        <div className="orders-layout">
          <ul className="orders-list" aria-label="Listado administrativo de órdenes">
            {orders.map((order) => (
              <li key={order.id}>
                <article className="order-card">
                  <div className="order-card__header">
                    <div>
                      <h2>Orden #{order.orderNumber || 'Folio pendiente'}</h2>
                      <p>{formatDate(order.createdAt)}</p>
                    </div>
                    <span className="order-card__status">{getOrderStatusLabel(order.status)}</span>
                  </div>

                  <dl className="order-card__summary">
                    <div>
                      <dt>Usuario</dt>
                      <dd>{order.customerUserName || 'No disponible'}</dd>
                    </div>
                    <div>
                      <dt>Correo</dt>
                      <dd>{order.customerEmail || 'No disponible'}</dd>
                    </div>
                    <div>
                      <dt>Productos</dt>
                      <dd>{getItemCount(order)}</dd>
                    </div>
                    <div>
                      <dt>Subtotal</dt>
                      <dd>{formatPrice(order.subtotal)}</dd>
                    </div>
                    <div>
                      <dt>Impuestos</dt>
                      <dd>{formatPrice(order.tax)}</dd>
                    </div>
                    <div>
                      <dt>Total</dt>
                      <dd>{formatPrice(order.total)}</dd>
                    </div>
                  </dl>

                  <div className="order-card__actions">
                    <button
                      className="order-card__button"
                      type="button"
                      disabled={detailLoadingId === order.id}
                      onClick={() => handleViewDetail(order.id)}
                    >
                      {detailLoadingId === order.id ? 'Cargando...' : 'Ver detalle'}
                    </button>
                    <button
                      className="order-card__button"
                      type="button"
                      disabled={ticketLoadingId === order.id}
                      onClick={() => handleViewTicket(order)}
                    >
                      {ticketLoadingId === order.id ? 'Generando...' : 'Ver ticket PDF'}
                    </button>
                    {isPending(order) && (
                      <>
                        <button
                          className="order-card__button"
                          type="button"
                          disabled={statusLoadingId === order.id}
                          onClick={() => handleUpdateStatus(order, 'Confirmed')}
                        >
                          Confirmar orden
                        </button>
                        <button
                          className="basket__delete-button"
                          type="button"
                          disabled={statusLoadingId === order.id}
                          onClick={() => handleUpdateStatus(order, 'Cancelled')}
                        >
                          Cancelar orden
                        </button>
                      </>
                    )}
                  </div>
                </article>
              </li>
            ))}
          </ul>

          {selectedOrder && (
            <aside className="order-detail" aria-label="Detalle administrativo de orden">
              <div>
                <p className="order-detail__label">Detalle de orden</p>
                <h2>Folio: {selectedOrder.orderNumber || 'Folio pendiente'}</h2>
                <p>Usuario: {selectedOrder.customerUserName || 'No disponible'}</p>
                <p>Correo: {selectedOrder.customerEmail || 'No disponible'}</p>
                <p>Fecha: {formatDate(selectedOrder.createdAt)}</p>
                <p>Estado: {getOrderStatusLabel(selectedOrder.status)}</p>
              </div>

              <div>
                <h3>Productos</h3>
                <ul className="order-detail__items">
                  {(selectedOrder.items ?? []).map((item) => (
                    <li key={item.productId}>
                      <span>{item.productName}</span>
                      <span>
                        {item.quantity} x {formatPrice(item.unitPrice)} = {formatPrice(item.lineTotal)}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>

              <dl className="order-detail__totals">
                <div>
                  <dt>Subtotal</dt>
                  <dd>{formatPrice(selectedOrder.subtotal)}</dd>
                </div>
                <div>
                  <dt>Impuestos</dt>
                  <dd>{formatPrice(selectedOrder.tax)}</dd>
                </div>
                <div>
                  <dt>Total</dt>
                  <dd>{formatPrice(selectedOrder.total)}</dd>
                </div>
              </dl>

              <button
                className="order-card__button"
                type="button"
                disabled={ticketLoadingId === selectedOrder.id}
                onClick={() => handleViewTicket(selectedOrder)}
              >
                {ticketLoadingId === selectedOrder.id ? 'Generando...' : 'Ver ticket PDF'}
              </button>
            </aside>
          )}
        </div>
      )}
    </section>
  )
}

export default AdminOrdersPage
