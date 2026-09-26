<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { historiaService } from '../services/historiaService'
import type { Historia } from '../types/historia'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Message from 'primevue/message'

const auth = useAuthStore()
const esAdmin = computed(() => auth.rol === 'Admin')

const historia = ref<Historia | null>(null)
const loading = ref(false)
const guardando = ref(false)
const error = ref<string | null>(null)
const noConfigurada = ref(false)
const validacion = ref<string[]>([])
const success = ref<string | null>(null)

const form = reactive({
  titulo: '',
  texto: '',
})

function apiMessage(e: unknown, fallback: string) {
  return (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? fallback
}

function mensajesValidacion(e: unknown): string[] {
  const errors = (e as { response?: { data?: { errors?: { errorMessage?: string }[] } } })
    ?.response?.data?.errors
  const mensajes = (errors ?? [])
    .map((item) => item.errorMessage)
    .filter((item): item is string => Boolean(item))
  return mensajes.length ? mensajes : [apiMessage(e, 'Revisá título y texto.')]
}

function aplicarFormulario(data: Historia) {
  form.titulo = data.titulo
  form.texto = data.texto
}

async function load() {
  loading.value = true
  error.value = null
  noConfigurada.value = false
  validacion.value = []
  try {
    const { data } = await historiaService.get()
    historia.value = data
    aplicarFormulario(data)
  } catch (e: unknown) {
    historia.value = null
    const status = (e as { response?: { status?: number } })?.response?.status
    if (status === 404) {
      noConfigurada.value = true
      return
    }
    error.value = apiMessage(e, 'No se pudo cargar la historia.')
  } finally {
    loading.value = false
  }
}

async function guardar() {
  if (!esAdmin.value || !historia.value) return
  guardando.value = true
  error.value = null
  validacion.value = []
  success.value = null
  try {
    const { data } = await historiaService.update({
      titulo: form.titulo,
      texto: form.texto,
    })
    historia.value = data
    aplicarFormulario(data)
    success.value = 'Historia actualizada.'
  } catch (e: unknown) {
    const status = (e as { response?: { status?: number } })?.response?.status
    if (status === 400) {
      validacion.value = mensajesValidacion(e)
      return
    }
    if (status === 404) {
      historia.value = null
      noConfigurada.value = true
      return
    }
    error.value = apiMessage(e, 'No se pudo guardar la historia.')
  } finally {
    guardando.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Historia</h1>
        <p>Texto único del restaurante (RN-14)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
      </div>
    </header>

    <p v-if="loading">Cargando…</p>
    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="noConfigurada" severity="warn">Historia no configurada</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{ success }}</Message>
    <Message
      v-for="(mensaje, indice) in validacion"
      :key="indice"
      severity="error"
    >
      {{ mensaje }}
    </Message>

    <article v-if="historia" class="historia-card">
      <h2>{{ historia.titulo }}</h2>
      <p class="historia-texto">{{ historia.texto }}</p>
    </article>

    <form v-if="esAdmin && historia" class="product-form historia-form" @submit.prevent="guardar">
      <label for="historia-titulo">Título</label>
      <InputText id="historia-titulo" v-model="form.titulo" class="w-full" />

      <label for="historia-texto">Texto</label>
      <Textarea id="historia-texto" v-model="form.texto" class="w-full" rows="8" auto-resize />

      <div class="form-actions">
        <Button type="submit" label="Guardar" icon="pi pi-save" :loading="guardando" />
      </div>
    </form>
  </div>
</template>

<style scoped>
.historia-card {
  background: #fff;
  border: 1px solid #e7e5e4;
  border-radius: 12px;
  padding: 1.25rem 1.5rem;
  margin-bottom: 1.25rem;
}

.historia-card h2 {
  margin: 0 0 0.75rem;
}

.historia-texto {
  margin: 0;
  white-space: pre-wrap;
  line-height: 1.5;
}

.historia-form {
  background: #fff;
  border: 1px solid #e7e5e4;
  border-radius: 12px;
  padding: 1.25rem 1.5rem;
}
</style>
