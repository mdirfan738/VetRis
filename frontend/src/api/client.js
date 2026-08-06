import axios from 'axios'

// VITE_API_URL is injected at build/runtime via env vars.
// See frontend/.env.example
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080'

export const apiClient = axios.create({
  baseURL: API_URL,
  timeout: 15000
})

export const getHealth = () => apiClient.get('/api/health')

export const listStudies = () => apiClient.get('/api/studies')

export const getStudy = (id) => apiClient.get(`/api/studies/${id}`)

export const uploadStudy = (formData, onUploadProgress) =>
  apiClient.post('/api/studies/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
    onUploadProgress
  })

export default apiClient
