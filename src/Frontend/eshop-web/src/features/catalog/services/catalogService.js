import axios from 'axios'
import { apiConfig } from '../../../config/apiConfig.js'
import { getStoredAccessToken } from '../../auth/services/authStorage.js'

const catalogApi = axios.create({
  baseURL: apiConfig.catalogApiUrl,
})

function getAuthorizationHeaders() {
  const accessToken = getStoredAccessToken()

  return accessToken
    ? {
        Authorization: `Bearer ${accessToken}`,
      }
    : undefined
}

export async function getProducts(pageNumber = 1, pageSize = 10, signal) {
  const response = await catalogApi.get('/products', {
    params: {
      pageNumber,
      pageSize,
    },
    signal,
  })

  const products = response.data?.products

  if (!products || !Array.isArray(products.data)) {
    throw new Error('Invalid Catalog.API products response.')
  }

  return {
    pageNumber: products.pageNumber,
    pageSize: products.pageSize,
    totalCount: products.totalCount,
    data: products.data,
  }
}

export async function getProductsByCategory(category, signal) {
  const response = await catalogApi.get(`/products/category/${encodeURIComponent(category)}`, {
    signal,
  })

  return Array.isArray(response.data?.products) ? response.data.products : []
}

export async function getProductById(productId, signal) {
  const response = await catalogApi.get(`/products/${encodeURIComponent(productId)}`, {
    signal,
  })

  return response.data?.product ?? null
}

export async function createProduct(product, signal) {
  const response = await catalogApi.post(
    '/products',
    {
      name: product.name,
      description: product.description,
      category: product.category,
      imageFiles: product.imageFiles,
      price: product.price,
      stock: product.stock,
    },
    {
      signal,
      headers: getAuthorizationHeaders(),
    },
  )

  return response.data
}

export async function updateProductStock(productId, stock, signal) {
  const response = await catalogApi.patch(
    `/products/${encodeURIComponent(productId)}/stock`,
    {
      stock,
    },
    {
      signal,
      headers: getAuthorizationHeaders(),
    },
  )

  if (!response.data?.product) {
    throw new Error('Invalid Catalog.API stock update response.')
  }

  return response.data.product
}

export async function deleteProduct(productId, signal) {
  const response = await catalogApi.delete(`/products/${encodeURIComponent(productId)}`, {
    signal,
    headers: getAuthorizationHeaders(),
  })

  return response.data
}
