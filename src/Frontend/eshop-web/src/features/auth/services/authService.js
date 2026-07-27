import axios from 'axios'
import { AUTH_API_URL } from '../../../config/apiConfig.js'

const authApi = axios.create({
  baseURL: AUTH_API_URL,
})

export async function registerUser(credentials, signal) {
  const response = await authApi.post(
    '/auth/register',
    {
      userName: credentials.userName,
      email: credentials.email,
      password: credentials.password,
    },
    { signal },
  )

  return response.data
}

export async function loginUser(credentials, signal) {
  const response = await authApi.post(
    '/auth/login',
    {
      email: credentials.email,
      password: credentials.password,
    },
    { signal },
  )

  return response.data
}

export async function getCurrentUser(accessToken, signal) {
  const response = await authApi.get('/auth/me', {
    signal,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  return response.data.user
}
