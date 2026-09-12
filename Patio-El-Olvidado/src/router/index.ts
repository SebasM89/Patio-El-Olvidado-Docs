import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/login',
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue'),
      meta: { public: true },
    },
    {
      path: '/forgot-password',
      name: 'forgot-password',
      component: () => import('../views/ForgotPasswordView.vue'),
      meta: { public: true },
    },
    {
      path: '/reset-password',
      name: 'reset-password',
      component: () => import('../views/ResetPasswordView.vue'),
      meta: { public: true },
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: () => import('../views/DashboardView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/menu',
      name: 'menu',
      component: () => import('../views/MenuView.vue'),
      meta: {
        requiresAuth: true,
        roles: ['Admin', 'Empleado', 'Cliente'],
      },
    },
    {
      path: '/pedidos',
      name: 'pedidos',
      component: () => import('../views/PedidosView.vue'),
      meta: {
        requiresAuth: true,
        roles: ['Admin', 'Empleado', 'Cliente'],
      },
    },
    {
      path: '/caja',
      name: 'caja',
      component: () => import('../views/CajaView.vue'),
      meta: {
        requiresAuth: true,
        roles: ['Admin', 'Empleado'],
      },
    },
    {
      path: '/clientes',
      name: 'clientes',
      component: () => import('../views/ClientesView.vue'),
      meta: {
        requiresAuth: true,
        roles: ['Admin', 'Empleado'],
      },
    },
    {
      path: '/mi-historial',
      name: 'mi-historial',
      component: () => import('../views/MiHistorialView.vue'),
      meta: {
        requiresAuth: true,
        roles: ['Cliente'],
      },
    },
  ],
})

/** RN-01: un usuario debe autenticarse para operar */
router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta.public) {
    if (auth.isAuthenticated && (to.name === 'login' || to.name === 'forgot-password')) {
      return { name: 'dashboard' }
    }
    return true
  }

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  const roles = to.meta.roles as string[] | undefined
  if (roles?.length && auth.rol && !roles.includes(auth.rol)) {
    return { name: 'dashboard' }
  }

  return true
})

export default router
