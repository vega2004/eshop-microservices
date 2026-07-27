function SuccessMessage({ message }) {
  if (!message) {
    return null
  }

  return (
    <div className="success-message" role="status">
      <span>{message}</span>
    </div>
  )
}

export default SuccessMessage
