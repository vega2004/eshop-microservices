import { useState } from 'react'
import { Link } from 'react-router-dom'

const priceFormatter = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
})

function formatPrice(price) {
  return typeof price === 'number' && Number.isFinite(price)
    ? priceFormatter.format(price)
    : 'Precio no disponible'
}

function getCategories(category) {
  if (!Array.isArray(category)) {
    return []
  }

  return category
    .map((item) => String(item).trim())
    .filter((item) => item.length > 0)
}

function getStockMessage(stock) {
  if (!Number.isInteger(stock) || stock < 0) {
    return 'Disponibilidad no informada'
  }

  return stock === 0 ? 'Producto agotado' : `Disponibles: ${stock}`
}

function ProductDetails({
  product,
  quantity,
  onQuantityChange,
  onAddToBasket,
  isAdding,
  canDelete,
  onDeleteProduct,
  isDeleting,
}) {
  const [imageFailed, setImageFailed] = useState(false)
  const name = product.name ?? 'Producto sin nombre'
  const description = product.description ?? 'Sin descripción disponible.'
  const imageUrl = typeof product.imageFiles === 'string' ? product.imageFiles.trim() : ''
  const shouldShowImage = imageUrl.length > 0 && !imageFailed
  const categories = getCategories(product.category)
  const hasValidStock = Number.isInteger(product.stock) && product.stock >= 0
  const isOutOfStock = product.stock === 0
  const canSelectQuantity = hasValidStock && !isOutOfStock

  function handleQuantityInputChange(event) {
    const nextQuantity = Number(event.target.value)
    onQuantityChange(nextQuantity)
  }

  return (
    <article className="product-details">
      <div className="product-details__media">
        {shouldShowImage ? (
          <img
            className="product-details__image"
            src={imageUrl}
            alt={name}
            loading="lazy"
            onError={() => setImageFailed(true)}
          />
        ) : (
          <div className="product-details__image-placeholder" aria-label="Imagen no disponible">
            Imagen no disponible
          </div>
        )}
      </div>

      <div className="product-details__content">
        <h1>{name}</h1>
        <p className="product-details__description">{description}</p>
        <strong className="product-details__price">{formatPrice(product.price)}</strong>
        <p className={isOutOfStock ? 'product-stock product-stock--empty' : 'product-stock'}>
          {getStockMessage(product.stock)}
        </p>

        <section aria-label="Categorías del producto">
          <h2 className="product-details__section-title">Categorías</h2>
          {categories.length > 0 ? (
            <ul className="product-details__categories">
              {categories.map((category) => (
                <li key={category}>{category}</li>
              ))}
            </ul>
          ) : (
            <p className="product-details__empty-category">Sin categoría</p>
          )}
        </section>

        <div className="quantity-selector" aria-label="Seleccionar cantidad">
          <button
            className="quantity-selector__button"
            type="button"
            disabled={!canSelectQuantity || quantity <= 1 || isAdding}
            aria-label={`Disminuir cantidad de ${name}`}
            onClick={() => onQuantityChange(quantity - 1)}
          >
            -
          </button>
          <input
            className="quantity-selector__input"
            type="number"
            min="1"
            step="1"
            max={hasValidStock ? product.stock : undefined}
            value={quantity}
            disabled={!canSelectQuantity || isAdding}
            aria-label={`Cantidad de ${name}`}
            onChange={handleQuantityInputChange}
          />
          <button
            className="quantity-selector__button"
            type="button"
            disabled={!canSelectQuantity || quantity >= product.stock || isAdding}
            aria-label={`Aumentar cantidad de ${name}`}
            onClick={() => onQuantityChange(quantity + 1)}
          >
            +
          </button>
        </div>

        <div className="product-details__actions">
          <button
            className="product-details__button"
            type="button"
            disabled={isAdding || isOutOfStock}
            onClick={onAddToBasket}
          >
            {isOutOfStock ? 'Producto agotado' : isAdding ? 'Agregando...' : 'Agregar al carrito'}
          </button>
          <Link className="product-details__link" to="/">
            Volver al catálogo
          </Link>
          <Link className="product-details__link" to="/basket">
            Ir al carrito
          </Link>
          {canDelete && (
            <button
              className="product-details__delete-button"
              type="button"
              disabled={isDeleting}
              onClick={onDeleteProduct}
            >
              {isDeleting ? 'Eliminando...' : 'Eliminar producto'}
            </button>
          )}
        </div>
      </div>
    </article>
  )
}

export default ProductDetails
