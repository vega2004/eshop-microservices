import { Link } from 'react-router-dom'

function NotFoundPage() {
  return (
    <section className="page-card not-found">
      <div>
        <span className="not-found__code">404</span>
        <h1>Página no encontrada</h1>
        <p>La ruta solicitada no existe o ya no está disponible.</p>
        <Link className="page-link" to="/">
          Regresar al catálogo
        </Link>
      </div>
    </section>
  )
}

export default NotFoundPage
