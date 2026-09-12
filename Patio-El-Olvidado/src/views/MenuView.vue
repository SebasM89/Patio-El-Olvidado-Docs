<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { productoService } from '../services/productoService'
import type { Producto, ProductoPayload } from '../types/producto'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Textarea from 'primevue/textarea'
import Checkbox from 'primevue/checkbox'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'

const auth = useAuthStore()
const isAdmin = computed(() => auth.rol === 'Admin')

const productos = ref<Producto[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const filters = reactive({
  q: '',
  categoria: '',
  etiqueta: '',
  soloActivos: true,
})

const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = reactive<ProductoPayload>({
  nombre: '',
  descripcion: '',
  precio: 0,
  categoria: '',
  imagen: '',
  etiquetas: '',
  activo: true,
})

const dialogTitle = computed(() => (editingId.value ? 'Editar producto' : 'Nuevo producto'))

async function load() {
  loading.value = true
  error.value = null
  try {
    const { data } = await productoService.list({
      q: filters.q || undefined,
      categoria: filters.categoria || undefined,
      etiqueta: filters.etiqueta || undefined,
      soloActivos: isAdmin.value ? filters.soloActivos : true,
    })
    productos.value = data
  } catch (e: unknown) {
    error.value =
      (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
      'No se pudo cargar el menú.'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, {
    nombre: '',
    descripcion: '',
    precio: 0,
    categoria: '',
    imagen: '',
    etiquetas: '',
    activo: true,
  })
  dialogVisible.value = true
}

function openEdit(p: Producto) {
  editingId.value = p.id
  Object.assign(form, {
    nombre: p.nombre,
    descripcion: p.descripcion ?? '',
    precio: p.precio,
    categoria: p.categoria,
    imagen: p.imagen ?? '',
    etiquetas: p.etiquetas ?? '',
    activo: p.activo,
  })
  dialogVisible.value = true
}

async function save() {
  if (!isAdmin.value) return
  error.value = null
  success.value = null
  const payload: ProductoPayload = {
    nombre: form.nombre.trim(),
    descripcion: form.descripcion?.toString().trim() || null,
    precio: Number(form.precio),
    categoria: form.categoria.trim(),
    imagen: form.imagen?.toString().trim() || null,
    etiquetas: form.etiquetas?.toString().trim() || null,
    activo: form.activo,
  }
  try {
    if (editingId.value) {
      await productoService.update(editingId.value, payload)
      success.value = 'Producto actualizado.'
    } else {
      await productoService.create(payload)
      success.value = 'Producto creado.'
    }
    dialogVisible.value = false
    await load()
  } catch (e: unknown) {
    const status = (e as { response?: { status?: number; data?: { message?: string } } })?.response
      ?.status
    if (status === 403) {
      error.value = 'Solo el administrador puede modificar el menú (RN-03).'
    } else {
      error.value =
        (e as { response?: { data?: { message?: string } } })?.response?.data?.message ??
        'No se pudo guardar el producto.'
    }
  }
}

async function remove(p: Producto) {
  if (!isAdmin.value) return
  if (!confirm(`¿Desactivar "${p.nombre}"? (soft-delete)`)) return
  error.value = null
  try {
    await productoService.remove(p.id)
    success.value = 'Producto desactivado.'
    await load()
  } catch (e: unknown) {
    const status = (e as { response?: { status?: number } })?.response?.status
    error.value =
      status === 403
        ? 'Solo el administrador puede modificar el menú (RN-03).'
        : 'No se pudo eliminar el producto.'
  }
}

function etiquetaList(raw: string | null): string[] {
  if (!raw) return []
  return raw.split(',').map((t) => t.trim()).filter(Boolean)
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Menú</h1>
        <p>Catálogo de productos (RF-02 / CU02)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button v-if="isAdmin" label="Nuevo producto" icon="pi pi-plus" @click="openCreate" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{
      success
    }}</Message>

    <section class="filters">
      <div class="filter-field">
        <label for="q">Buscar</label>
        <InputText id="q" v-model="filters.q" placeholder="Nombre o descripción" class="w-full" />
      </div>
      <div class="filter-field">
        <label for="categoria">Categoría</label>
        <InputText id="categoria" v-model="filters.categoria" placeholder="Ej. Bebidas" class="w-full" />
      </div>
      <div class="filter-field">
        <label for="etiqueta">Etiqueta</label>
        <InputText id="etiqueta" v-model="filters.etiqueta" placeholder="Ej. clasico" class="w-full" />
      </div>
      <div v-if="isAdmin" class="filter-field check">
        <label for="soloActivos">Solo activos</label>
        <Checkbox id="soloActivos" v-model="filters.soloActivos" binary />
      </div>
      <div class="filter-actions">
        <Button label="Filtrar" icon="pi pi-search" @click="load" :loading="loading" />
      </div>
    </section>

    <DataTable :value="productos" :loading="loading" striped-rows class="menu-table" data-key="id">
      <Column header="Producto">
        <template #body="{ data }">
          <div class="product-cell">
            <img
              v-if="data.imagen"
              :src="data.imagen"
              :alt="data.nombre"
              class="thumb"
              loading="lazy"
            />
            <div>
              <strong>{{ data.nombre }}</strong>
              <p class="desc">{{ data.descripcion || '—' }}</p>
            </div>
          </div>
        </template>
      </Column>
      <Column field="categoria" header="Categoría" />
      <Column header="Precio">
        <template #body="{ data }">
          {{ Number(data.precio).toLocaleString('es-AR', { style: 'currency', currency: 'ARS' }) }}
        </template>
      </Column>
      <Column header="Etiquetas">
        <template #body="{ data }">
          <div class="tags">
            <Tag v-for="t in etiquetaList(data.etiquetas)" :key="t" :value="t" severity="secondary" />
          </div>
        </template>
      </Column>
      <Column v-if="isAdmin" header="Estado">
        <template #body="{ data }">
          <Tag :value="data.activo ? 'Activo' : 'Inactivo'" :severity="data.activo ? 'success' : 'danger'" />
        </template>
      </Column>
      <Column v-if="isAdmin" header="Acciones" style="width: 10rem">
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
              @click="remove(data)"
            />
          </div>
        </template>
      </Column>
      <template #empty>No hay productos para mostrar.</template>
    </DataTable>

    <Dialog
      v-model:visible="dialogVisible"
      :header="dialogTitle"
      modal
      :style="{ width: 'min(520px, 95vw)' }"
    >
      <form class="product-form" @submit.prevent="save">
        <label for="nombre">Nombre</label>
        <InputText id="nombre" v-model="form.nombre" class="w-full" required />

        <label for="descripcion">Descripción</label>
        <Textarea id="descripcion" v-model="form.descripcion" rows="3" class="w-full" />

        <label for="precio">Precio</label>
        <InputNumber id="precio" v-model="form.precio" mode="currency" currency="ARS" locale="es-AR" class="w-full" />

        <label for="cat">Categoría</label>
        <InputText id="cat" v-model="form.categoria" class="w-full" required />

        <label for="imagen">Imagen (URL)</label>
        <InputText id="imagen" v-model="form.imagen" class="w-full" placeholder="https://..." />

        <label for="etiquetas">Etiquetas (separadas por coma)</label>
        <InputText id="etiquetas" v-model="form.etiquetas" class="w-full" placeholder="vegano,sin gluten" />

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
