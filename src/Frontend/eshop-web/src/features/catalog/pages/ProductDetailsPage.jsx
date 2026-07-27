import { useEffect, useState } from 'react'
import axios from 'axios'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import ConfirmDialog from '../../../components/common/ConfirmDialog.jsx'
import ErrorMessage from '../../../components/common/ErrorMessage.jsx'
import Loading from '../../../components/common/Loading.jsx'
import SuccessMessage from '../../../components/common/SuccessMessage.jsx'
import { useAuth } from '../../auth/hooks/useAuth.js'
import { useBasket } from '../../basket/hooks/useBasket.js'
import ProductDetails from '../components/ProductDetails.jsx'
import { deleteProduct, getProductById, updateProductStock } from '../services/catalogService.js'

const invalidProductIdMessage = 'El identificador del producto no es válido.'
const productNotFoundMessage = 'El producto solicitado no fue encontrado.'
const productLoadErrorMessage =
  'No fue posible cargar el producto. Verifica que Catalog.API esté disponible.'
const addToBasketErrorMessage =
  'No fue posible agregar el producto al carrito. Verifica que Basket.API esté disponible.'
const sessionErrorMessage = 'Tu sesión no es válida o ha expirado. Inicia sesión nuevamente.'
const deleteProductErrorMessage = 'No fue posible eliminar el producto. Intenta nuevamente.'
const updateStockErrorMessage = 'No fue posible actualizar el stock. Intenta nuevamente.'

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
const emptyGuid = '00000000-0000-0000-0000-000000000000'

function isValidGuid(value) {
  return typeof value === 'string' && guidPattern.test(value) && value !== emptyGuid
}

function isCanceledRequest(error) {
  return error.name === 'AbortError' || error.code === 'ERR_CANCELED' || axios.isCancel(error)
}

function normalizeQuantity(value, stock) {
  const nextQuantity = Number(value)

  if (!Number.isInteger(nextQuantity) || nextQuantity < 1) {
    return 1
  }

  if (Number.isInteger(stock) && stock >= 0) {
    return Math.min(nextQuantity, Math.max(stock, 1))
  }

  return nextQuantity
}

function getProductLoadErrorMessage(error) {
  if (error.response?.status === 404) {
    return productNotFoundMessage
  }

  if (error.response?.status === 400) {
    return invalidProductIdMessage
  }

  return productLoadErrorMessage
}

function ProductDetailsPage() {
  const { productId } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, logout, user } = useAuth()
  const { addProduct } = useBasket()
  const [product, setProduct] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [adding, setAdding] = useState(false)
  const [basketMessage, setBasketMessage] = useState(null)
  const [basketError, setBasketError] = useState(null)
  const [deleting, setDeleting] = useState(false)
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const [quantity, setQuantity] = useState(1)
  const [stockInput, setStockInput] = useState('')
  const [updatingStock, setUpdatingStock] = useState(false)
  const [stockMessage, setStockMessage] = useState(null)
  const [stockError, setStockError] = useState(null)
  const isProductIdValid = isValidGuid(productId)
  const canDelete = user?.role === 'Admin'

  useEffect(() => {
    if (!isProductIdValid) {
      return undefined
    }

    const controller = new AbortController()

    async function loadProduct() {
      try {
        setLoading(true)
        setError(null)
        setProduct(null)

        const loadedProduct = await getProductById(productId, controller.signal)
        setProduct(loadedProduct)
        setQuantity(normalizeQuantity(1, loadedProduct?.stock))
        setStockInput(Number.isInteger(loadedProduct?.stock) ? String(loadedProduct.stock) : '')
      } catch (requestError) {
        if (!isCanceledRequest(requestError)) {
          setProduct(null)
          setError(getProductLoadErrorMessage(requestError))
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }

    loadProduct()

    return () => {
      controller.abort()
    }
  }, [isProductIdValid, productId])

  async function handleAddToBasket() {
    if (!isAuthenticated) {
      navigate('/login', { state: { from: location } })
      return
    }

    if (!product) {
      return
    }

    try {
      setBasketMessage(null)
      setBasketError(null)
      setAdding(true)

      await addProduct(product, quantity)
      setBasketMessage('Producto agregado al carrito correctamente.')
    } catch (requestError) {
      if (requestError.response?.status === 401) {
        logout()
        navigate('/login', { replace: true, state: { from: location } })
        setBasketError(sessionErrorMessage)
        return
      }

      setBasketError(addToBasketErrorMessage)
    } finally {
      setAdding(false)
    }
  }

  function handleQuantityChange(nextQuantity) {
    if (!product) {
      return
    }

    setQuantity(normalizeQuantity(nextQuantity, product.stock))
  }

  async function handleUpdateStock(event) {
    event.preventDefault()

    if (!product) {
      return
    }

    const nextStock = Number(stockInput)

    if (!Number.isInteger(nextStock) || nextStock < 0) {
      setStockMessage(null)
      setStockError('El stock debe ser un entero mayor o igual a cero.')
      return
    }

    try {
      setUpdatingStock(true)
      setStockMessage(null)
      setStockError(null)

      const updatedProduct = await updateProductStock(product.id, nextStock)

      setProduct(updatedProduct)
      setQuantity((currentQuantity) => normalizeQuantity(currentQuantity, updatedProduct.stock))
      setStockInput(String(updatedProduct.stock))
      setStockMessage('Stock actualizado correctamente.')
    } catch (requestError) {
      if (requestError.response?.status === 401) {
        logout()
        setStockError('Tu sesión expiró. Inicia sesión nuevamente.')
        navigate('/login', { replace: true, state: { from: location } })
        return
      }

      if (requestError.response?.status === 403) {
        setStockError('No tienes permisos para realizar esta acción.')
        return
      }

      if (requestError.response?.status === 404) {
        setStockError('El producto ya no existe en el catálogo.')
        return
      }

      setStockError(updateStockErrorMessage)
    } finally {
      setUpdatingStock(false)
    }
  }

  function handleRequestDeleteProduct() {
    if (!product) {
      return
    }

    setShowDeleteDialog(true)
  }

  async function handleConfirmDeleteProduct() {
    if (!product) {
      return
    }

    try {
      setBasketMessage(null)
      setBasketError(null)
      setDeleting(true)

      await deleteProduct(product.id)
      navigate('/', { replace: true })
    } catch (requestError) {
      if (requestError.response?.status === 401) {
        logout()
        setBasketError('Tu sesión expiró. Inicia sesión nuevamente.')
        navigate('/login', { replace: true, state: { from: location } })
        return
      }

      if (requestError.response?.status === 403) {
        setBasketError('No tienes permisos para realizar esta acción.')
        setShowDeleteDialog(false)
        return
      }

      if (requestError.response?.status === 404) {
        setBasketError('El producto ya no existe en el catálogo.')
        setShowDeleteDialog(false)
        return
      }

      setBasketError(deleteProductErrorMessage)
      setShowDeleteDialog(false)
    } finally {
      setDeleting(false)
    }
  }

  return (
    <section className="page-card product-details-page">
      {isProductIdValid && loading && <Loading message="Cargando producto..." />}

      {!isProductIdValid && <ErrorMessage message={invalidProductIdMessage} />}

      {isProductIdValid && !loading && error && <ErrorMessage message={error} />}

      <SuccessMessage message={basketMessage} />
      <ErrorMessage message={basketError} />

      {isProductIdValid && !loading && !error && product && (
        <ProductDetails
          product={product}
          quantity={quantity}
          onQuantityChange={handleQuantityChange}
          onAddToBasket={handleAddToBasket}
          isAdding={adding}
          canDelete={canDelete}
          onDeleteProduct={handleRequestDeleteProduct}
          isDeleting={deleting}
        />
      )}

      {isProductIdValid && !loading && !error && product && canDelete && (
        <section className="inventory-admin" aria-labelledby="inventory-admin-title">
          <div>
            <span className="inventory-admin__eyebrow">Administrar inventario</span>
            <h2 id="inventory-admin-title">Administrar inventario</h2>
            <p>Stock actual: {Number.isInteger(product.stock) ? product.stock : 'No informado'}</p>
          </div>

          <SuccessMessage message={stockMessage} />
          <ErrorMessage message={stockError} />

          <form className="inventory-admin__form" onSubmit={handleUpdateStock} noValidate>
            <label htmlFor="product-stock-update">Nuevo stock</label>
            <input
              id="product-stock-update"
              type="number"
              min="0"
              step="1"
              value={stockInput}
              disabled={updatingStock}
              onChange={(event) => setStockInput(event.target.value)}
            />
            <button className="inventory-admin__button" type="submit" disabled={updatingStock}>
              {updatingStock ? 'Actualizando...' : 'Actualizar stock'}
            </button>
          </form>
        </section>
      )}

      {showDeleteDialog && (
        <ConfirmDialog
          title="Eliminar producto"
          message="¿Seguro que deseas eliminar este producto? Esta acción no se puede deshacer."
          confirmLabel="Eliminar producto"
          loading={deleting}
          onCancel={() => setShowDeleteDialog(false)}
          onConfirm={handleConfirmDeleteProduct}
        />
      )}
    </section>
  )
}

export default ProductDetailsPage
