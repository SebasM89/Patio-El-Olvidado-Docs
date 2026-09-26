<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { promocionService } from '../services/promocionService'
import type { CreatePromocionPayload, Promocion, UpdatePromocionPayload } from '../types/promocion'
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

const items = ref<Promocion[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const filters = reactive({
  q: '',
  activo: 'todos' as 'todos' | 'activos' | 'inactivos',
  vigente: 'todos' as 'todos' | 'vigentes' | 'no-vigentes',
})

const activoOptions = [
  { label: 'Todos', value: 'todos' },
  { label: 'Activos', value: 'activos' },
  { label: 'Inactivos', value: 'inactivos' },
]

const vigenteOptions = [
  { label: 'Todas', value: 'todos' },
  { label: 'Vigentes hoy', value: 'vigentes' },
  { label: 'Fuera de vigencia', value: 'no-vigentes' },
]

const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = reactive({
  nombre: '',
  descripcion: '',
  vigenteDesde: '',
  vigenteHasta: '',
  activo: true,
})

const dialogTitle = computed(() => (editingId.value ? 'Editar promoción' : 'Nueva promoción'))

function hoyUtc() {
  const d = new Date()
  const y = d.getUTCFullYear()
  const m = String(d.getUTCMonth() + 1).padStart(2, '0')
  const day = String(d.getUTCDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function formatFecha(iso: string) {
  const [y, m, d] = iso.slice(0, 10).split('-')
  if (!y || !m || !d) return iso
  return `${d}/${m}/${y}`
}

function apiMessage(e: unknown, fallback: string) {
  const data = (e as { response?: { data?: { message?: string; errors?: { errorMessage?: string }[] } } })
    ?.response?.data
  const first = data?.errors?.find((x) => x.errorMessage)?.errorMessage
  return first ?? data?.message ?? fallback
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const { data } = await promocionService.list(
      isAdmin.value
        ? {
            q: filters.q || undefined,
            activo: filters.activo === 'todos' ? undefined : filters.activo === 'activos',
            vigente: filters.vigente === 'todos' ? undefined : filters.vigente === 'vigentes',
          }
        : {},
    )
    items.value = data
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudieron cargar las promociones.')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  const hoy = hoyUtc()
  Object.assign(form, {
    nombre: '',
    descripcion: '',
    vigenteDesde: hoy,
    vigenteHasta: hoy,
    activo: true,
  })
  dialogVisible.value = true
}

function openEdit(item: Promocion) {
  editingId.value = item.id
  Object.assign(form, {
    nombre: item.nombre,
    descripcion: item.descripcion,
    vigenteDesde: item.vigenteDesde.slice(0, 10),
    vigenteHasta: item.vigenteHasta.slice(0, 10),
    activo: item.activo,
  })
  dialogVisible.value = true
}

function payloadBase(): CreatePromocionPayload {
  return {
    nombre: form.nombre.trim(),
    descripcion: form.descripcion.trim(),
    vigenteDesde: form.vigenteDesde,
    vigenteHasta: form.vigenteHasta,
  }
}

async function save() {
  if (!isAdmin.value) return
  error.value = null
  success.value = null
  if (!form.nombre.trim() || !form.descripcion.trim() || !form.vigenteDesde || !form.vigenteHasta) {
    error.value = 'Completá nombre, descripción y vigencia.'
    return
  }
  if (form.vigenteHasta < form.vigenteDesde) {
    error.value = 'La vigencia hasta no puede ser anterior a la vigencia desde.'
    return
  }
  try {
    if (editingId.value) {
      const payload: UpdatePromocionPayload = { ...payloadBase(), activo: form.activo }
      await promocionService.update(editingId.value, payload)
      success.value = 'Promoción actualizada.'
    } else {
      await promocionService.create(payloadBase())
      success.value = 'Promoción creada.'
    }
    dialogVisible.value = false
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo guardar la promoción.')
  }
}

async function desactivar(item: Promocion) {
  if (!isAdmin.value || !item.activo) return
  if (!confirm(`¿Dar de baja "${item.nombre}"?`)) return
  error.value = null
  success.value = null
  try {
    await promocionService.remove(item.id)
    success.value = `"${item.nombre}" quedó inactiva.`
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo dar de baja la promoción.')
  }
}

onMounted(load)
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Promociones</h1>
        <p v-if="isAdmin">Avisos con vigencia. No modifican el total del pedido (RN-13).</p>
        <p v-else>Promociones vigentes hoy.</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button v-if="isAdmin" label="Nueva promoción" icon="pi pi-plus" @click="openCreate" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{
      success
    }}</Message>

    <section v-if="isAdmin" class="filters">
      <div class="filter-field">
        <label for="q">Buscar</label>
        <InputText id="q" v-model="filters.q" placeholder="Nombre o descripción" class="w-full" />
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
      <div class="filter-field">
        <label for="vigente">Vigencia</label>
        <Select
          id="vigente"
          v-model="filters.vigente"
          :options="vigenteOptions"
          option-label="label"
          option-value="value"
          class="w-full"
        />
      </div>
      <div class="filter-actions">
        <Button label="Filtrar" icon="pi pi-search" :loading="loading" @click="load" />
      </div>
    </section>

    <DataTable
      v-if="isAdmin"
      :value="items"
      :loading="loading"
      striped-rows
      class="menu-table"
      data-key="id"
    >
      <Column field="nombre" header="Nombre" />
      <Column field="descripcion" header="Descripción" />
      <Column header="Desde">
        <template #body="{ data }">{{ formatFecha(data.vigenteDesde) }}</template>
      </Column>
      <Column header="Hasta">
        <template #body="{ data }">{{ formatFecha(data.vigenteHasta) }}</template>
      </Column>
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.activo ? 'Activo' : 'Inactivo'" :severity="data.activo ? 'success' : 'danger'" />
        </template>
      </Column>
      <Column header="Acciones" style="width: 8rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button icon="pi pi-pencil" text rounded aria-label="Editar" @click="openEdit(data)" />
            <Button
              icon="pi pi-trash"
              text
              rounded
              severity="danger"
              aria-label="Dar de baja"
              :disabled="!data.activo"
              @click="desactivar(data)"
            />
          </div>
        </template>
      </Column>
      <template #empty>No hay promociones para mostrar.</template>
    </DataTable>

    <section v-else>
      <p v-if="loading">Cargando promociones…</p>
      <p v-else-if="items.length === 0" class="hint">No hay promociones vigentes.</p>
      <ul v-else class="promo-list">
        <li v-for="item in items" :key="item.id" class="nav-card">
          <span class="nav-title">{{ item.nombre }}</span>
          <p class="promo-desc">{{ item.descripcion }}</p>
          <span class="nav-desc">
            Desde {{ formatFecha(item.vigenteDesde) }} hasta {{ formatFecha(item.vigenteHasta) }}
          </span>
        </li>
      </ul>
    </section>

    <Dialog
      v-model:visible="dialogVisible"
      :header="dialogTitle"
      modal
      :style="{ width: 'min(520px, 95vw)' }"
    >
      <form class="product-form" @submit.prevent="save">
        <label for="nombre">Nombre</label>
        <InputText id="nombre" v-model="form.nombre" class="w-full" maxlength="100" required />

        <label for="descripcion">Descripción</label>
        <Textarea id="descripcion" v-model="form.descripcion" rows="3" class="w-full" maxlength="500" required />

        <label for="desde">Vigente desde</label>
        <InputText id="desde" v-model="form.vigenteDesde" type="date" class="w-full" required />

        <label for="hasta">Vigente hasta</label>
        <InputText id="hasta" v-model="form.vigenteHasta" type="date" class="w-full" required />

        <div v-if="editingId" class="check-row">
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

<style scoped>
.promo-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 0.75rem;
}

.promo-desc {
  margin: 0.35rem 0;
}
</style>
