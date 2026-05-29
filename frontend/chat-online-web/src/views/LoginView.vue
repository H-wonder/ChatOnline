<template>
  <div class="auth-page">
    <div class="auth-card glass-strong fade-in">
      <div class="auth-header">
        <div class="logo-icon">💬</div>
        <h1>ChatOnline</h1>
        <p>匿名聊天 · 畅所欲言</p>
      </div>
      <form class="auth-form" @submit.prevent="handleLogin">
        <div class="field">
          <label>用户名</label>
          <input class="input" v-model="form.username" placeholder="请输入用户名" autocomplete="username" />
        </div>
        <div class="field">
          <label>密码</label>
          <input class="input" v-model="form.password" type="password" placeholder="请输入密码"
            autocomplete="current-password" @keydown.enter="handleLogin" />
        </div>
        <button type="submit" :disabled="loading" class="btn btn-primary submit-btn">
          {{ loading ? '登录中...' : '登 录' }}
        </button>
        <p class="switch">还没有账号？<router-link to="/register">立即注册</router-link></p>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { toast } from '@/utils/toast.js'
const router = useRouter()
const authStore = useAuthStore()
const loading = ref(false)
const form = reactive({ username: '', password: '' })
async function handleLogin() {
  if (!form.username || !form.password) { toast.warning('请输入用户名和密码'); return }
  loading.value = true
  const result = await authStore.login(form.username, form.password)
  loading.value = false
  if (result.success) { toast.success('登录成功'); router.push('/lobby') }
  else toast.error(result.message || '登录失败')
}
</script>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}
.auth-card {
  width: 100%;
  max-width: 380px;
  padding: 40px 36px;
  border-radius: var(--r-xl);
}
.auth-header { text-align: center; margin-bottom: 32px; }
.logo-icon {
  width: 72px; height: 72px;
  margin: 0 auto 16px;
  display: flex; align-items: center; justify-content: center;
  background: var(--accent-gradient);
  border-radius: var(--r-lg);
  font-size: 36px;
  box-shadow: 0 12px 30px rgba(99,102,241,.4);
}
.auth-header h1 {
  margin: 0 0 6px;
  font-size: 24px;
  font-weight: 700;
  background: var(--accent-gradient);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}
.auth-header p { margin: 0; color: var(--text-secondary); font-size: 13px; }
.auth-form { display: flex; flex-direction: column; gap: 16px; }
.field { display: flex; flex-direction: column; gap: 6px; }
.field label {
  font-size: 12px;
  color: var(--text-secondary);
  font-weight: 500;
  margin-left: 4px;
}
.submit-btn {
  margin-top: 8px;
  padding: 13px;
  font-size: 14px;
  width: 100%;
}
.switch {
  text-align: center;
  margin: 12px 0 0;
  color: var(--text-secondary);
  font-size: 13px;
}

@media (max-width: 480px) {
  .auth-card { padding: 28px 20px; }
  .auth-header h1 { font-size: 20px; }
  .logo-icon { width: 60px; height: 60px; font-size: 30px; }
}
</style>
