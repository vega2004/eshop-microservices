import ProductCard from './ProductCard.jsx'

function ProductList({ products, onAddToBasket, addingProductId }) {
  return (
    <ul className="product-grid" aria-label="Productos del catálogo">
      {products.map((product) => (
        <li key={product.id} className="product-grid__item">
          <ProductCard
            product={product}
            onAddToBasket={onAddToBasket}
            isAdding={addingProductId === product.id}
          />
        </li>
      ))}
    </ul>
  )
}

export default ProductList
