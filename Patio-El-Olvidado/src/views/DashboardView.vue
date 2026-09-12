<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import Button from 'primevue/button'

const auth = useAuthStore()
const router = useRouter()

const rolLabel = computed(() => auth.rol ?? 'Sin rol')
const welcome = computed(() => {
  switch (auth.rol) {
    case 'Admin':
      return 'Panel administrador (placeholder)'
    case 'Empleado':
      return 'Panel empleado (placeholder)'
    case 'Cliente':
      return 'Panel cliente (placeholder)'
    default:
      return 'Dashboard'
  }
})

async function onLogout() {
  await auth.logout()
  await router.push('/login')
}
</script>

<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <div>
        <h1>Patio El Olvidado</h1>
        <p>{{ welcome }}</p>
      </div>
      <Button label="Cerrar sesión" severity="secondary" @click="onLogout" />
    </header>

    <section class="dashboard-body">
      <p><strong>Usuario:</strong> {{ auth.usuario?.nombre }}</p>
      <p><strong>Email:</strong> {{ auth.usuario?.email }}</p>
      <p><strong>Rol:</strong> {{ rolLabel }}</p>

      <nav class="dashboard-nav">
        <RouterLink to="/menu" class="nav-card">
          <span class="nav-title">Gestión de menú</span>
          <span class="nav-desc">Ver catálogo{{ auth.rol === 'Admin' ? ' y administrar productos' : '' }}</span>
        </RouterLink>
        <RouterLink to="/pedidos" class="nav-card">
          <span class="nav-title">Pedidos</span>
          <span class="nav-desc">
            {{
              auth.rol === 'Cliente'
                ? 'Crear y seguir tus pedidos'
                : 'Gestionar pedidos local / para llevar y cobrar'
            }}
          </span>
        </RouterLink>
        <RouterLink
          v-if="auth.rol === 'Admin' || auth.rol === 'Empleado'"
          to="/caja"
          class="nav-card"
        >
          <span class="nav-title">Caja del día</span>
          <span class="nav-desc">Totales por método (RN-08)</span>
        </RouterLink>
        <RouterLink
          v-if="auth.rol === 'Admin' || auth.rol === 'Empleado'"
          to="/clientes"
          class="nav-card"
        >
          <span class="nav-title">Clientes</span>
          <span class="nav-desc">Registro, historial y fidelización (RF-05)</span>
        </RouterLink>
        <RouterLink v-if="auth.rol === 'Cliente'" to="/mi-historial" class="nav-card">
          <span class="nav-title">Mi historial</span>
          <span class="nav-desc">Consultá tus consumos y visitas (CU09)</span>
        </RouterLink>
      </nav>

      <p class="hint">Módulos disponibles según rol (MVP).</p>
    </section>
  </div>
</template>
