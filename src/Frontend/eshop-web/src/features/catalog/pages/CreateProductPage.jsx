import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import ErrorMessage from '../../../components/common/ErrorMessage.jsx'
import SuccessMessage from '../../../components/common/SuccessMessage.jsx'
import { useAuth } from '../../auth/hooks/useAuth.js'
import { createProduct } from '../services/catalogService.js'

const createProductErrorMessage = 'No fue posible crear el producto. Verifica los datos e intenta nuevamente.'

const initialForm = {
  name: '',
  description: '',
  category: '',
  imageFiles: '',
  price: '',
  stock: '',
}

function getCategories(value) {
  return value
    .split(',')
    .map((category) => category.trim())
    .filter((category) => category.length > 0)
}

function validateForm(form) {
  const categories = getCategories(form.category)
  const price = Number(form.price)
  const stock = Number(form.stock)

  if (!form.name.trim() || !form.description.trim() || categories.length === 0) {
    return false
  }

  return Number.isFinite(price) && price >= 0 && Number.isInteger(stock) && stock >= 0
}

function CreateProductPage() {
  const navigate = useNavigate()
  const { logout } = useAuth()
  const [form, setForm] = useState(initialForm)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const [successMessage, setSuccessMessage] = useState(null)

  function handleChange(event) {
    const { name, value } = event.target

    setForm((currentForm) => ({
      ...currentForm,
      [name]: value,
    }))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    setError(null)
    setSuccessMessage(null)

    if (!validateForm(form)) {
      setError('Completa nombre, descripción, categorías, precio válido y unidades disponibles.')
      return
    }

    try {
      setSubmitting(true)

      const result = await createProduct({
        name: form.name.trim(),
        description: form.description.trim(),
        category: getCategories(form.category),
        imageFiles: form.imageFiles.trim(),
        price: Number(form.price),
        stock: Number(form.stock),
      })

      setSuccessMessage('Producto creado correctamente.')

      if (result?.id) {
        navigate(`/products/${result.id}`, { replace: true })
        return
      }

      navigate('/', { replace: true })
    } catch (requestError) {
      if (requestError.response?.status === 401) {
        logout()
        setError('Tu sesión expiró. Inicia sesión nuevamente.')
        navigate('/login', { replace: true })
        return
      }

      if (requestError.response?.status === 403) {
        setError('No tienes permisos para realizar esta acción.')
        return
      }

      setError(createProductErrorMessage)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="product-admin-page">
      <div className="product-admin-hero">
        <span className="product-admin-hero__eyebrow">Gestión de catálogo</span>
        <h1>Crear producto</h1>
        <p>Registra un producto para que esté disponible en el catálogo principal.</p>
      </div>

      <div className="product-admin-card">
        <ErrorMessage message={error} />
        <SuccessMessage message={successMessage} />

        <form className="product-admin-form" onSubmit={handleSubmit} noValidate>
          <div className="product-admin-form__field">
            <label htmlFor="product-name">Nombre</label>
            <input
              id="product-name"
              name="name"
              type="text"
              value={form.name}
              placeholder="Ej. Teclado mecánico profesional"
              disabled={submitting}
              onChange={handleChange}
            />
          </div>

          <div className="product-admin-form__field">
            <label htmlFor="product-description">Descripción</label>
            <textarea
              id="product-description"
              name="description"
              value={form.description}
              placeholder="Describe las características principales del producto."
              rows="5"
              disabled={submitting}
              onChange={handleChange}
            />
          </div>

          <div className="product-admin-form__meta">
            <div className="product-admin-form__field">
              <label htmlFor="product-category">Categorías</label>
              <input
                id="product-category"
                name="category"
                type="text"
                value={form.category}
                placeholder="Tecnología, Accesorios"
                disabled={submitting}
                onChange={handleChange}
              />
              <p>Separa varias categorías con coma.</p>
            </div>

            <div className="product-admin-form__field">
              <label htmlFor="product-price">Precio</label>
              <input
                id="product-price"
                name="price"
                type="number"
                min="0"
                step="0.01"
                value={form.price}
                placeholder="0.00"
                disabled={submitting}
                onChange={handleChange}
              />
            </div>

            <div className="product-admin-form__field">
              <label htmlFor="product-stock">Unidades disponibles</label>
              <input
                id="product-stock"
                name="stock"
                type="number"
                min="0"
                step="1"
                value={form.stock}
                placeholder="0"
                disabled={submitting}
                onChange={handleChange}
              />
            </div>
          </div>

          <div className="product-admin-form__field">
            <label htmlFor="product-image-files">Imagen</label>
            <input
              id="product-image-files"
              name="imageFiles"
              type="text"
              value={form.imageFiles}
              placeholder="URL o ruta de imagen"
              disabled={submitting}
              onChange={handleChange}
            />
            <p>Este campo se envía como texto. No se realiza subida de archivos.</p>
          </div>

          <div className="product-admin-form__actions">
            <button className="product-admin-form__button" type="submit" disabled={submitting}>
              {submitting ? 'Creando producto...' : 'Crear producto'}
            </button>
            <Link className="product-admin-form__link" to="/">
              Volver al catálogo
            </Link>
          </div>
        </form>
      </div>
    </section>
  )
}

export default CreateProductPage
