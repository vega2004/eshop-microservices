const priceFormatter = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
})

function formatPrice(value) {
  return Number.isFinite(value) ? priceFormatter.format(value) : priceFormatter.format(0)
}

function BasketItem({
  item,
  stock,
  stockUnavailable,
  isUpdating,
  isRemoving,
  onDecrease,
  onIncrease,
  onRemove,
}) {
  const price = Number(item.price)
  const quantity = Number.isInteger(item.quantity) ? item.quantity : 0
  const subtotal = Number.isFinite(price) ? price * quantity : 0
  const productName = item.productName ?? 'Producto'
  const hasStock = Number.isInteger(stock) && stock >= 0
  const exceedsStock = hasStock && quantity > stock
  const increaseDisabled = isUpdating || isRemoving || (hasStock && quantity >= stock) || stock === 0

  function getStockLabel() {
    if (stockUnavailable) {
      return 'Stock no disponible'
    }

    if (!hasStock) {
      return 'Stock no disponible'
    }

    return `Disponibles: ${stock}`
  }

  return (
    <article className="basket-item">
      <div className="basket-item__details">
        <h2>{productName}</h2>
        <p>Color: {item.color}</p>
        <p className={stock === 0 ? 'basket-item__stock basket-item__stock--empty' : 'basket-item__stock'}>
          {getStockLabel()}
        </p>
        {exceedsStock && (
          <p className="basket-item__stock-warning">
            Solo quedan {stock} unidades disponibles. Ajusta la cantidad.
          </p>
        )}

        <div className="basket-item__quantity" aria-label={`Cantidad de ${productName}`}>
          <button
            className="basket-item__quantity-button"
            type="button"
            disabled={isUpdating || isRemoving || quantity <= 1}
            aria-label={`Disminuir cantidad de ${productName}`}
            onClick={onDecrease}
          >
            -
          </button>
          <span className="basket-item__quantity-value">{quantity}</span>
          <button
            className="basket-item__quantity-button"
            type="button"
            disabled={increaseDisabled}
            aria-label={`Aumentar cantidad de ${productName}`}
            onClick={onIncrease}
          >
            +
          </button>
        </div>
      </div>

      <dl className="basket-item__summary">
        <div>
          <dt>Precio unitario</dt>
          <dd>{formatPrice(price)}</dd>
        </div>
        <div>
          <dt>Subtotal</dt>
          <dd>{formatPrice(subtotal)}</dd>
        </div>
      </dl>

      <div className="basket-item__actions">
        <button
          className="basket-item__remove-button"
          type="button"
          disabled={isRemoving || isUpdating}
          aria-label={`Eliminar ${productName} del carrito`}
          onClick={onRemove}
        >
          {isRemoving ? 'Eliminando...' : 'Eliminar'}
        </button>
      </div>
    </article>
  )
}

export default BasketItem
