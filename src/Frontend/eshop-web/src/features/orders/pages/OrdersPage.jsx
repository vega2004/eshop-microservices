import axios from 'axios'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import ErrorMessage from '../../../components/common/ErrorMessage.jsx'
import Loading from '../../../components/common/Loading.jsx'
import { useAuth } from '../../auth/hooks/useAuth.js'
import {
  getOrderById,
  getOrdersByCustomer,
  getOrdersQueryErrorMessage,
  getOrderStatusLabel,
  getTicketErrorMessage,
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

function isCanceledRequest(error) {
  return axios.isCancel(error) || error.code === 'ERR_CANCELED'
}

function matchesStatusFilter(order, statusFilter) {
  if (statusFilter === 'all') {
    return true
  }

  return getOrderStatusLabel(order.status).toLowerCase() === statusFilter
}

function matchesSearch(order, searchTerm) {
  const normalizedSearchTerm = searchTerm.trim().toLowerCase()

  if (!normalizedSearchTerm) {
    return true
  }

  const searchableText = [
    order.orderNumber,
    getOrderStatusLabel(order.status),
    formatDate(order.createdAt),
  ]
    .join(' ')
    .toLowerCase()

  return searchableText.includes(normalizedSearchTerm)
}

function OrdersPage() {
  const { accessToken, user } = useAuth()
  const [orders, setOrders] = useState([])
  const [selectedOrder, setSelectedOrder] = useState(null)
  const [loading, setLoading] = useState(true)
  const [detailLoadingId, setDetailLoadingId] = useState(null)
  const [ticketLoadingId, setTicketLoadingId] = useState(null)
  const [error, setError] = useState(null)
  const [detailError, setDetailError] = useState(null)
  const [ticketError, setTicketError] = useState(null)
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState('all')

  useEffect(() => {
    const controller = new AbortController()

    async function loadOrders() {
      try {
        setLoading(true)
        setError(null)
        setDetailError(null)
        setTicketError(null)

        const customerOrders = await getOrdersByCustomer(
          user.id,
          accessToken,
          controller.signal,
        )

        setOrders(customerOrders)
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
  }, [accessToken, user.id])

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

  const filteredOrders = orders.filter(
    (order) => matchesStatusFilter(order, statusFilter) && matchesSearch(order, searchTerm),
  )

  return (
    <section className="page-card orders-page">
      <div>
        <h1>Mis órdenes</h1>
        <p>Consulta las compras realizadas con tu cuenta.</p>
      </div>

      {loading && <Loading message="Cargando órdenes..." />}

      {!loading && error && <ErrorMessage message={error} />}
      <ErrorMessage message={detailError} />
      <ErrorMessage message={ticketError} />

      {!loading && !error && orders.length === 0 && (
        <div className="orders-empty">
          <h2>No tienes órdenes registradas.</h2>
          <Link className="page-link" to="/">
            Ver productos
          </Link>
        </div>
      )}

      {!loading && !error && orders.length > 0 && (
        <>
          <div className="orders-filters" aria-label="Filtros de órdenes">
            <label className="orders-search">
              <span>Buscar órdenes</span>
              <input
                type="search"
                value={searchTerm}
                placeholder="Buscar por folio, estado o fecha..."
                onChange={(event) => setSearchTerm(event.target.value)}
              />
            </label>

            <div className="orders-status-filters" aria-label="Filtro por estado">
              <button
                type="button"
                className={statusFilter === 'all' ? 'orders-status-filters__button active' : 'orders-status-filters__button'}
                onClick={() => setStatusFilter('all')}
              >
                Todas
              </button>
              <button
                type="button"
                className={statusFilter === 'pendiente' ? 'orders-status-filters__button active' : 'orders-status-filters__button'}
                onClick={() => setStatusFilter('pendiente')}
              >
                Pendientes
              </button>
              <button
                type="button"
                className={statusFilter === 'confirmada' ? 'orders-status-filters__button active' : 'orders-status-filters__button'}
                onClick={() => setStatusFilter('confirmada')}
              >
                Confirmadas
              </button>
              <button
                type="button"
                className={statusFilter === 'cancelada' ? 'orders-status-filters__button active' : 'orders-status-filters__button'}
                onClick={() => setStatusFilter('cancelada')}
              >
                Canceladas
              </button>
            </div>
          </div>

          {filteredOrders.length === 0 && (
            <div className="orders-empty">
              <h2>No se encontraron órdenes con los filtros seleccionados.</h2>
            </div>
          )}

          {filteredOrders.length > 0 && (
            <div className="orders-layout">
              <ul className="orders-list" aria-label="Listado de órdenes">
                {filteredOrders.map((order) => (
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
                </article>
              </li>
            ))}
          </ul>

              {selectedOrder && (
            <aside className="order-detail" aria-label="Detalle de orden">
              <div>
                <p className="order-detail__label">Detalle de orden</p>
                <h2>Folio: {selectedOrder.orderNumber || 'Folio pendiente'}</h2>
                <p>{formatDate(selectedOrder.createdAt)}</p>
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
        </>
      )}
    </section>
  )
}

export default OrdersPage
