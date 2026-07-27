function Pagination({ pageNumber, pageSize, totalCount, onPageChange, disabled }) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  return (
    <nav className="pagination" aria-label="Paginación del catálogo">
      <button
        className="pagination__button"
        type="button"
        disabled={disabled || pageNumber <= 1}
        onClick={() => onPageChange(pageNumber - 1)}
      >
        Anterior
      </button>

      <span className="pagination__current" aria-current="page">
        Página {pageNumber} de {totalPages}
      </span>

      <button
        className="pagination__button"
        type="button"
        disabled={disabled || pageNumber >= totalPages}
        onClick={() => onPageChange(pageNumber + 1)}
      >
        Siguiente
      </button>
    </nav>
  )
}

export default Pagination
