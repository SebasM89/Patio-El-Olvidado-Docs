<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { clienteService } from '../services/clienteService'
import type { Cliente, HistorialConsumoItem } from '../types/cliente'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'

const perfil = ref<Cliente | null>(null)
const items = ref<HistorialConsumoItem[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

function money(n: number) {
  return Number(n).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' })
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const [me, hist] = await Promise.all([
      clienteService.getMe(),
      clienteService.meHistorial(),
    ])
    perfil.value = me.data
    items.value = hist.data
  } catch (e: unknown) {
    const status = (e as { response?: { status?: number; data?: { message?: string } } })?.response
      ?.status
    const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message
    if (status === 404) {
      error.value =
        msg ??
        'No hay perfil de cliente vinculado a tu usuario. Pedile al staff que te registre.'
    } else {
      error.value = msg ?? 'No se pudo cargar tu historial.'
    }
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Mi historial</h1>
        <p>Consumos y fidelización (CU09 / RN-05)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>

    <section v-if="perfil" class="perfil-card">
      <p><strong>{{ perfil.nombre }}</strong> · {{ perfil.telefono }}</p>
      <p>
        Visitas acumuladas:
        <Tag :value="String(perfil.visitas)" severity="info" />
        <span v-if="perfil.visitas % 5 === 4" class="hint-rn">
          — ¡Tu próximo pedido completo tiene 10% de descuento (RN-05)!
        </span>
      </p>
    </section>

    <DataTable :value="items" :loading="loading" striped-rows class="menu-table" data-key="pedidoId">
      <Column field="pedidoId" header="#" style="width: 4rem" />
      <Column header="Fecha">
        <template #body="{ data }">
          {{ new Date(data.fechaCreacion).toLocaleString('es-AR') }}
        </template>
      </Column>
      <Column field="tipo" header="Tipo" />
      <Column field="estado" header="Estado" />
      <Column header="Subtotal">
        <template #body="{ data }">{{ money(data.subtotal) }}</template>
      </Column>
      <Column header="Total">
        <template #body="{ data }">{{ money(data.total) }}</template>
      </Column>
      <Column header="Descuento">
        <template #body="{ data }">
          {{ data.descuentoMonto > 0 ? money(data.descuentoMonto) : '—' }}
        </template>
      </Column>
      <template #empty>Todavía no tenés pedidos en el historial.</template>
    </DataTable>
  </div>
</template>

<style scoped>
.perfil-card {
  margin-bottom: 1.25rem;
  padding: 0.75rem 1rem;
  background: color-mix(in srgb, var(--p-surface-100, #f4f4f5) 80%, transparent);
  border-radius: 8px;
}
.hint-rn {
  margin-left: 0.5rem;
  font-size: 0.9rem;
  opacity: 0.85;
}
</style>
