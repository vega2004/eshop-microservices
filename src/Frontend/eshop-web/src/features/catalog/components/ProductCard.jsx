import { useState } from 'react'
import { Link } from 'react-router-dom'

function isValidImageUrl(value) {
  if (typeof value !== 'string') {
    return false
  }

  return value.startsWith('http://') || value.startsWith('https://')
}

function formatPrice(price) {
  if (typeof price !== 'number') {
    return 'Precio no disponible'
  }

  return new Intl.NumberFormat('es-MX', {
    style: 'currency',
    currency: 'MXN',
  }).format(price)
}

function getCategories(category) {
  if (Array.isArray(category)) {
    return category
  }

  return typeof category === 'string' && category.length > 0 ? [category] : []
}

function getStockMessage(stock) {
  if (!Number.isInteger(stock) || stock < 0) {
    return 'Disponibilidad no informada'
  }

  return stock === 0 ? 'Agotado' : `${stock} disponibles`
}

function ProductCard({ product, onAddToBasket, isAdding }) {
  const [imageFailed, setImageFailed] = useState(false)
  const name = product.name ?? 'Producto sin nombre'
  const description = product.description ?? 'Sin descripción disponible.'
  const categories = getCategories(product.category)
  const imageUrl = isValidImageUrl(product.imageFiles) && !imageFailed ? product.imageFiles : null
  const isOutOfStock = product.stock === 0

  return (
    <article className="product-card">
      <div className="product-card__media">
        {imageUrl ? (
          <img
            className="product-card__image"
            src={imageUrl}
            alt={name}
            loading="lazy"
            onError={() => setImageFailed(true)}
          />
        ) : (
          <div className="product-card__image-placeholder" aria-label="Imagen no disponible">
            Imagen no disponible
          </div>
        )}
      </div>

      <div className="product-card__body">
        <h2>{name}</h2>
        <p className="product-card__description">{description}</p>
        <strong className="product-card__price">{formatPrice(product.price)}</strong>
        <p className={isOutOfStock ? 'product-stock product-stock--empty' : 'product-stock'}>
          {getStockMessage(product.stock)}
        </p>

        {categories.length > 0 && (
          <ul className="product-card__categories" aria-label="Categorías">
            {categories.map((category) => (
              <li key={category}>{category}</li>
            ))}
          </ul>
        )}

        <div className="product-card__actions">
          <Link className="product-card__details-link" to={`/products/${product.id}`}>
            Ver detalle
          </Link>

          <button
            className="product-card__button"
            type="button"
            disabled={isAdding || isOutOfStock}
            onClick={() => onAddToBasket(product, 1)}
          >
            {isOutOfStock ? 'Producto agotado' : isAdding ? 'Agregando...' : 'Agregar al carrito'}
          </button>
        </div>
      </div>
    </article>
  )
}

export default ProductCard
