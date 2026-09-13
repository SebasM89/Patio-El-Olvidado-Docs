<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { reportesService } from '../services/reportesService'
import type {
  CajaRangoReporte,
  NominaReporte,
  ReporteTab,
  VentasReporte,
} from '../types/reporte'
import Button from 'primevue/button'
import Message from 'primevue/message'

function todayUtcIso(): string {
  const now = new Date()
  const y = now.getUTCFullYear()
  const m = String(now.getUTCMonth() + 1).padStart(2, '0')
  const d = String(now.getUTCDate()).padStart(2, '0')
  return `${y}-${m}-${d}`
}

function firstOfMonthUtcIso(): string {
  const now = new Date()
  const y = now.getUTCFullYear()
  const m = String(now.getUTCMonth() + 1).padStart(2, '0')
  return `${y}-${m}-01`
}

const tab = ref<ReporteTab>('ventas')
const desde = ref(firstOfMonthUtcIso())
const hasta = ref(todayUtcIso())
const loading = ref(false)
const exporting = ref(false)
const error = ref<string | null>(null)

const ventas = ref<VentasReporte | null>(null)
const caja = ref<CajaRangoReporte | null>(null)
const nomina = ref<NominaReporte | null>(null)

function money(n: number) {
  return Number(n).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' })
}

async function load() {
  loading.value = true
  error.value = null
  try {
    if (tab.value === 'ventas') {
      const { data } = await reportesService.ventas(desde.value, hasta.value)
      ventas.value = data
    } else if (tab.value === 'caja') {
      const { data } = await reportesService.caja(desde.value, hasta.value)
      caja.value = data
    } else {
      const { data } = await reportesService.nomina(desde.value, hasta.value)
      nomina.value = data
    }
  } catch (e: unknown) {
    const ax = e as {
      response?: {
        status?: number
        data?: { message?: string; errors?: { propertyName?: string; errorMessage?: string }[] }
      }
    }
    if (ax.response?.status === 400) {
      const first = ax.response.data?.errors?.[0]?.errorMessage
      error.value = first ?? ax.response.data?.message ?? 'Rango de fechas inválido.'
    } else {
      error.value = ax.response?.data?.message ?? 'No se pudo cargar el reporte.'
    }
  } finally {
    loading.value = false
  }
}

async function exportPdf() {
  exporting.value = true
  error.value = null
  try {
    if (tab.value === 'ventas') await reportesService.exportVentasPdf(desde.value, hasta.value)
    else if (tab.value === 'caja') await reportesService.exportCajaPdf(desde.value, hasta.value)
    else await reportesService.exportNominaPdf(desde.value, hasta.value)
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
    if (tab.value === 'ventas') await reportesService.exportVentasCsv(desde.value, hasta.value)
    else if (tab.value === 'caja') await reportesService.exportCajaCsv(desde.value, hasta.value)
    else await reportesService.exportNominaCsv(desde.value, hasta.value)
  } catch {
    error.value = 'No se pudo exportar el CSV.'
  } finally {
    exporting.value = false
  }
}

watch(tab, load)
onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Reportes</h1>
        <p>Ventas, caja (rango) y nómina (CU11). Fechas en UTC.</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button label="Actualizar" icon="pi pi-refresh" :loading="loading" @click="load" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>

    <div class="reportes-tabs" role="tablist">
      <button
        type="button"
        class="reportes-tab"
        :class="{ active: tab === 'ventas' }"
        @click="tab = 'ventas'"
      >
        Ventas
      </button>
      <button
        type="button"
        class="reportes-tab"
        :class="{ active: tab === 'caja' }"
        @click="tab = 'caja'"
      >
        Caja
      </button>
      <button
        type="button"
        class="reportes-tab"
        :class="{ active: tab === 'nomina' }"
        @click="tab = 'nomina'"
      >
        Nómina
      </button>
    </div>

    <div class="caja-toolbar">
      <label class="caja-fecha-label">
        Desde (UTC)
        <input v-model="desde" type="date" class="caja-fecha-input" />
      </label>
      <label class="caja-fecha-label">
        Hasta (UTC)
        <input v-model="hasta" type="date" class="caja-fecha-input" />
      </label>
      <Button label="Consultar" icon="pi pi-search" :loading="loading" @click="load" />
      <div class="caja-export-actions">
        <Button
          label="PDF"
          icon="pi pi-file-pdf"
          severity="secondary"
          :loading="exporting"
          :disabled="loading"
          @click="exportPdf"
        />
        <Button
          label="CSV"
          icon="pi pi-file"
          severity="secondary"
          :loading="exporting"
          :disabled="loading"
          @click="exportCsv"
        />
      </div>
    </div>

    <section v-if="tab === 'ventas' && ventas" class="caja-resumen">
      <p class="caja-fecha">
        Rango: <strong>{{ ventas.desde }}</strong> → <strong>{{ ventas.hasta }}</strong>
        · Pagos: {{ ventas.cantidadPagos }} · Pedidos: {{ ventas.cantidadPedidos }}
      </p>
      <dl class="caja-totales">
        <div>
          <dt>Efectivo</dt>
          <dd>{{ money(ventas.totalEfectivo) }}</dd>
        </div>
        <div>
          <dt>Tarjeta</dt>
          <dd>{{ money(ventas.totalTarjeta) }}</dd>
        </div>
        <div>
          <dt>Transferencia</dt>
          <dd>{{ money(ventas.totalTransferencia) }}</dd>
        </div>
        <div class="caja-total">
          <dt>Total</dt>
          <dd>{{ money(ventas.total) }}</dd>
        </div>
      </dl>
      <div class="reportes-table-wrap">
        <table class="reportes-table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Efectivo</th>
              <th>Tarjeta</th>
              <th>Transferencia</th>
              <th>Total</th>
              <th>Pagos</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="d in ventas.dias" :key="d.fecha">
              <td>{{ d.fecha }}</td>
              <td>{{ money(d.totalEfectivo) }}</td>
              <td>{{ money(d.totalTarjeta) }}</td>
              <td>{{ money(d.totalTransferencia) }}</td>
              <td>{{ money(d.total) }}</td>
              <td>{{ d.cantidadPagos }}</td>
            </tr>
            <tr v-if="!ventas.dias.length">
              <td colspan="6">Sin pagos completados en el rango.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section v-if="tab === 'caja' && caja" class="caja-resumen">
      <p class="caja-fecha">
        Rango: <strong>{{ caja.desde }}</strong> → <strong>{{ caja.hasta }}</strong>
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
      <div class="reportes-table-wrap">
        <table class="reportes-table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Efectivo</th>
              <th>Tarjeta</th>
              <th>Transferencia</th>
              <th>Total</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="d in caja.dias" :key="d.fecha">
              <td>{{ d.fecha }}</td>
              <td>{{ money(d.totalEfectivo) }}</td>
              <td>{{ money(d.totalTarjeta) }}</td>
              <td>{{ money(d.totalTransferencia) }}</td>
              <td>{{ money(d.total) }}</td>
            </tr>
            <tr v-if="!caja.dias.length">
              <td colspan="5">Sin filas de caja en el rango.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section v-if="tab === 'nomina' && nomina" class="caja-resumen">
      <p class="caja-fecha">
        Rango: <strong>{{ nomina.desde }}</strong> → <strong>{{ nomina.hasta }}</strong>
        · Horas: {{ nomina.totalHoras.toFixed(2) }} · Monto: {{ money(nomina.totalMonto) }}
      </p>
      <div class="reportes-table-wrap">
        <table class="reportes-table">
          <thead>
            <tr>
              <th>Empleado</th>
              <th>Periodo</th>
              <th>Horas</th>
              <th>Tarifa</th>
              <th>Monto</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="l in nomina.lineas" :key="l.id">
              <td>{{ l.empleadoNombre }}</td>
              <td>{{ l.periodoDesde }} → {{ l.periodoHasta }}</td>
              <td>{{ l.horas.toFixed(2) }}</td>
              <td>{{ money(l.tarifaHoraSnapshot) }}</td>
              <td>{{ money(l.monto) }}</td>
            </tr>
            <tr v-if="!nomina.lineas.length">
              <td colspan="5">Sin liquidaciones que intersecten el rango.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
