<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { pedidoService } from '../services/pedidoService'
import { productoService } from '../services/productoService'
import type { Pedido, PedidoPayload, PedidoTipo } from '../types/pedido'
import type { Producto } from '../types/producto'
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'

const auth = useAuthStore()
const isStaff = computed(() => auth.rol === 'Admin' || auth.rol === 'Empleado')
const isCliente = computed(() => auth.rol === 'Cliente')

const pedidos = ref<Pedido[]>([])
const productos = ref<Producto[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const filters = reactive({
  estado: '' as string,
  tipo: '' as string,
})

const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = reactive<{
  tipo: PedidoTipo
  lineas: { productoId: number | null; cantidad: number }[]
}>({
  tipo: 'Local',
  lineas: [{ productoId: null, cantidad: 1 }],
})

const tipoOptions = [
  { label: 'Local', value: 'Local' },
  { label: 'Para llevar', value: 'ParaLlevar' },
]

const estadoOptions = [
  { label: 'Todos', value: '' },
  { label: 'En preparación', value: 'EnPreparacion' },
  { label: 'Listo', value: 'Listo' },
  { label: 'Entregado', value: 'Entregado' },
  { label: 'Cancelado', value: 'Cancelado' },
]

const dialogTitle = computed(() => (editingId.value ? 'Editar pedido' : 'Nuevo pedido'))

const formSubtotal = computed(() =>
  form.lineas.reduce((acc, line) => {
    const p = productos.value.find((x) => x.id === line.productoId)
    if (!p) return acc
    return acc + p.precio * (line.cantidad || 0)
  }, 0),
)

function money(n: number) {
  return Number(n).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' })
}

function estadoSeverity(estado: string) {
  switch (estado) {
    case 'EnPreparacion':
      return 'warn'
    case 'Listo':
      return 'info'
    case 'Entregado':
      return 'success'
    case 'Cancelado':
      return 'danger'
    default:
      return 'secondary'
  }
}

function estadoLabel(estado: string) {
  switch (estado) {
    case 'EnPreparacion':
      return 'En preparación'
    case 'ParaLlevar':
      return 'Para llevar'
    default:
      return estado
  }
}

async function loadProductos() {
  const { data } = await productoService.list({ soloActivos: true })
  productos.value = data
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const { data } = await pedidoService.list({
      estado: filters.estado || undefined,
      tipo: filters.tipo || undefined,
    })
    pedidos.value = data
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudieron cargar los pedidos.'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  form.tipo = 'Local'
  form.lineas = [{ productoId: null, cantidad: 1 }]
  dialogVisible.value = true
}

function openEdit(p: Pedido) {
  if (p.estado !== 'EnPreparacion') return
  editingId.value = p.id
  form.tipo = (p.tipo as PedidoTipo) || 'Local'
  form.lineas = p.detalles.map((d) => ({
    productoId: d.productoId,
    cantidad: d.cantidad,
  }))
  dialogVisible.value = true
}

function addLinea() {
  form.lineas.push({ productoId: null, cantidad: 1 })
}

function removeLinea(idx: number) {
  if (form.lineas.length <= 1) return
  form.lineas.splice(idx, 1)
}

function buildPayload(): PedidoPayload | null {
  const detalles = form.lineas
    .filter((l) => l.productoId && l.cantidad > 0)
    .map((l) => ({ productoId: l.productoId as number, cantidad: l.cantidad }))
  if (!detalles.length) {
    error.value = 'Agregá al menos un producto.'
    return null
  }
  return { tipo: form.tipo, clienteId: null, detalles }
}

async function save() {
  error.value = null
  success.value = null
  const payload = buildPayload()
  if (!payload) return
  try {
    if (editingId.value) {
      await pedidoService.update(editingId.value, payload)
      success.value = 'Pedido actualizado.'
    } else {
      await pedidoService.create(payload)
      success.value = 'Pedido creado.'
    }
    dialogVisible.value = false
    await load()
  } catch (e: unknown) {
    const status = (e as { response?: { status?: number; data?: { message?: string } } })?.response
      ?.status
    const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message
    if (status === 409) {
      error.value = msg ?? 'No se puede modificar el pedido en este estado (RN-04).'
    } else {
      error.value = msg ?? 'No se pudo guardar el pedido.'
    }
  }
}

async function cambiarEstado(p: Pedido, estado: 'Listo' | 'Entregado' | 'Cancelado') {
  error.value = null
  success.value = null
  try {
    await pedidoService.cambiarEstado(p.id, { estado })
    success.value = `Pedido #${p.id} → ${estadoLabel(estado)}.`
    await load()
  } catch (e: unknown) {
    const status = (e as { response?: { status?: number; data?: { message?: string } } })?.response
      ?.status
    const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message
    if (status === 403) {
      error.value = msg ?? 'No tenés permiso para ese cambio de estado.'
    } else if (status === 409) {
      error.value = msg ?? 'Transición de estado no permitida.'
    } else {
      error.value = msg ?? 'No se pudo cambiar el estado.'
    }
  }
}

function canEdit(p: Pedido) {
  return p.estado === 'EnPreparacion'
}

function canCancel(p: Pedido) {
  return p.estado === 'EnPreparacion' || (isStaff.value && p.estado === 'Listo')
}

onMounted(async () => {
  try {
    await loadProductos()
  } catch {
    /* listado de productos puede fallar; el alta lo mostrará */
  }
  await load()
})
</script>

<template>
  <div class="menu-page pedidos-page">
    <header class="menu-header">
      <div>
        <h1>Pedidos</h1>
        <p>Local y para llevar (RF-03 / CU03)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button label="Nuevo pedido" icon="pi pi-plus" @click="openCreate" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{
      success
    }}</Message>

    <section class="filters">
      <div class="filter-field">
        <label for="estado">Estado</label>
        <Select
          id="estado"
          v-model="filters.estado"
          :options="estadoOptions"
          option-label="label"
          option-value="value"
          class="w-full"
        />
      </div>
      <div class="filter-field">
        <label for="tipo">Tipo</label>
        <Select
          id="tipo"
          v-model="filters.tipo"
          :options="[{ label: 'Todos', value: '' }, ...tipoOptions]"
          option-label="label"
          option-value="value"
          class="w-full"
        />
      </div>
      <div class="filter-actions">
        <Button label="Filtrar" icon="pi pi-search" :loading="loading" @click="load" />
      </div>
    </section>

    <DataTable :value="pedidos" :loading="loading" striped-rows class="menu-table" data-key="id">
      <Column field="id" header="#" style="width: 4rem" />
      <Column header="Tipo">
        <template #body="{ data }">
          {{ data.tipo === 'ParaLlevar' ? 'Para llevar' : data.tipo }}
        </template>
      </Column>
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="estadoLabel(data.estado)" :severity="estadoSeverity(data.estado)" />
        </template>
      </Column>
      <Column header="Total">
        <template #body="{ data }">{{ money(data.total) }}</template>
      </Column>
      <Column header="Fecha">
        <template #body="{ data }">
          {{ new Date(data.fechaCreacion).toLocaleString('es-AR') }}
        </template>
      </Column>
      <Column header="Líneas">
        <template #body="{ data }">
          <ul class="lineas-mini">
            <li v-for="d in data.detalles" :key="d.id">
              {{ d.cantidad }}× {{ d.productoNombre || `#${d.productoId}` }}
            </li>
          </ul>
        </template>
      </Column>
      <Column header="Acciones" style="width: 14rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button
              v-if="canEdit(data)"
              icon="pi pi-pencil"
              text
              rounded
              aria-label="Editar"
              @click="openEdit(data)"
            />
            <Button
              v-if="isStaff && data.estado === 'EnPreparacion'"
              label="Listo"
              size="small"
              severity="info"
              text
              @click="cambiarEstado(data, 'Listo')"
            />
            <Button
              v-if="isStaff && data.estado === 'Listo'"
              label="Entregar"
              size="small"
              severity="success"
              text
              @click="cambiarEstado(data, 'Entregado')"
            />
            <Button
              v-if="canCancel(data)"
              label="Cancelar"
              size="small"
              severity="danger"
              text
              @click="cambiarEstado(data, 'Cancelado')"
            />
          </div>
        </template>
      </Column>
      <template #empty>No hay pedidos para mostrar.</template>
    </DataTable>

    <p v-if="isCliente" class="hint">
      Como cliente solo ves y gestionás tus propios pedidos. Podés cancelar mientras estén en
      preparación.
    </p>

    <Dialog
      v-model:visible="dialogVisible"
      :header="dialogTitle"
      modal
      :style="{ width: 'min(560px, 95vw)' }"
    >
      <form class="product-form" @submit.prevent="save">
        <label for="tipoPedido">Tipo</label>
        <Select
          id="tipoPedido"
          v-model="form.tipo"
          :options="tipoOptions"
          option-label="label"
          option-value="value"
          class="w-full"
        />

        <div class="lineas-editor">
          <div v-for="(line, idx) in form.lineas" :key="idx" class="linea-row">
            <Select
              v-model="line.productoId"
              :options="productos"
              option-label="nombre"
              option-value="id"
              placeholder="Producto"
              class="w-full"
              filter
            />
            <InputNumber v-model="line.cantidad" :min="1" show-buttons class="qty" />
            <Button
              type="button"
              icon="pi pi-trash"
              severity="danger"
              text
              rounded
              :disabled="form.lineas.length <= 1"
              @click="removeLinea(idx)"
            />
          </div>
          <Button type="button" label="Agregar línea" icon="pi pi-plus" text @click="addLinea" />
        </div>

        <p class="subtotal-preview"><strong>Subtotal estimado:</strong> {{ money(formSubtotal) }}</p>

        <div class="form-actions">
          <Button type="button" label="Cancelar" severity="secondary" text @click="dialogVisible = false" />
          <Button type="submit" label="Guardar" />
        </div>
      </form>
    </Dialog>
  </div>
</template>
