import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginView.vue'),
    meta: { requiresAuth: false, title: '登入' },
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/RegisterView.vue'),
    meta: { requiresAuth: false, title: '註冊' },
  },
  {
    path: '/',
    component: () => import('@/components/layout/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: () => import('@/views/DashboardView.vue'),
        meta: { title: '總覽' },
      },
      {
        path: 'transactions',
        name: 'Transactions',
        component: () => import('@/views/TransactionsView.vue'),
        meta: { title: '交易記錄' },
      },
      {
        path: 'categories',
        name: 'Categories',
        component: () => import('@/views/CategoriesView.vue'),
        meta: { title: '分類管理' },
      },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/',
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

// 導航守衛：未登入導向登入頁，已登入跳過登入/註冊頁
router.beforeEach((to, _from, next) => {
  const token = localStorage.getItem('token')
  const isAuthenticated = !!token

  const title = to.meta.title as string | undefined
  document.title = title ? `${title} - FinFree` : 'FinFree'

  if (to.meta.requiresAuth !== false && !isAuthenticated) {
    next({ name: 'Login' })
  } else if ((to.name === 'Login' || to.name === 'Register') && isAuthenticated) {
    next({ name: 'Dashboard' })
  } else {
    next()
  }
})

export default router
