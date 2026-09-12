<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { authService } from '../services/authService'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'

const route = useRoute()
const router = useRouter()

const token = ref((route.query.token as string) || '')
const newPassword = ref('')
const loading = ref(false)
const message = ref<string | null>(null)
const error = ref<string | null>(null)

async function onSubmit() {
  loading.value = true
  message.value = null
  error.value = null
  try {
    const { data } = await authService.resetPassword({
      token: token.value,
      newPassword: newPassword.value,
    })
    message.value = data.message
    setTimeout(() => router.push('/login'), 1500)
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo restablecer la contraseña.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-page">
    <form class="auth-form" @submit.prevent="onSubmit">
      <h1>Nueva contraseña</h1>
      <p class="subtitle">Pegá el token recibido e ingresá la nueva clave.</p>

      <label for="token">Token</label>
      <InputText id="token" v-model="token" class="w-full" required />

      <label for="password">Nueva contraseña</label>
      <Password
        id="password"
        v-model="newPassword"
        toggle-mask
        :feedback="true"
        input-class="w-full"
        class="w-full"
        required
      />

      <Message v-if="message" severity="success" :closable="false">{{ message }}</Message>
      <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

      <Button type="submit" label="Guardar" :loading="loading" class="w-full" />
      <RouterLink class="link" to="/login">Volver al login</RouterLink>
    </form>
  </div>
</template>
