import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { login as loginApi, register as registerApi } from '@/api/authApi'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token') || '')
  const user = ref(JSON.parse(localStorage.getItem('user') || 'null'))
  const isLoggedIn = computed(() => !!token.value)

  async function register(username, password) {
    try {
      const res = await registerApi(username, password)
      const data = res.data
      if (data.success) {
        token.value = data.token
        user.value = data.user
        localStorage.setItem('token', data.token)
        localStorage.setItem('user', JSON.stringify(data.user))
      }
      return data
    } catch (err) {
      // 后端返回 400 时，Axios 会抛异常，错误详情在 err.response.data 里
      return err.response?.data || { success: false, message: '注册失败，请检查网络' }
    }
  }

  async function login(username, password) {
    try {
      const res = await loginApi(username, password)
      const data = res.data
      if (data.success) {
        token.value = data.token
        user.value = data.user
        localStorage.setItem('token', data.token)
        localStorage.setItem('user', JSON.stringify(data.user))
      }
      return data
    } catch (err) {
      return err.response?.data || { success: false, message: '登录失败，请检查网络' }
    }
  }

  function logout() {
    token.value = ''
    user.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  return { token, user, isLoggedIn, register, login, logout }
})
