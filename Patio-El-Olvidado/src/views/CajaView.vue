<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { cajaService } from '../services/cajaService'
import type { CajaDia } from '../types/pago'
import Button from 'primevue/button'
import Message from 'primevue/message'

function todayUtcIso(): string {
  const now = new Date()
  const y = now.getUTCFullYear()
  const m = String(now.getUTCMonth() + 1).padStart(2, '0')
  const d = String(now.getUTCDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

const loading = ref(false)
const exporting = ref(false)
const error = ref<string | null>(null)
const caja = ref<CajaDia | null>(null)
const fecha = ref(todayUtcIso())

function money(n: number) {
  return Number(n).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' })
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const { data } = await cajaService.byFecha(fecha.value)
    caja.value = data
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo cargar la caja.'
  } finally {
    loading.value = false
  }
}

async function exportPdf() {
  exporting.value = true
  error.value = null
  try {
    await cajaService.exportPdf(fecha.value)
  } catch {
    error.value = 'No se pudo exportar el PDF.'
  } finally {
    exporting.value = false
  }
}

async function exportCsv() {
  exporting.value = true
  error.value = null
  try {
    await cajaService.exportCsv(fecha.value)
  } catch {
    error.value = 'No se pudo exportar el CSV.'
  } finally {
    exporting.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Caja del día</h1>
        <p>Resumen diario (RF-07). Fecha en UTC.</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button label="Actualizar" icon="pi pi-refresh" :loading="loading" @click="load" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>

    <div class="caja-toolbar">
      <label class="caja-fecha-label">
        Fecha (UTC)
        <input v-model="fecha" type="date" class="caja-fecha-input" @change="load" />
      </label>
      <div class="caja-export-actions">
        <Button
          label="Exportar PDF"
          icon="pi pi-file-pdf"
          severity="secondary"
          :loading="exporting"
          :disabled="loading"
          @click="exportPdf"
        />
        <Button
          label="Exportar CSV"
          icon="pi pi-file"
          severity="secondary"
          :loading="exporting"
          :disabled="loading"
          @click="exportCsv"
        />
      </div>
    </div>

    <section v-if="caja" class="caja-resumen">
      <p class="caja-fecha">
        Fecha (UTC): <strong>{{ caja.fecha }}</strong>
      </p>
      <dl class="caja-totales">
        <div>
          <dt>Efectivo</dt>
          <dd>{{ money(caja.totalEfectivo) }}</dd>
        </div>
        <div>
          <dt>Tarjeta</dt>
          <dd>{{ money(caja.totalTarjeta) }}</dd>
        </div>
        <div>
          <dt>Transferencia</dt>
          <dd>{{ money(caja.totalTransferencia) }}</dd>
        </div>
        <div class="caja-total">
          <dt>Total</dt>
          <dd>{{ money(caja.total) }}</dd>
        </div>
      </dl>
    </section>
  </div>
</template>
