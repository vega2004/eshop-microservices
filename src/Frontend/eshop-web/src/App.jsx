import { Route, Routes } from 'react-router-dom'
import Footer from './components/layout/Footer.jsx'
import Header from './components/layout/Header.jsx'
import ProtectedRoute from './features/auth/components/ProtectedRoute.jsx'
import RoleProtectedRoute from './features/auth/components/RoleProtectedRoute.jsx'
import LoginPage from './features/auth/pages/LoginPage.jsx'
import RegisterPage from './features/auth/pages/RegisterPage.jsx'
import BasketPage from './features/basket/pages/BasketPage.jsx'
import CatalogPage from './features/catalog/pages/CatalogPage.jsx'
import CreateProductPage from './features/catalog/pages/CreateProductPage.jsx'
import ProductDetailsPage from './features/catalog/pages/ProductDetailsPage.jsx'
import NotFoundPage from './pages/NotFoundPage.jsx'

function App() {
  return (
    <div className="app-layout">
      <Header />
      <main className="app-main">
        <Routes>
          <Route path="/" element={<CatalogPage />} />
          <Route
            path="/admin/products/new"
            element={
              <RoleProtectedRoute allowedRoles={['Admin']}>
                <CreateProductPage />
              </RoleProtectedRoute>
            }
          />
          <Route path="/products/:productId" element={<ProductDetailsPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route
            path="/basket"
            element={
              <ProtectedRoute>
                <BasketPage />
              </ProtectedRoute>
            }
          />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </main>
      <Footer />
    </div>
  )
}

export default App
