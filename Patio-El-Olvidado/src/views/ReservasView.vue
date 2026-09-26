<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { clienteService } from '../services/clienteService'
import { mesaService, reservaService } from '../services/reservaService'
import type { Cliente } from '../types/cliente'
import type { Mesa, Reserva } from '../types/reserva'
import Button from 'primevue/button'
import Select from 'primevue/select'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import InputNumber from 'primevue/inputnumber'

const auth = useAuthStore()
const isStaff = computed(() => auth.rol === 'Admin' || auth.rol === 'Empleado')
const isCliente = computed(() => auth.rol === 'Cliente')

const mesas = ref<Mesa[]>([])
const clientes = ref<Cliente[]>([])
const reservas = ref<Reserva[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)
const dialogVisible = ref(false)

const filters = reactive({
  fecha: todayIso(),
  mesaId: null as number | null,
})

const form = reactive({
  mesaId: null as number | null,
  fecha: todayIso(),
  horaInicio: '20:00',
  horaFin: '21:30',
  personas: 2,
  clienteId: null as number | null,
})

const mesaOptions = computed(() =>
  mesas.value.map((m) => ({
    label: `Mesa ${m.numero} · ${m.capacidad} pers.${m.ubicacion ? ` · ${m.ubicacion}` : ''}`,
    value: m.id,
  })),
)

const filtroMesaOptions = computed(() => [
  { label: 'Todas las mesas', value: null as number | null },
  ...mesaOptions.value,
])

const clienteOptions = computed(() =>
  clientes.value.map((c) => ({
    label: `${c.nombre} · ${c.telefono}`,
    value: c.id,
  })),
)

const mesaSeleccionada = computed(
  () => mesas.value.find((m) => m.id === form.mesaId) ?? null,
)

function todayIso() {
  const d = new Date()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${month}-${day}`
}

function fechaLabel(iso: string) {
  const day = iso.slice(0, 10)
  const [y, m, d] = day.split('-')
  if (!y || !m || !d) return iso
  return `${d}/${m}/${y}`
}

function horaCorta(value: string) {
  return value?.slice(0, 5) ?? ''
}

function toApiTime(value: string) {
  return value.length === 5 ? `${value}:00` : value
}

function estadoSeverity(estado: string) {
  switch (estado) {
    case 'Confirmada':
      return 'success'
    case 'Cancelada':
      return 'danger'
    case 'Finalizada':
      return 'secondary'
    default:
      return 'info'
  }
}

function apiMessage(e: unknown, fallback: string) {
  return (
    (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? fallback
  )
}

async function loadMesas() {
  const { data } = await mesaService.list()
  mesas.value = data
}

async function loadClientes() {
  if (!isStaff.value) return
  const { data } = await clienteService.list({ activo: true })
  clientes.value = data
}

async function load() {
  loading.value = true
  error.value = null
  try {
    if (isStaff.value) {
      if (!filters.fecha) {
        error.value = 'Elegí la fecha de la agenda.'
        reservas.value = []
        return
      }
      const { data } = await reservaService.list({
        fecha: filters.fecha,
        mesaId: filters.mesaId ?? undefined,
      })
      reservas.value = data
    } else {
      const { data } = await reservaService.list()
      reservas.value = data
    }
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudieron cargar las reservas.')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  error.value = null
  form.mesaId = mesas.value[0]?.id ?? null
  form.fecha = isStaff.value ? filters.fecha || todayIso() : todayIso()
  form.horaInicio = '20:00'
  form.horaFin = '21:30'
  form.personas = 2
  form.clienteId = null
  dialogVisible.value = true
}

function validarFormulario(): string | null {
  if (!form.mesaId) return 'Elegí una mesa.'
  if (!form.fecha) return 'La fecha es obligatoria.'
  if (!form.horaInicio || !form.horaFin) return 'Completá el horario.'
  if (form.horaFin <= form.horaInicio) return 'La hora de fin debe ser posterior a la de inicio.'
  if (!form.personas || form.personas <= 0) return 'Indicá la cantidad de personas.'
  const mesa = mesaSeleccionada.value
  if (mesa && form.personas > mesa.capacidad) {
    return `La mesa ${mesa.numero} admite hasta ${mesa.capacidad} personas.`
  }
  if (isStaff.value && !form.clienteId) return 'Elegí un cliente activo.'
  return null
}

async function save() {
  error.value = null
  success.value = null
  const local = validarFormulario()
  if (local) {
    error.value = local
    return
  }
  try {
    await reservaService.create({
      mesaId: form.mesaId as number,
      fecha: form.fecha,
      horaInicio: toApiTime(form.horaInicio),
      horaFin: toApiTime(form.horaFin),
      personas: form.personas,
      clienteId: isStaff.value ? form.clienteId : undefined,
    })
    success.value = 'Reserva confirmada.'
    dialogVisible.value = false
    if (isStaff.value) filters.fecha = form.fecha
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo crear la reserva.')
  }
}

async function cambiarEstado(reserva: Reserva, estado: 'Cancelada' | 'Finalizada') {
  error.value = null
  success.value = null
  try {
    await reservaService.cambiarEstado(reserva.id, estado)
    success.value = estado === 'Cancelada' ? 'Reserva cancelada.' : 'Reserva finalizada.'
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo cambiar el estado.')
  }
}

onMounted(async () => {
  try {
    await loadMesas()
    await loadClientes()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo cargar el catálogo de mesas.')
  }
  await load()
})
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Reservas</h1>
        <p>
          {{
            isCliente
              ? 'Elegí mesa, día y horario. Solo ves y cancelás las tuyas.'
              : 'Agenda del día. Una reserva confirmada no se superpone en la misma mesa (RN-06).'
          }}
        </p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button label="Nueva reserva" icon="pi pi-plus" @click="openCreate" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{
      success
    }}</Message>

    <section v-if="isStaff" class="filters">
      <div class="filter-field">
        <label for="fechaAgenda">Fecha</label>
        <input id="fechaAgenda" v-model="filters.fecha" type="date" class="caja-fecha-input" />
      </div>
      <div class="filter-field">
        <label for="mesaAgenda">Mesa</label>
        <Select
          id="mesaAgenda"
          v-model="filters.mesaId"
          :options="filtroMesaOptions"
          option-label="label"
          option-value="value"
          class="w-full"
        />
      </div>
      <div class="filter-actions">
        <Button label="Ver agenda" icon="pi pi-search" :loading="loading" @click="load" />
      </div>
    </section>

    <DataTable :value="reservas" :loading="loading" striped-rows class="menu-table" data-key="id">
      <Column field="mesaNumero" header="Mesa" style="width: 5rem" />
      <Column v-if="isStaff" field="clienteNombre" header="Cliente" />
      <Column header="Fecha">
        <template #body="{ data }">{{ fechaLabel(data.fecha) }}</template>
      </Column>
      <Column header="Horario">
        <template #body="{ data }">
          {{ horaCorta(data.horaInicio) }} – {{ horaCorta(data.horaFin) }}
        </template>
      </Column>
      <Column field="personas" header="Personas" style="width: 6rem" />
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.estado" :severity="estadoSeverity(data.estado)" />
        </template>
      </Column>
      <Column header="Acciones" style="width: 14rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button
              v-if="data.estado === 'Confirmada'"
              label="Cancelar"
              size="small"
              severity="danger"
              text
              @click="cambiarEstado(data, 'Cancelada')"
            />
            <Button
              v-if="isStaff && data.estado === 'Confirmada'"
              label="Finalizar"
              size="small"
              severity="secondary"
              text
              @click="cambiarEstado(data, 'Finalizada')"
            />
          </div>
        </template>
      </Column>
      <template #empty>
        {{ isCliente ? 'Todavía no tenés reservas.' : 'No hay reservas para esa fecha.' }}
      </template>
    </DataTable>

    <Dialog
      v-model:visible="dialogVisible"
      header="Nueva reserva"
      modal
      :style="{ width: 'min(480px, 95vw)' }"
    >
      <form class="product-form" @submit.prevent="save">
        <label v-if="isStaff" for="clienteReserva">Cliente</label>
        <Select
          v-if="isStaff"
          id="clienteReserva"
          v-model="form.clienteId"
          :options="clienteOptions"
          option-label="label"
          option-value="value"
          placeholder="Cliente activo"
          class="w-full"
          filter
        />

        <label for="mesaReserva">Mesa</label>
        <Select
          id="mesaReserva"
          v-model="form.mesaId"
          :options="mesaOptions"
          option-label="label"
          option-value="value"
          placeholder="Mesa"
          class="w-full"
        />

        <label for="fechaReserva">Fecha</label>
        <input id="fechaReserva" v-model="form.fecha" type="date" class="caja-fecha-input" required />

        <label for="inicioReserva">Hora de inicio</label>
        <input id="inicioReserva" v-model="form.horaInicio" type="time" class="caja-fecha-input" required />

        <label for="finReserva">Hora de fin</label>
        <input id="finReserva" v-model="form.horaFin" type="time" class="caja-fecha-input" required />

        <label for="personasReserva">Personas</label>
        <InputNumber
          id="personasReserva"
          v-model="form.personas"
          :min="1"
          :max="mesaSeleccionada?.capacidad"
          show-buttons
        />
        <p v-if="mesaSeleccionada" class="hint">
          Capacidad de la mesa {{ mesaSeleccionada.numero }}: {{ mesaSeleccionada.capacidad }} personas.
        </p>

        <div class="form-actions">
          <Button type="button" label="Cerrar" severity="secondary" text @click="dialogVisible = false" />
          <Button type="submit" label="Confirmar" />
        </div>
      </form>
    </Dialog>
  </div>
</template>
