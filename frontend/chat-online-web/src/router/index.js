import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/lobby' },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { guest: true }
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/views/RegisterView.vue'),
      meta: { guest: true }
    },
    {
      path: '/lobby',
      name: 'lobby',
      component: () => import('@/views/LobbyView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/create-group',
      name: 'createGroup',
      component: () => import('@/views/CreateGroupView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/group/:id',
      name: 'groupChat',
      component: () => import('@/views/GroupChatView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/chat/new',
      name: 'newPrivateChat',
      component: () => import('@/views/PrivateChatView.vue'),
      meta: { requiresAuth: true }
    },
    {
      path: '/chat/:id',
      name: 'privateChat',
      component: () => import('@/views/PrivateChatView.vue'),
      meta: { requiresAuth: true }
    }
  ]
})

// 路由守卫
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  if (to.meta.requiresAuth && !authStore.isLoggedIn) {
    next('/login')
  } else if (to.meta.guest && authStore.isLoggedIn) {
    next('/lobby')
  } else {
    next()
  }
})

export default router
