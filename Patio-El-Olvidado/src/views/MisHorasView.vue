<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { empleadoService } from '../services/empleadoService'
import type { Empleado, Fichaje, Liquidacion, LiquidacionPreview } from '../types/empleado'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'

const perfil = ref<Empleado | null>(null)
const fichajes = ref<Fichaje[]>([])
const liquidaciones = ref<Liquidacion[]>([])
const preview = ref<LiquidacionPreview | null>(null)
const loading = ref(false)
const acting = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const periodo = reactive({
  desde: new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10),
  hasta: new Date().toISOString().slice(0, 10),
})

const abierto = computed(() => fichajes.value.some((f) => f.abierto || !f.salidaUtc))

function money(n: number) {
  return Number(n).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' })
}

function hours(n: number | null | undefined) {
  if (n == null) return '—'
  return Number(n).toLocaleString('es-AR', { maximumFractionDigits: 4 })
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const [me, f, l, p] = await Promise.all([
      empleadoService.getMe(),
      empleadoService.listMeFichajes(),
      empleadoService.listMeLiquidaciones(),
      empleadoService.previewMeLiquidacion(periodo.desde, periodo.hasta),
    ])
    perfil.value = me.data
    fichajes.value = f.data
    liquidaciones.value = l.data
    preview.value = p.data
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo cargar tu perfil de horas.'
  } finally {
    loading.value = false
  }
}

async function refreshPreview() {
  try {
    const { data } = await empleadoService.previewMeLiquidacion(periodo.desde, periodo.hasta)
    preview.value = data
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo calcular el preview.'
  }
}

async function entrada() {
  acting.value = true
  error.value = null
  success.value = null
  try {
    await empleadoService.entradaMe()
    success.value = 'Entrada registrada.'
    await load()
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo registrar la entrada.'
  } finally {
    acting.value = false
  }
}

async function salida() {
  acting.value = true
  error.value = null
  success.value = null
  try {
    await empleadoService.salidaMe()
    success.value = 'Salida registrada.'
    await load()
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo registrar la salida.'
  } finally {
    acting.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Mis horas</h1>
        <p>Fichaje propio, saldo y liquidaciones (RF-06 / RN-07)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{
      success
    }}</Message>

    <section v-if="perfil" class="saldo-card">
      <div>
        <h2>{{ perfil.nombre }}</h2>
        <p>{{ perfil.puesto || 'Sin puesto' }} · Tarifa {{ money(perfil.tarifaHora) }}</p>
      </div>
      <div class="saldo">
        <span class="saldo-label">Saldo de horas</span>
        <strong>{{ hours(perfil.horasTrabajadas) }}</strong>
        <Tag
          :value="abierto ? 'Fichaje abierto' : 'Sin fichaje abierto'"
          :severity="abierto ? 'warn' : 'success'"
        />
      </div>
      <div class="detalle-actions">
        <Button
          label="Registrar entrada"
          icon="pi pi-sign-in"
          :disabled="abierto"
          :loading="acting"
          @click="entrada"
        />
        <Button
          label="Registrar salida"
          icon="pi pi-sign-out"
          severity="secondary"
          :disabled="!abierto"
          :loading="acting"
          @click="salida"
        />
      </div>
    </section>

    <section class="liq-box">
      <h3>Preview liquidación</h3>
      <div class="filters">
        <div class="filter-field">
          <label for="desde">Desde</label>
          <InputText id="desde" v-model="periodo.desde" type="date" class="w-full" />
        </div>
        <div class="filter-field">
          <label for="hasta">Hasta</label>
          <InputText id="hasta" v-model="periodo.hasta" type="date" class="w-full" />
        </div>
        <div class="filter-actions">
          <Button label="Calcular" :loading="loading" @click="refreshPreview" />
        </div>
      </div>
      <p v-if="preview">
        Horas: <strong>{{ hours(preview.horas) }}</strong> · Monto estimado:
        <strong>{{ money(preview.monto) }}</strong>
      </p>
    </section>

    <h3>Mis fichajes</h3>
    <DataTable :value="fichajes" :loading="loading" striped-rows class="menu-table" data-key="id">
      <Column field="id" header="#" style="width: 4rem" />
      <Column header="Entrada">
        <template #body="{ data }">{{ new Date(data.entradaUtc).toLocaleString('es-AR') }}</template>
      </Column>
      <Column header="Salida">
        <template #body="{ data }">
          {{ data.salidaUtc ? new Date(data.salidaUtc).toLocaleString('es-AR') : 'Abierto' }}
        </template>
      </Column>
      <Column header="Horas">
        <template #body="{ data }">{{ hours(data.horas) }}</template>
      </Column>
      <template #empty>Todavía no registraste horas.</template>
    </DataTable>

    <h3>Mis liquidaciones</h3>
    <DataTable :value="liquidaciones" :loading="loading" striped-rows class="menu-table" data-key="id">
      <Column field="id" header="#" style="width: 4rem" />
      <Column header="Período">
        <template #body="{ data }">{{ data.periodoDesde }} → {{ data.periodoHasta }}</template>
      </Column>
      <Column header="Horas">
        <template #body="{ data }">{{ hours(data.horas) }}</template>
      </Column>
      <Column header="Monto">
        <template #body="{ data }">{{ money(data.monto) }}</template>
      </Column>
      <Column header="Generada">
        <template #body="{ data }">{{ new Date(data.generadaEnUtc).toLocaleString('es-AR') }}</template>
      </Column>
      <template #empty>Sin liquidaciones generadas por administración.</template>
    </DataTable>
  </div>
</template>

<style scoped>
.saldo-card {
  display: grid;
  gap: 1rem;
  margin-bottom: 1.25rem;
  padding: 1rem 0;
}
.saldo {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.75rem;
}
.saldo-label {
  opacity: 0.75;
}
.detalle-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
}
.liq-box {
  margin-bottom: 1.25rem;
}
h3 {
  margin: 1rem 0 0.5rem;
  font-size: 1rem;
}
</style>
