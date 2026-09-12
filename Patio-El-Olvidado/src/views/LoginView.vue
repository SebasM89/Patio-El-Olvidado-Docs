<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('admin@patioelolvidado.local')
const password = ref('')
const localError = ref<string | null>(null)

async function onSubmit() {
  localError.value = null
  try {
    await auth.login({ email: email.value, password: password.value })
    const redirect = (route.query.redirect as string) || '/dashboard'
    await router.push(redirect)
  } catch {
    localError.value = auth.error ?? 'Credenciales inválidas.'
  }
}
</script>

<template>
  <div class="auth-page">
    <form class="auth-form" @submit.prevent="onSubmit">
      <h1>Patio El Olvidado</h1>
      <p class="subtitle">Iniciar sesión</p>

      <label for="email">Email</label>
      <InputText id="email" v-model="email" type="email" class="w-full" autocomplete="username" />

      <label for="password">Contraseña</label>
      <Password
        id="password"
        v-model="password"
        :feedback="false"
        toggle-mask
        input-class="w-full"
        class="w-full"
        autocomplete="current-password"
      />

      <Message v-if="localError" severity="error" :closable="false">{{ localError }}</Message>

      <Button type="submit" label="Entrar" :loading="auth.loading" class="w-full" />

      <RouterLink class="link" to="/forgot-password">¿Olvidaste tu contraseña?</RouterLink>
    </form>
  </div>
</template>
