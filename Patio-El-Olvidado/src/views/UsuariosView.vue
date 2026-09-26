<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { usuarioService } from '../services/usuarioService'
import type { RolCatalogo, Usuario, UsuarioPayload } from '../types/usuario'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Dialog from 'primevue/dialog'
import Message from 'primevue/message'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Select from 'primevue/select'

const auth = useAuthStore()
const isAdmin = computed(() => auth.rol === 'Admin')

const usuarios = ref<Usuario[]>([])
const roles = ref<RolCatalogo[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const filters = reactive({
  q: '',
  rol: '',
  estado: '',
})

const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = reactive({
  nombre: '',
  email: '',
  password: '',
  rolId: null as number | null,
})

const estadoOptions = [
  { label: 'Todos', value: '' },
  { label: 'Activo', value: 'Activo' },
  { label: 'Inactivo', value: 'Inactivo' },
  { label: 'Bloqueado', value: 'Bloqueado' },
]

const rolFilterOptions = computed(() => [
  { label: 'Todos', value: '' },
  ...roles.value.map((r) => ({ label: r.nombre, value: r.nombre })),
])

const dialogTitle = computed(() => (editingId.value ? 'Editar usuario' : 'Nuevo usuario'))

function estadoSeverity(estado: string) {
  if (estado === 'Activo') return 'success'
  if (estado === 'Inactivo') return 'danger'
  return 'warn'
}

function formatAcceso(value: string | null) {
  if (!value) return '—'
  return new Date(value).toLocaleString('es-AR')
}

function apiMessage(e: unknown, fallback: string) {
  const data = (e as { response?: { data?: { message?: string; errors?: { errorMessage?: string }[] } } })
    ?.response?.data
  return data?.errors?.[0]?.errorMessage ?? data?.message ?? fallback
}

async function loadRoles() {
  const { data } = await usuarioService.listRoles()
  roles.value = data
}

async function load() {
  if (!isAdmin.value) return
  loading.value = true
  error.value = null
  try {
    const { data } = await usuarioService.list({
      q: filters.q.trim() || undefined,
      rol: filters.rol || undefined,
      estado: filters.estado || undefined,
    })
    usuarios.value = data
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudieron cargar los usuarios.')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  form.nombre = ''
  form.email = ''
  form.password = ''
  form.rolId = roles.value[0]?.id ?? null
  dialogVisible.value = true
}

function openEdit(u: Usuario) {
  editingId.value = u.id
  form.nombre = u.nombre
  form.email = u.email
  form.password = ''
  form.rolId = u.rolId
  dialogVisible.value = true
}

async function save() {
  if (!isAdmin.value || form.rolId == null) return
  error.value = null
  success.value = null
  const payload: UsuarioPayload = {
    nombre: form.nombre.trim(),
    email: form.email.trim(),
    rolId: form.rolId,
  }
  if (!editingId.value || form.password.trim()) {
    payload.password = form.password
  }
  try {
    if (editingId.value) {
      await usuarioService.update(editingId.value, payload)
      success.value = 'Usuario actualizado.'
    } else {
      await usuarioService.create(payload)
      success.value = 'Usuario creado.'
    }
    dialogVisible.value = false
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo guardar el usuario.')
  }
}

async function cambiarEstado(u: Usuario, estado: 'Activo' | 'Inactivo') {
  if (!isAdmin.value) return
  error.value = null
  success.value = null
  try {
    await usuarioService.cambiarEstado(u.id, { estado })
    success.value = estado === 'Inactivo' ? `Usuario "${u.nombre}" desactivado.` : `Usuario "${u.nombre}" activado.`
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo cambiar el estado.')
  }
}

onMounted(async () => {
  if (!isAdmin.value) return
  try {
    await loadRoles()
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudieron cargar los usuarios.')
  }
})
</script>

<template>
  <div class="menu-page">
    <header class="menu-header">
      <div>
        <h1>Usuarios</h1>
        <p>Alta, edición y estado de cuentas (solo administrador)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button v-if="isAdmin" label="Nuevo usuario" icon="pi pi-plus" @click="openCreate" />
      </div>
    </header>

    <Message v-if="error" severity="error" :closable="true" @close="error = null">{{ error }}</Message>
    <Message v-if="success" severity="success" :closable="true" @close="success = null">{{ success }}</Message>

    <section v-if="isAdmin" class="filters">
      <div class="filter-field">
        <label for="q">Buscar</label>
        <InputText id="q" v-model="filters.q" placeholder="Nombre o email" class="w-full" />
      </div>
      <div class="filter-field">
        <label for="rol">Rol</label>
        <Select
          id="rol"
          v-model="filters.rol"
          :options="rolFilterOptions"
          option-label="label"
          option-value="value"
          class="w-full"
        />
      </div>
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
      <div class="filter-actions">
        <Button label="Filtrar" icon="pi pi-search" :loading="loading" @click="load" />
      </div>
    </section>

    <DataTable
      v-if="isAdmin"
      :value="usuarios"
      :loading="loading"
      striped-rows
      class="menu-table"
      data-key="id"
    >
      <Column field="nombre" header="Nombre" />
      <Column field="email" header="Email" />
      <Column field="rolNombre" header="Rol" />
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.estado" :severity="estadoSeverity(data.estado)" />
        </template>
      </Column>
      <Column header="Último acceso">
        <template #body="{ data }">{{ formatAcceso(data.ultimoAcceso) }}</template>
      </Column>
      <Column header="Acciones" style="width: 14rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button icon="pi pi-pencil" text rounded aria-label="Editar" @click="openEdit(data)" />
            <Button
              v-if="data.estado === 'Activo'"
              icon="pi pi-ban"
              text
              rounded
              severity="danger"
              aria-label="Desactivar"
              :disabled="data.id === auth.usuario?.id"
              @click="cambiarEstado(data, 'Inactivo')"
            />
            <Button
              v-else
              icon="pi pi-check"
              text
              rounded
              severity="success"
              aria-label="Activar"
              @click="cambiarEstado(data, 'Activo')"
            />
          </div>
        </template>
      </Column>
      <template #empty>No hay usuarios para mostrar.</template>
    </DataTable>

    <Message v-else severity="warn">Solo el administrador gestiona usuarios.</Message>

    <Dialog
      v-model:visible="dialogVisible"
      :header="dialogTitle"
      modal
      :style="{ width: 'min(480px, 95vw)' }"
    >
      <form class="product-form" @submit.prevent="save">
        <label for="nombre">Nombre</label>
        <InputText id="nombre" v-model="form.nombre" class="w-full" required maxlength="100" />

        <label for="email">Email</label>
        <InputText id="email" v-model="form.email" type="email" class="w-full" required maxlength="150" />

        <label for="password">{{ editingId ? 'Contraseña (opcional)' : 'Contraseña' }}</label>
        <InputText
          id="password"
          v-model="form.password"
          type="password"
          class="w-full"
          :required="!editingId"
          minlength="8"
          autocomplete="new-password"
        />
        <p class="hint">
          {{
            editingId
              ? 'Dejala vacía para conservar la actual. Mínimo 8 caracteres si la cambiás.'
              : 'Obligatoria al crear. Mínimo 8 caracteres.'
          }}
        </p>

        <label for="rolId">Rol</label>
        <Select
          id="rolId"
          v-model="form.rolId"
          :options="roles"
          option-label="nombre"
          option-value="id"
          placeholder="Elegí un rol"
          class="w-full"
        />

        <div class="form-actions">
          <Button type="button" label="Cancelar" severity="secondary" text @click="dialogVisible = false" />
          <Button type="submit" label="Guardar" :disabled="form.rolId == null" />
        </div>
      </form>
    </Dialog>
  </div>
</template>
