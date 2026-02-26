import axios from 'axios'
import { ElMessage } from 'element-plus'

const http = axios.create({
  baseURL: '/api',
  timeout: 30000
})

http.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  
  // Filter out null and undefined params
  if (config.params) {
    const filteredParams: Record<string, any> = {}
    for (const key in config.params) {
      const value = config.params[key]
      if (value !== null && value !== undefined && value !== '') {
        filteredParams[key] = value
      }
    }
    config.params = filteredParams
  }
  
  return config
})

http.interceptors.response.use(
  res => res.data,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    ElMessage.error(err.response?.data?.message || '请求失败')
    return Promise.reject(err)
  }
)

export default http
