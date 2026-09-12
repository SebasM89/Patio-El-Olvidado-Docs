<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { pagoService } from '../services/pagoService'
import type { CajaDia } from '../types/pago'
import Button from 'primevue/button'
import Message from 'primevue/message'

const loading = ref(false)
const error = ref<string | null>(null)
const caja = ref<CajaDia | null>(null)

function money(n: number) {
  return Number(n).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' })
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const { data } = await pagoService.cajaHoy()
    caja.value = data
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo cargar la caja del día.'
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
        <h1>Caja del día</h1>
        <p>Resumen mínimo (RN-08). Sin export PDF/CSV.</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button label="Actualizar" icon="pi pi-refresh" :loading="loading" @click="load" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>

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
