<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { proveedorService } from '../services/proveedorService'
import type { CreateProveedorPayload, Proveedor, UpdateProveedorPayload } from '../types/proveedor'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import Checkbox from 'primevue/checkbox'
import Select from 'primevue/select'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'

const auth = useAuthStore()
const isAdmin = computed(() => auth.rol === 'Admin')

const items = ref<Proveedor[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const filters = reactive({
  q: '',
  activo: 'todos' as 'todos' | 'activos' | 'inactivos',
})

const activoOptions = [
  { label: 'Todos', value: 'todos' },
  { label: 'Activos', value: 'activos' },
  { label: 'Inactivos', value: 'inactivos' },
]

const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = reactive({
  nombre: '',
  contacto: '',
  telefono: '',
  email: '',
  notas: '',
  activo: true,
})

const dialogTitle = computed(() => (editingId.value ? 'Editar proveedor' : 'Nuevo proveedor'))

function apiMessage(e: unknown, fallback: string) {
  return (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? fallback
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const { data } = await proveedorService.list({
      q: filters.q || undefined,
      activo: filters.activo === 'todos' ? undefined : filters.activo === 'activos',
    })
    items.value = data
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudieron cargar los proveedores.')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, {
    nombre: '',
    contacto: '',
    telefono: '',
    email: '',
    notas: '',
    activo: true,
  })
  dialogVisible.value = true
}

function openEdit(item: Proveedor) {
  editingId.value = item.id
  Object.assign(form, {
    nombre: item.nombre,
    contacto: item.contacto ?? '',
    telefono: item.telefono ?? '',
    email: item.email ?? '',
    notas: item.notas ?? '',
    activo: item.activo,
  })
  dialogVisible.value = true
}

function payloadFromForm(): CreateProveedorPayload {
  return {
    nombre: form.nombre.trim(),
    contacto: form.contacto.trim() || null,
    telefono: form.telefono.trim() || null,
    email: form.email.trim() || null,
    notas: form.notas.trim() || null,
    activo: form.activo,
  }
}

async function save() {
  if (!isAdmin.value) return
  error.value = null
  success.value = null
  try {
    if (editingId.value) {
      const payload: UpdateProveedorPayload = payloadFromForm()
      await proveedorService.update(editingId.value, payload)
      success.value = 'Proveedor actualizado.'
    } else {
      await proveedorService.create(payloadFromForm())
      success.value = 'Proveedor creado.'
    }
    dialogVisible.value = false
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo guardar el proveedor.')
  }
}

async function desactivar(item: Proveedor) {
  if (!isAdmin.value || !item.activo) return
  error.value = null
  success.value = null
  try {
    await proveedorService.update(item.id, {
      nombre: item.nombre,
      contacto: item.contacto ?? null,
      telefono: item.telefono ?? null,
      email: item.email ?? null,
      notas: item.notas ?? null,
      activo: false,
    })
    success.value = `"${item.nombre}" quedó inactivo.`
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo desactivar el proveedor.')
  }
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Proveedores</h1>
        <p>Catálogo para reposición de stock (RN-11)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button v-if="isAdmin" label="Nuevo proveedor" icon="pi pi-plus" @click="openCreate" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{
      success
    }}</Message>

    <section class="filters">
      <div class="filter-field">
        <label for="q">Buscar</label>
        <InputText id="q" v-model="filters.q" placeholder="Nombre, contacto o email" class="w-full" />
      </div>
      <div class="filter-field">
        <label for="activo">Estado</label>
        <Select
          id="activo"
          v-model="filters.activo"
          :options="activoOptions"
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
      <Column field="nombre" header="Nombre" />
      <Column header="Contacto">
        <template #body="{ data }">{{ data.contacto || '—' }}</template>
      </Column>
      <Column header="Teléfono">
        <template #body="{ data }">{{ data.telefono || '—' }}</template>
      </Column>
      <Column header="Email">
        <template #body="{ data }">{{ data.email || '—' }}</template>
      </Column>
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.activo ? 'Activo' : 'Inactivo'" :severity="data.activo ? 'success' : 'danger'" />
        </template>
      </Column>
      <Column v-if="isAdmin" header="Acciones" style="width: 8rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button icon="pi pi-pencil" text rounded aria-label="Editar" @click="openEdit(data)" />
            <Button
              icon="pi pi-trash"
              text
              rounded
              severity="danger"
              aria-label="Desactivar"
              :disabled="!data.activo"
              @click="desactivar(data)"
            />
          </div>
        </template>
      </Column>
      <template #empty>No hay proveedores para mostrar.</template>
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

        <label for="contacto">Contacto</label>
        <InputText id="contacto" v-model="form.contacto" class="w-full" />

        <label for="telefono">Teléfono</label>
        <InputText id="telefono" v-model="form.telefono" class="w-full" />

        <label for="email">Email</label>
        <InputText id="email" v-model="form.email" type="email" class="w-full" />

        <label for="notas">Notas</label>
        <Textarea id="notas" v-model="form.notas" rows="2" class="w-full" auto-resize />

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
  </div>
</template>
