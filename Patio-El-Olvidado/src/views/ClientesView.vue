<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { clienteService } from '../services/clienteService'
import type { Cliente, ClientePayload, HistorialConsumoItem } from '../types/cliente'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Checkbox from 'primevue/checkbox'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'

const auth = useAuthStore()
const isAdmin = computed(() => auth.rol === 'Admin')
const isStaff = computed(() => auth.rol === 'Admin' || auth.rol === 'Empleado')

const clientes = ref<Cliente[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const filters = reactive({
  q: '',
  soloActivos: true,
})

const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = reactive<ClientePayload>({
  nombre: '',
  telefono: '',
  email: '',
  usuarioId: null,
  activo: true,
})

const historialVisible = ref(false)
const historialCliente = ref<Cliente | null>(null)
const historialItems = ref<HistorialConsumoItem[]>([])
const historialLoading = ref(false)

const dialogTitle = computed(() => (editingId.value ? 'Editar cliente' : 'Nuevo cliente'))

function money(n: number) {
  return Number(n).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' })
}

async function load() {
  if (!isStaff.value) return
  loading.value = true
  error.value = null
  try {
    const { data } = await clienteService.list({
      q: filters.q || undefined,
      activo: filters.soloActivos ? true : undefined,
    })
    clientes.value = data
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudieron cargar los clientes.'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, {
    nombre: '',
    telefono: '',
    email: '',
    usuarioId: null,
    activo: true,
  })
  dialogVisible.value = true
}

function openEdit(c: Cliente) {
  editingId.value = c.id
  Object.assign(form, {
    nombre: c.nombre,
    telefono: c.telefono,
    email: c.email ?? '',
    usuarioId: c.usuarioId,
    activo: c.activo,
  })
  dialogVisible.value = true
}

async function save() {
  if (!isStaff.value) return
  error.value = null
  success.value = null
  const payload: ClientePayload = {
    nombre: form.nombre.trim(),
    telefono: form.telefono.trim(),
    email: form.email?.toString().trim() || null,
    usuarioId: form.usuarioId || null,
    activo: form.activo,
  }
  try {
    if (editingId.value) {
      await clienteService.update(editingId.value, payload)
      success.value = 'Cliente actualizado.'
    } else {
      await clienteService.create(payload)
      success.value = 'Cliente creado.'
    }
    dialogVisible.value = false
    await load()
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo guardar el cliente.'
  }
}

async function remove(c: Cliente) {
  if (!isAdmin.value || !c.activo) return
  error.value = null
  success.value = null
  try {
    await clienteService.remove(c.id)
    success.value = `Cliente "${c.nombre}" desactivado.`
    await load()
  } catch (e: unknown) {
    const status = (e as { response?: { status?: number; data?: { message?: string } } })?.response
      ?.status
    const msg = (e as { response?: { data?: { message?: string } } })?.response?.data?.message
    if (status === 403) {
      error.value = msg ?? 'Solo el administrador puede desactivar clientes.'
    } else {
      error.value = msg ?? 'No se pudo desactivar el cliente.'
    }
  }
}

async function openHistorial(c: Cliente) {
  historialCliente.value = c
  historialVisible.value = true
  historialLoading.value = true
  historialItems.value = []
  try {
    const { data } = await clienteService.historial(c.id)
    historialItems.value = data
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo cargar el historial.'
    historialVisible.value = false
  } finally {
    historialLoading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Clientes</h1>
        <p>Registro, historial y fidelización (RF-05 / RN-05)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button v-if="isStaff" label="Nuevo cliente" icon="pi pi-plus" @click="openCreate" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{
      success
    }}</Message>

    <section class="filters">
      <div class="filter-field">
        <label for="q">Buscar</label>
        <InputText id="q" v-model="filters.q" placeholder="Nombre, teléfono o email" class="w-full" />
      </div>
      <div class="filter-field check">
        <label for="soloActivos">Solo activos</label>
        <Checkbox id="soloActivos" v-model="filters.soloActivos" binary />
      </div>
      <div class="filter-actions">
        <Button label="Filtrar" icon="pi pi-search" :loading="loading" @click="load" />
      </div>
    </section>

    <DataTable :value="clientes" :loading="loading" striped-rows class="menu-table" data-key="id">
      <Column field="nombre" header="Nombre" />
      <Column field="telefono" header="Teléfono" />
      <Column header="Email">
        <template #body="{ data }">{{ data.email || '—' }}</template>
      </Column>
      <Column field="visitas" header="Visitas" style="width: 6rem" />
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.activo ? 'Activo' : 'Inactivo'" :severity="data.activo ? 'success' : 'danger'" />
        </template>
      </Column>
      <Column header="Acciones" style="width: 12rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button icon="pi pi-history" text rounded aria-label="Historial" @click="openHistorial(data)" />
            <Button icon="pi pi-pencil" text rounded aria-label="Editar" @click="openEdit(data)" />
            <Button
              v-if="isAdmin"
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
      <template #empty>No hay clientes para mostrar.</template>
    </DataTable>

    <Dialog
      v-model:visible="dialogVisible"
      :header="dialogTitle"
      modal
      :style="{ width: 'min(480px, 95vw)' }"
    >
      <form class="product-form" @submit.prevent="save">
        <label for="nombre">Nombre</label>
        <InputText id="nombre" v-model="form.nombre" class="w-full" required />

        <label for="telefono">Teléfono</label>
        <InputText id="telefono" v-model="form.telefono" class="w-full" required />

        <label for="email">Email (opcional)</label>
        <InputText id="email" v-model="form.email" type="email" class="w-full" />

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
      v-model:visible="historialVisible"
      :header="historialCliente ? `Historial — ${historialCliente.nombre}` : 'Historial'"
      modal
      :style="{ width: 'min(720px, 95vw)' }"
    >
      <DataTable :value="historialItems" :loading="historialLoading" striped-rows data-key="pedidoId">
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
        <Column header="Desc.">
          <template #body="{ data }">
            {{ data.descuentoMonto > 0 ? money(data.descuentoMonto) : '—' }}
          </template>
        </Column>
        <Column header="Cobrado">
          <template #body="{ data }">{{ money(data.pagosCompletados) }}</template>
        </Column>
        <template #empty>Sin pedidos vinculados.</template>
      </DataTable>
    </Dialog>
  </div>
</template>
