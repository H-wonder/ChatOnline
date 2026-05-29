import axios from 'axios'

// 创建 Axios 实例（对应后端的 HTTP 客户端，类似 HttpClient）
const httpClient = axios.create({
  baseURL: '/api',           // 所有请求以 /api 开头
  timeout: 10000,            // 10 秒超时
  headers: {
    'Content-Type': 'application/json'
  }
})

// 请求拦截器：每次请求前自动加 JWT Token 到 Header
// 对应后端的 [Authorize] 检查
httpClient.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
}, error => {
  return Promise.reject(error)
})

// 响应拦截器：处理 401 错误（Token 过期或无效）
httpClient.interceptors.response.use(
  response => response,           // 正常响应直接返回
  error => {
    if (error.response?.status === 401) {
      // Token 失效 → 清除登录状态 → 跳转登录页
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default httpClient
