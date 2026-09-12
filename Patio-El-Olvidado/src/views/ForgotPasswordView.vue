<script setup lang="ts">
import { ref } from 'vue'
import { authService } from '../services/authService'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'

const email = ref('')
const loading = ref(false)
const message = ref<string | null>(null)
const error = ref<string | null>(null)

async function onSubmit() {
  loading.value = true
  message.value = null
  error.value = null
  try {
    const { data } = await authService.forgotPassword({ email: email.value })
    message.value = data.message
  } catch {
    error.value = 'No se pudo procesar la solicitud.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-page">
    <form class="auth-form" @submit.prevent="onSubmit">
      <h1>Recuperar contraseña</h1>
      <p class="subtitle">Te enviaremos instrucciones si el email existe.</p>

      <label for="email">Email</label>
      <InputText id="email" v-model="email" type="email" class="w-full" required />

      <Message v-if="message" severity="success" :closable="false">{{ message }}</Message>
      <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>

      <Button type="submit" label="Enviar" :loading="loading" class="w-full" />
      <RouterLink class="link" to="/login">Volver al login</RouterLink>
    </form>
  </div>
</template>
