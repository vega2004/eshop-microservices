function ProductSearch({ searchTerm, resultCount, totalCount, onSearchChange, onClear, disabled }) {
  return (
    <div className="product-search">
      <label className="product-search__label" htmlFor="product-search">
        Buscar productos
      </label>
      <div className="product-search__controls">
        <input
          className="product-search__input"
          id="product-search"
          type="search"
          value={searchTerm}
          placeholder="Buscar por nombre, descripción o categoría"
          disabled={disabled}
          onChange={(event) => onSearchChange(event.target.value)}
        />
        <button
          className="product-search__button"
          type="button"
          disabled={disabled || searchTerm.length === 0}
          onClick={onClear}
        >
          Limpiar
        </button>
      </div>
      <p className="product-search__summary" aria-live="polite">
        {resultCount} de {totalCount} resultados visibles
      </p>
    </div>
  )
}

export default ProductSearch
