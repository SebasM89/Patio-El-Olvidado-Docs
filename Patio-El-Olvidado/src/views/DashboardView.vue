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
      <p class="hint">Este dashboard es un placeholder por rol (MVP Autenticación).</p>
    </section>
  </div>
</template>
