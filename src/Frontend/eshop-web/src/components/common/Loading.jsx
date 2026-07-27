function Loading({ message = 'Cargando productos...' }) {
  return (
    <div className="status-message loading-message" role="status" aria-live="polite">
      <span className="loading-message__spinner" aria-hidden="true" />
      <span>{message}</span>
    </div>
  )
}

export default Loading
