<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { notificacionService } from '../services/notificacionService'
import type { Notificacion } from '../types/notificacion'
import Button from 'primevue/button'
import Select from 'primevue/select'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'

const items = ref<Notificacion[]>([])
const loading = ref(false)
const markingId = ref<number | null>(null)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const filtro = ref<'todas' | 'no-leidas' | 'leidas'>('todas')

const filtroOptions = [
  { label: 'Todas', value: 'todas' },
  { label: 'No leídas', value: 'no-leidas' },
  { label: 'Leídas', value: 'leidas' },
]

const leidaParam = computed(() => {
  if (filtro.value === 'no-leidas') return false
  if (filtro.value === 'leidas') return true
  return undefined
})

function apiMessage(e: unknown, fallback: string) {
  return (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? fallback
}

function formatFecha(iso: string | null) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString('es-AR')
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const { data } = await notificacionService.list(leidaParam.value)
    items.value = data
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudieron cargar las notificaciones.')
  } finally {
    loading.value = false
  }
}

async function marcarLeida(item: Notificacion) {
  if (item.leida) return
  markingId.value = item.id
  error.value = null
  success.value = null
  try {
    await notificacionService.marcarLeida(item.id)
    success.value = 'Aviso marcado como leído.'
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo marcar el aviso.')
  } finally {
    markingId.value = null
  }
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Notificaciones</h1>
        <p>Avisos de stock en alerta (RN-12)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{ success }}</Message>

    <section class="filters">
      <div class="filter-field">
        <label for="leida">Estado</label>
        <Select
          id="leida"
          v-model="filtro"
          :options="filtroOptions"
          option-label="label"
          option-value="value"
          class="w-full"
        />
      </div>
      <div class="filter-actions">
        <Button label="Filtrar" icon="pi pi-search" :loading="loading" @click="load" />
      </div>
    </section>

    <DataTable :value="items" :loading="loading" striped-rows class="menu-table" data-key="id">
      <Column header="Fecha">
        <template #body="{ data }">{{ formatFecha(data.fechaUtc) }}</template>
      </Column>
      <Column field="titulo" header="Título" />
      <Column field="mensaje" header="Mensaje" />
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.leida ? 'Leída' : 'No leída'" :severity="data.leida ? 'secondary' : 'warn'" />
        </template>
      </Column>
      <Column header="Acciones" style="width: 11rem">
        <template #body="{ data }">
          <Button
            label="Marcar leída"
            size="small"
            :disabled="data.leida"
            :loading="markingId === data.id"
            @click="marcarLeida(data)"
          />
        </template>
      </Column>
      <template #empty>No hay notificaciones para mostrar.</template>
    </DataTable>
  </div>
</template>
