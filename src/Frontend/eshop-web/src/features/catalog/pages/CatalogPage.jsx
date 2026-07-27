import { useEffect, useMemo, useRef, useState } from 'react'
import axios from 'axios'
import { useLocation, useNavigate } from 'react-router-dom'
import ErrorMessage from '../../../components/common/ErrorMessage.jsx'
import Loading from '../../../components/common/Loading.jsx'
import SuccessMessage from '../../../components/common/SuccessMessage.jsx'
import { useAuth } from '../../auth/hooks/useAuth.js'
import { useBasket } from '../../basket/hooks/useBasket.js'
import CategoryFilter from '../components/CategoryFilter.jsx'
import Pagination from '../components/Pagination.jsx'
import ProductList from '../components/ProductList.jsx'
import ProductSearch from '../components/ProductSearch.jsx'
import { getProducts, getProductsByCategory } from '../services/catalogService.js'

const PRODUCTS_ERROR_MESSAGE =
  'No fue posible cargar los productos. Verifica que Catalog.API esté disponible.'
const CATEGORY_PRODUCTS_ERROR_MESSAGE =
  'No fue posible cargar los productos de la categoría seleccionada. Verifica que Catalog.API esté disponible.'
const ADD_TO_BASKET_ERROR_MESSAGE =
  'No fue posible agregar el producto al carrito. Verifica que Basket.API esté disponible.'
const LOGIN_REQUIRED_MESSAGE = 'Inicia sesión para agregar productos al carrito.'

function isCanceledRequest(error) {
  return error.name === 'AbortError' || error.code === 'ERR_CANCELED' || axios.isCancel(error)
}

function isUnauthorized(error) {
  return error.response?.status === 401
}

function getProductCategories(product) {
  if (!Array.isArray(product.category)) {
    return []
  }

  return product.category
    .map((category) => String(category).trim())
    .filter((category) => category.length > 0)
}

function mergeCategories(previousCategories, products) {
  const nextCategories = new Set(previousCategories)

  products.forEach((product) => {
    getProductCategories(product).forEach((category) => {
      nextCategories.add(category)
    })
  })

  const sortedCategories = Array.from(nextCategories).sort((first, second) =>
    first.localeCompare(second, 'es'),
  )

  if (
    sortedCategories.length === previousCategories.length &&
    sortedCategories.every((category, index) => category === previousCategories[index])
  ) {
    return previousCategories
  }

  return sortedCategories
}

function normalizeSearchValue(value) {
  return String(value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
}

function getSearchableText(product) {
  const categories = Array.isArray(product.category) ? product.category : [product.category]

  return normalizeSearchValue([product.name, product.description, ...categories].join(' '))
}

function CatalogPage() {
  const catalogRef = useRef(null)
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, logout } = useAuth()
  const { addProduct } = useBasket()
  const [products, setProducts] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [pageNumber, setPageNumber] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [totalCount, setTotalCount] = useState(0)
  const [addingProductId, setAddingProductId] = useState(null)
  const [basketMessage, setBasketMessage] = useState(null)
  const [basketError, setBasketError] = useState(null)
  const [availableCategories, setAvailableCategories] = useState([])
  const [selectedCategory, setSelectedCategory] = useState('')
  const [searchTerm, setSearchTerm] = useState('')

  const visibleProducts = useMemo(() => {
    const normalizedSearchTerm = normalizeSearchValue(searchTerm.trim())

    if (!normalizedSearchTerm) {
      return products
    }

    return products.filter((product) => getSearchableText(product).includes(normalizedSearchTerm))
  }, [products, searchTerm])

  useEffect(() => {
    const controller = new AbortController()

    async function loadProducts() {
      try {
        setLoading(true)
        setError(null)

        if (selectedCategory) {
          const categoryProducts = await getProductsByCategory(selectedCategory, controller.signal)

          setProducts(categoryProducts)
          setTotalCount(categoryProducts.length)
          return
        }

        const catalogProducts = await getProducts(pageNumber, pageSize, controller.signal)

        setProducts(catalogProducts.data)
        setPageSize(catalogProducts.pageSize)
        setTotalCount(catalogProducts.totalCount)
        setAvailableCategories((previousCategories) =>
          mergeCategories(previousCategories, catalogProducts.data),
        )
      } catch (requestError) {
        if (!isCanceledRequest(requestError)) {
          setError(selectedCategory ? CATEGORY_PRODUCTS_ERROR_MESSAGE : PRODUCTS_ERROR_MESSAGE)
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }

    loadProducts()

    return () => {
      controller.abort()
    }
  }, [pageNumber, pageSize, selectedCategory])

  function handleCategoryChange(category) {
    setError(null)
    setBasketMessage(null)
    setBasketError(null)
    setProducts([])
    setLoading(true)
    setSelectedCategory(category)
    setPageNumber(1)
  }

  function handlePageChange(newPageNumber) {
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

    if (newPageNumber < 1 || newPageNumber > totalPages || newPageNumber === pageNumber) {
      return
    }

    setError(null)
    setBasketMessage(null)
    setBasketError(null)
    setProducts([])
    setLoading(true)
    setPageNumber(newPageNumber)
    catalogRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  async function handleAddToBasket(product, quantity = 1) {
    if (!isAuthenticated) {
      setBasketMessage(null)
      setBasketError(LOGIN_REQUIRED_MESSAGE)
      navigate('/login', { state: { from: location } })
      return
    }

    try {
      setBasketMessage(null)
      setBasketError(null)
      setAddingProductId(product.id)

      await addProduct(product, quantity)
      setBasketMessage('Producto agregado al carrito correctamente.')
    } catch (requestError) {
      if (isUnauthorized(requestError)) {
        logout()
        navigate('/login', { replace: true, state: { from: location } })
        setBasketError('Tu sesión no es válida o ha expirado. Inicia sesión nuevamente.')
        return
      }

      setBasketError(ADD_TO_BASKET_ERROR_MESSAGE)
    } finally {
      setAddingProductId(null)
    }
  }

  return (
    <section className="catalog-page" ref={catalogRef}>
      <div className="catalog-hero" aria-labelledby="catalog-title">
        <div className="catalog-hero__content">
          <span className="catalog-hero__eyebrow">Catálogo tecnológico</span>
          <h1 id="catalog-title">Equipamiento digital para trabajar mejor</h1>
          <p>
            Productos organizados para comparar, elegir y agregar al carrito con una experiencia
            clara y profesional.
          </p>
          <span className="catalog-hero__cta">Explorar productos</span>
        </div>
      </div>

      <div className="catalog-tools" aria-label="Herramientas del catálogo">
        <div className="catalog-tools__controls">
          <CategoryFilter
            categories={availableCategories}
            selectedCategory={selectedCategory}
            onCategoryChange={handleCategoryChange}
            disabled={loading}
          />

          <ProductSearch
            searchTerm={searchTerm}
            resultCount={visibleProducts.length}
            totalCount={products.length}
            onSearchChange={setSearchTerm}
            onClear={() => setSearchTerm('')}
            disabled={loading}
          />
        </div>

        {!loading && !error && totalCount > 0 && selectedCategory && (
          <p className="catalog-tools__summary">{totalCount} productos en {selectedCategory}</p>
        )}
      </div>

      {loading && <Loading />}

      {!loading && error && <ErrorMessage message={error} />}

      <SuccessMessage message={basketMessage} />
      <ErrorMessage message={basketError} />

      {!loading && !error && products.length === 0 && selectedCategory && (
        <p className="status-message">Actualmente no hay productos disponibles en esta categoría.</p>
      )}

      {!loading && !error && products.length === 0 && !selectedCategory && (
        <p className="status-message">No hay productos disponibles en el catálogo.</p>
      )}

      {!loading && !error && products.length > 0 && visibleProducts.length === 0 && (
        <p className="status-message">No hay productos cargados que coincidan con la búsqueda.</p>
      )}

      {!loading && !error && visibleProducts.length > 0 && (
        <ProductList
          products={visibleProducts}
          onAddToBasket={handleAddToBasket}
          addingProductId={addingProductId}
        />
      )}

      {!error && !selectedCategory && totalCount > pageSize && (
        <Pagination
          pageNumber={pageNumber}
          pageSize={pageSize}
          totalCount={totalCount}
          onPageChange={handlePageChange}
          disabled={loading}
        />
      )}
    </section>
  )
}

export default CatalogPage
