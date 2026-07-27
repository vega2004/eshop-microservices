function ConfirmDialog({ title, message, confirmLabel, cancelLabel = 'Cancelar', loading, onCancel, onConfirm }) {
  return (
    <div className="confirm-dialog__backdrop">
      <section
        className="confirm-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
        aria-describedby="confirm-dialog-message"
      >
        <h2 id="confirm-dialog-title">{title}</h2>
        <p id="confirm-dialog-message">{message}</p>
        <div className="confirm-dialog__actions">
          <button className="confirm-dialog__cancel" type="button" disabled={loading} onClick={onCancel}>
            {cancelLabel}
          </button>
          <button className="confirm-dialog__confirm" type="button" disabled={loading} onClick={onConfirm}>
            {loading ? 'Eliminando...' : confirmLabel}
          </button>
        </div>
      </section>
    </div>
  )
}

export default ConfirmDialog
