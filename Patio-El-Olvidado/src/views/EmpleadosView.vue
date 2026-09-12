<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { empleadoService } from '../services/empleadoService'
import type {
  Empleado,
  EmpleadoPayload,
  Fichaje,
  Liquidacion,
  LiquidacionPreview,
} from '../types/empleado'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Checkbox from 'primevue/checkbox'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'

const auth = useAuthStore()
const isAdmin = computed(() => auth.rol === 'Admin')

const empleados = ref<Empleado[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const filters = reactive({
  q: '',
  soloActivos: true,
})

const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = reactive<EmpleadoPayload>({
  nombre: '',
  puesto: '',
  telefono: '',
  tarifaHora: 2500,
  usuarioId: null,
  activo: true,
})

const detalleVisible = ref(false)
const detalleEmpleado = ref<Empleado | null>(null)
const fichajes = ref<Fichaje[]>([])
const liquidaciones = ref<Liquidacion[]>([])
const preview = ref<LiquidacionPreview | null>(null)
const detalleLoading = ref(false)
const periodo = reactive({
  desde: new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10),
  hasta: new Date().toISOString().slice(0, 10),
})

const dialogTitle = computed(() => (editingId.value ? 'Editar empleado' : 'Nuevo empleado'))

function money(n: number) {
  return Number(n).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' })
}

function hours(n: number | null | undefined) {
  if (n == null) return '—'
  return Number(n).toLocaleString('es-AR', { maximumFractionDigits: 4 })
}

async function load() {
  if (!isAdmin.value) return
  loading.value = true
  error.value = null
  try {
    const { data } = await empleadoService.list({
      q: filters.q || undefined,
      activo: filters.soloActivos ? true : undefined,
    })
    empleados.value = data
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudieron cargar los empleados.'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, {
    nombre: '',
    puesto: '',
    telefono: '',
    tarifaHora: 2500,
    usuarioId: null,
    activo: true,
  })
  dialogVisible.value = true
}

function openEdit(e: Empleado) {
  editingId.value = e.id
  Object.assign(form, {
    nombre: e.nombre,
    puesto: e.puesto ?? '',
    telefono: e.telefono ?? '',
    tarifaHora: e.tarifaHora,
    usuarioId: e.usuarioId,
    activo: e.activo,
  })
  dialogVisible.value = true
}

async function save() {
  if (!isAdmin.value) return
  error.value = null
  success.value = null
  const payload: EmpleadoPayload = {
    nombre: form.nombre.trim(),
    puesto: form.puesto?.toString().trim() || null,
    telefono: form.telefono?.toString().trim() || null,
    tarifaHora: Number(form.tarifaHora),
    usuarioId: form.usuarioId || null,
    activo: form.activo,
  }
  try {
    if (editingId.value) {
      await empleadoService.update(editingId.value, payload)
      success.value = 'Empleado actualizado.'
    } else {
      await empleadoService.create(payload)
      success.value = 'Empleado creado.'
    }
    dialogVisible.value = false
    await load()
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo guardar el empleado.'
  }
}

async function remove(e: Empleado) {
  if (!isAdmin.value || !e.activo) return
  error.value = null
  success.value = null
  try {
    await empleadoService.remove(e.id)
    success.value = `Empleado "${e.nombre}" desactivado.`
    await load()
  } catch (err: unknown) {
    error.value =
      (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo desactivar el empleado.'
  }
}

async function openDetalle(e: Empleado) {
  detalleEmpleado.value = e
  detalleVisible.value = true
  detalleLoading.value = true
  fichajes.value = []
  liquidaciones.value = []
  preview.value = null
  try {
    const [f, l, p] = await Promise.all([
      empleadoService.listFichajes(e.id),
      empleadoService.listLiquidaciones(e.id),
      empleadoService.previewLiquidacion(e.id, periodo.desde, periodo.hasta),
    ])
    fichajes.value = f.data
    liquidaciones.value = l.data
    preview.value = p.data
  } catch (err: unknown) {
    error.value =
      (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo cargar el detalle.'
    detalleVisible.value = false
  } finally {
    detalleLoading.value = false
  }
}

async function refreshPreview() {
  if (!detalleEmpleado.value) return
  try {
    const { data } = await empleadoService.previewLiquidacion(
      detalleEmpleado.value.id,
      periodo.desde,
      periodo.hasta,
    )
    preview.value = data
  } catch (err: unknown) {
    error.value =
      (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo calcular el preview.'
  }
}

async function fichar(accion: 'entrada' | 'salida') {
  if (!detalleEmpleado.value) return
  error.value = null
  success.value = null
  try {
    if (accion === 'entrada') {
      await empleadoService.entrada(detalleEmpleado.value.id)
      success.value = 'Entrada registrada (asistida).'
    } else {
      await empleadoService.salida(detalleEmpleado.value.id)
      success.value = 'Salida registrada (asistida).'
    }
    await openDetalle(detalleEmpleado.value)
    await load()
  } catch (err: unknown) {
    error.value =
      (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo registrar el fichaje.'
  }
}

async function generarLiquidacion() {
  if (!detalleEmpleado.value) return
  error.value = null
  success.value = null
  try {
    await empleadoService.generarLiquidacion(detalleEmpleado.value.id, {
      periodoDesde: periodo.desde,
      periodoHasta: periodo.hasta,
    })
    success.value = 'Liquidación generada.'
    await openDetalle(detalleEmpleado.value)
  } catch (err: unknown) {
    error.value =
      (err as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo generar la liquidación.'
  }
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Empleados</h1>
        <p>ABM, tarifas, fichaje asistido y liquidaciones (RF-06 / RN-07)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button v-if="isAdmin" label="Nuevo empleado" icon="pi pi-plus" @click="openCreate" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{
      success
    }}</Message>

    <section v-if="isAdmin" class="filters">
      <div class="filter-field">
        <label for="q">Buscar</label>
        <InputText id="q" v-model="filters.q" placeholder="Nombre, puesto o teléfono" class="w-full" />
      </div>
      <div class="filter-field check">
        <label for="soloActivos">Solo activos</label>
        <Checkbox id="soloActivos" v-model="filters.soloActivos" binary />
      </div>
      <div class="filter-actions">
        <Button label="Filtrar" icon="pi pi-search" :loading="loading" @click="load" />
      </div>
    </section>

    <DataTable
      v-if="isAdmin"
      :value="empleados"
      :loading="loading"
      striped-rows
      class="menu-table"
      data-key="id"
    >
      <Column field="nombre" header="Nombre" />
      <Column header="Puesto">
        <template #body="{ data }">{{ data.puesto || '—' }}</template>
      </Column>
      <Column header="Tarifa/h">
        <template #body="{ data }">{{ money(data.tarifaHora) }}</template>
      </Column>
      <Column header="Horas">
        <template #body="{ data }">{{ hours(data.horasTrabajadas) }}</template>
      </Column>
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.activo ? 'Activo' : 'Inactivo'" :severity="data.activo ? 'success' : 'danger'" />
        </template>
      </Column>
      <Column header="Acciones" style="width: 14rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button icon="pi pi-clock" text rounded aria-label="Detalle" @click="openDetalle(data)" />
            <Button icon="pi pi-pencil" text rounded aria-label="Editar" @click="openEdit(data)" />
            <Button
              icon="pi pi-trash"
              text
              rounded
              severity="danger"
              aria-label="Desactivar"
              :disabled="!data.activo"
              @click="remove(data)"
            />
          </div>
        </template>
      </Column>
      <template #empty>No hay empleados para mostrar.</template>
    </DataTable>

    <Message v-else severity="warn">Solo el administrador gestiona empleados.</Message>

    <Dialog
      v-model:visible="dialogVisible"
      :header="dialogTitle"
      modal
      :style="{ width: 'min(480px, 95vw)' }"
    >
      <form class="product-form" @submit.prevent="save">
        <label for="nombre">Nombre</label>
        <InputText id="nombre" v-model="form.nombre" class="w-full" required />

        <label for="puesto">Puesto (opcional)</label>
        <InputText id="puesto" v-model="form.puesto" class="w-full" />

        <label for="telefono">Teléfono (opcional)</label>
        <InputText id="telefono" v-model="form.telefono" class="w-full" />

        <label for="tarifa">Tarifa por hora</label>
        <InputNumber
          id="tarifa"
          v-model="form.tarifaHora"
          mode="currency"
          currency="ARS"
          locale="es-AR"
          class="w-full"
          :min="0.01"
        />

        <label for="usuarioId">UsuarioId (opcional, rol Empleado)</label>
        <InputNumber id="usuarioId" v-model="form.usuarioId" class="w-full" :min="1" :use-grouping="false" />

        <div class="check-row">
          <Checkbox id="activo" v-model="form.activo" binary />
          <label for="activo">Activo</label>
        </div>

        <div class="form-actions">
          <Button type="button" label="Cancelar" severity="secondary" text @click="dialogVisible = false" />
          <Button type="submit" label="Guardar" />
        </div>
      </form>
    </Dialog>

    <Dialog
      v-model:visible="detalleVisible"
      :header="detalleEmpleado ? `Fichajes — ${detalleEmpleado.nombre}` : 'Detalle'"
      modal
      :style="{ width: 'min(860px, 96vw)' }"
    >
      <div class="detalle-actions">
        <Button label="Entrada asistida" icon="pi pi-sign-in" :loading="detalleLoading" @click="fichar('entrada')" />
        <Button
          label="Salida asistida"
          icon="pi pi-sign-out"
          severity="secondary"
          :loading="detalleLoading"
          @click="fichar('salida')"
        />
      </div>

      <section class="liq-box">
        <h3>Liquidación (preview)</h3>
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
            <Button label="Preview" @click="refreshPreview" />
            <Button label="Generar" severity="success" @click="generarLiquidacion" />
          </div>
        </div>
        <p v-if="preview">
          Horas: <strong>{{ hours(preview.horas) }}</strong> · Tarifa:
          <strong>{{ money(preview.tarifaHora) }}</strong> · Monto:
          <strong>{{ money(preview.monto) }}</strong>
        </p>
      </section>

      <h3>Fichajes</h3>
      <DataTable :value="fichajes" :loading="detalleLoading" striped-rows data-key="id">
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
        <template #empty>Sin fichajes.</template>
      </DataTable>

      <h3>Liquidaciones</h3>
      <DataTable :value="liquidaciones" :loading="detalleLoading" striped-rows data-key="id">
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
        <template #empty>Sin liquidaciones.</template>
      </DataTable>
    </Dialog>
  </div>
</template>

<style scoped>
.detalle-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  margin-bottom: 1rem;
}
.liq-box {
  margin-bottom: 1.25rem;
  padding: 0.75rem 0;
  border-top: 1px solid var(--p-content-border-color, #ddd);
  border-bottom: 1px solid var(--p-content-border-color, #ddd);
}
.liq-box h3,
h3 {
  margin: 0.75rem 0 0.5rem;
  font-size: 1rem;
}
</style>
