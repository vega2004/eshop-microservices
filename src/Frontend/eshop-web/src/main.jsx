import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App.jsx'
import { AuthProvider } from './features/auth/context/AuthContext.jsx'
import { BasketProvider } from './features/basket/context/BasketContext.jsx'
import './styles/variables.css'
import './styles/global.css'

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <BasketProvider>
          <App />
        </BasketProvider>
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>,
)
