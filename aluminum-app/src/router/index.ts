import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('../views/Login.vue')
    },
    {
      path: '/',
      component: () => import('../layouts/MainLayout.vue'),
      redirect: '/dashboard',
      children: [
        {
          path: 'dashboard',
          name: 'Dashboard',
          component: () => import('../views/Dashboard.vue'),
          meta: { title: '主控制台' }
        },
        {
          path: 'variety',
          name: 'Variety',
          component: () => import('../views/Variety.vue'),
          meta: { title: '品种设置' }
        },
        {
          path: 'order',
          name: 'Order',
          component: () => import('../views/Order.vue'),
          meta: { title: '订单管理' }
        },
        {
          path: 'inventory',
          name: 'Inventory',
          component: () => import('../views/Inventory.vue'),
          meta: { title: '库存管理' }
        },
        {
          path: 'waste',
          name: 'Waste',
          component: () => import('../views/Waste.vue'),
          meta: { title: '废料设置' }
        },
        {
          path: 'nesting',
          name: 'Nesting',
          component: () => import('../views/Nesting.vue'),
          meta: { title: '智能套料任务' }
        },
        {
          path: 'result',
          name: 'Result',
          component: () => import('../views/Result.vue'),
          meta: { title: '历史记录' }
        },
        {
          path: 'user',
          name: 'User',
          component: () => import('../views/User.vue'),
          meta: { title: '用户管理' }
        }
      ]
    }
  ]
})

router.beforeEach((to, _from, next) => {
  if (to.path !== '/login' && !localStorage.getItem('token')) {
    next('/login')
  } else {
    next()
  }
})

export default router
