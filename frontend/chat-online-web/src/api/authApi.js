import httpClient from './httpClient'

// 对应后端的 AuthController
export function register(username, password) {
  return httpClient.post('/auth/register', { username, password })
}

export function login(username, password) {
  return httpClient.post('/auth/login', { username, password })
}
