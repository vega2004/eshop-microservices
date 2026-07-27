function ErrorMessage({ message }) {
  if (!message) {
    return null
  }

  return (
    <div className="error-message" role="alert">
      <span>{message}</span>
    </div>
  )
}

export default ErrorMessage
