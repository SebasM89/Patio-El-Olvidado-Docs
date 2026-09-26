<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import { inventarioService } from '../services/inventarioService'
import { proveedorService } from '../services/proveedorService'
import type { Proveedor } from '../types/proveedor'
import type {
  CreateStockItemPayload,
  MovimientoStock,
  RegistrarMovimientoPayload,
  StockItem,
  TipoMovimientoStock,
  UnidadStock,
  UpdateStockItemPayload,
} from '../types/inventario'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
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

const items = ref<StockItem[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const success = ref<string | null>(null)

const filters = reactive({
  q: '',
  activo: 'todos' as 'todos' | 'activos' | 'inactivos',
  enAlerta: false,
})

const activoOptions = [
  { label: 'Todos', value: 'todos' },
  { label: 'Activos', value: 'activos' },
  { label: 'Inactivos', value: 'inactivos' },
]
const unidadOptions: UnidadStock[] = ['Unidad', 'Kg', 'L']
const tipoOptions: TipoMovimientoStock[] = ['Entrada', 'Salida']

const dialogVisible = ref(false)
const editingId = ref<number | null>(null)
const form = reactive({
  nombre: '',
  descripcion: '',
  unidad: 'Unidad' as UnidadStock,
  stockMinimo: 0,
  cantidadInicial: 0,
  activo: true,
})

const movVisible = ref(false)
const movItem = ref<StockItem | null>(null)
const movForm = reactive({
  tipo: 'Entrada' as TipoMovimientoStock,
  cantidad: null as number | null,
  motivo: '',
  proveedorId: null as number | null,
})
const proveedoresActivos = ref<Proveedor[]>([])

const historialVisible = ref(false)
const historialItem = ref<StockItem | null>(null)
const historial = ref<MovimientoStock[]>([])
const historialLoading = ref(false)

const dialogTitle = computed(() => (editingId.value ? 'Editar ítem' : 'Nuevo ítem'))

function qty(n: number) {
  return Number(n).toLocaleString('es-AR', { maximumFractionDigits: 3 })
}

function apiMessage(e: unknown, fallback: string) {
  return (e as { response?: { data?: { message?: string } } })?.response?.data?.message ?? fallback
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const { data } = await inventarioService.list({
      q: filters.q || undefined,
      activo: filters.activo === 'todos' ? undefined : filters.activo === 'activos',
      enAlerta: filters.enAlerta ? true : undefined,
    })
    items.value = data
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo cargar el inventario.')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editingId.value = null
  Object.assign(form, {
    nombre: '',
    descripcion: '',
    unidad: 'Unidad',
    stockMinimo: 0,
    cantidadInicial: 0,
    activo: true,
  })
  dialogVisible.value = true
}

function openEdit(item: StockItem) {
  editingId.value = item.id
  Object.assign(form, {
    nombre: item.nombre,
    descripcion: item.descripcion ?? '',
    unidad: item.unidad,
    stockMinimo: item.stockMinimo,
    cantidadInicial: 0,
    activo: item.activo,
  })
  dialogVisible.value = true
}

async function save() {
  if (!isAdmin.value) return
  error.value = null
  success.value = null
  try {
    if (editingId.value) {
      const payload: UpdateStockItemPayload = {
        nombre: form.nombre.trim(),
        descripcion: form.descripcion.trim() || null,
        stockMinimo: Number(form.stockMinimo),
        activo: form.activo,
      }
      await inventarioService.update(editingId.value, payload)
      success.value = 'Ítem actualizado.'
    } else {
      const payload: CreateStockItemPayload = {
        nombre: form.nombre.trim(),
        descripcion: form.descripcion.trim() || null,
        unidad: form.unidad,
        stockMinimo: Number(form.stockMinimo),
        cantidadInicial: Number(form.cantidadInicial) || 0,
        activo: form.activo,
      }
      await inventarioService.create(payload)
      success.value = 'Ítem creado.'
    }
    dialogVisible.value = false
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo guardar el ítem.')
  }
}

async function desactivar(item: StockItem) {
  if (!isAdmin.value || !item.activo) return
  error.value = null
  success.value = null
  try {
    await inventarioService.update(item.id, {
      nombre: item.nombre,
      descripcion: item.descripcion ?? null,
      stockMinimo: item.stockMinimo,
      activo: false,
    })
    success.value = `"${item.nombre}" quedó inactivo.`
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo desactivar el ítem.')
  }
}

async function openMovimiento(item: StockItem) {
  movItem.value = item
  Object.assign(movForm, { tipo: 'Entrada', cantidad: null, motivo: '', proveedorId: null })
  movVisible.value = true
  try {
    const { data } = await proveedorService.list({ activo: true })
    proveedoresActivos.value = data
  } catch {
    proveedoresActivos.value = []
  }
}

async function registrarMovimiento() {
  if (!isAdmin.value || !movItem.value || movForm.cantidad == null) return
  error.value = null
  success.value = null
  const payload: RegistrarMovimientoPayload = {
    tipo: movForm.tipo,
    cantidad: Number(movForm.cantidad),
    motivo: movForm.motivo.trim() || null,
    proveedorId: movForm.tipo === 'Entrada' ? movForm.proveedorId : null,
  }
  try {
    await inventarioService.registrarMovimiento(movItem.value.id, payload)
    success.value = 'Movimiento registrado.'
    movVisible.value = false
    await load()
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo registrar el movimiento.')
  }
}

async function openHistorial(item: StockItem) {
  historialItem.value = item
  historialVisible.value = true
  historialLoading.value = true
  historial.value = []
  try {
    const { data } = await inventarioService.movimientos(item.id)
    historial.value = data
  } catch (e: unknown) {
    error.value = apiMessage(e, 'No se pudo cargar el historial.')
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
        <h1>Inventario</h1>
        <p>Stock, movimientos y alertas (RF-08 / RN-09 / RN-10)</p>
      </div>
      <div class="header-actions">
        <RouterLink to="/dashboard" class="back-link">← Dashboard</RouterLink>
        <Button v-if="isAdmin" label="Nuevo ítem" icon="pi pi-plus" @click="openCreate" />
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
      <div class="filter-field check">
        <label for="enAlerta">Solo en alerta</label>
        <Checkbox id="enAlerta" v-model="filters.enAlerta" binary />
      </div>
      <div class="filter-actions">
        <Button label="Filtrar" icon="pi pi-search" :loading="loading" @click="load" />
      </div>
    </section>

    <DataTable :value="items" :loading="loading" striped-rows class="menu-table" data-key="id">
      <Column field="nombre" header="Nombre" />
      <Column field="unidad" header="Unidad" style="width: 7rem" />
      <Column header="Saldo">
        <template #body="{ data }">{{ qty(data.cantidadActual) }}</template>
      </Column>
      <Column header="Mínimo">
        <template #body="{ data }">{{ qty(data.stockMinimo) }}</template>
      </Column>
      <Column header="Estado">
        <template #body="{ data }">
          <Tag :value="data.activo ? 'Activo' : 'Inactivo'" :severity="data.activo ? 'success' : 'danger'" />
        </template>
      </Column>
      <Column header="Alerta">
        <template #body="{ data }">
          <Tag v-if="data.enAlerta" value="En alerta" severity="warn" />
          <span v-else>—</span>
        </template>
      </Column>
      <Column header="Acciones" style="width: 14rem">
        <template #body="{ data }">
          <div class="row-actions">
            <Button
              icon="pi pi-list"
              text
              rounded
              aria-label="Movimientos"
              @click="openHistorial(data)"
            />
            <Button
              v-if="isAdmin"
              icon="pi pi-pencil"
              text
              rounded
              aria-label="Editar"
              @click="openEdit(data)"
            />
            <Button
              v-if="isAdmin"
              icon="pi pi-arrow-right-arrow-left"
              text
              rounded
              aria-label="Registrar movimiento"
              :disabled="!data.activo"
              @click="openMovimiento(data)"
            />
            <Button
              v-if="isAdmin"
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
      <template #empty>No hay ítems para mostrar.</template>
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

        <label for="descripcion">Descripción</label>
        <Textarea id="descripcion" v-model="form.descripcion" rows="2" class="w-full" auto-resize />

        <template v-if="!editingId">
          <label for="unidad">Unidad</label>
          <Select
            id="unidad"
            v-model="form.unidad"
            :options="unidadOptions"
            class="w-full"
          />

          <label for="cantidadInicial">Cantidad inicial</label>
          <InputNumber
            id="cantidadInicial"
            v-model="form.cantidadInicial"
            :min="0"
            :max-fraction-digits="3"
            :min-fraction-digits="0"
            class="w-full"
          />
        </template>
        <p v-else class="hint">La unidad y el saldo no se editan. El saldo cambia con un movimiento.</p>

        <label for="stockMinimo">Stock mínimo</label>
        <InputNumber
          id="stockMinimo"
          v-model="form.stockMinimo"
          :min="0"
          :max-fraction-digits="3"
          :min-fraction-digits="0"
          class="w-full"
        />

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
      v-model:visible="movVisible"
      :header="movItem ? `Movimiento — ${movItem.nombre}` : 'Movimiento'"
      modal
      :style="{ width: 'min(420px, 95vw)' }"
    >
      <form class="product-form" @submit.prevent="registrarMovimiento">
        <p v-if="movItem">Saldo actual: {{ qty(movItem.cantidadActual) }} {{ movItem.unidad }}</p>
        <label for="tipo">Tipo</label>
        <Select id="tipo" v-model="movForm.tipo" :options="tipoOptions" class="w-full" />

        <label for="cantidad">Cantidad</label>
        <InputNumber
          id="cantidad"
          v-model="movForm.cantidad"
          :min="0.001"
          :max-fraction-digits="3"
          :min-fraction-digits="0"
          class="w-full"
          required
        />

        <template v-if="movForm.tipo === 'Entrada'">
          <label for="proveedor">Proveedor (opcional)</label>
          <Select
            id="proveedor"
            v-model="movForm.proveedorId"
            :options="proveedoresActivos"
            option-label="nombre"
            option-value="id"
            placeholder="Sin proveedor"
            show-clear
            class="w-full"
          />
        </template>

        <label for="motivo">Motivo</label>
        <InputText id="motivo" v-model="movForm.motivo" class="w-full" />

        <div class="form-actions">
          <Button type="button" label="Cancelar" severity="secondary" text @click="movVisible = false" />
          <Button type="submit" label="Registrar" :disabled="movForm.cantidad == null" />
        </div>
      </form>
    </Dialog>

    <Dialog
      v-model:visible="historialVisible"
      :header="historialItem ? `Movimientos — ${historialItem.nombre}` : 'Movimientos'"
      modal
      :style="{ width: 'min(720px, 95vw)' }"
    >
      <DataTable :value="historial" :loading="historialLoading" striped-rows data-key="id">
        <Column header="Fecha">
          <template #body="{ data }">{{ new Date(data.fechaUtc).toLocaleString('es-AR') }}</template>
        </Column>
        <Column field="tipo" header="Tipo" />
        <Column header="Cantidad">
          <template #body="{ data }">{{ qty(data.cantidad) }}</template>
        </Column>
        <Column header="Motivo">
          <template #body="{ data }">{{ data.motivo || '—' }}</template>
        </Column>
        <Column header="Proveedor">
          <template #body="{ data }">{{ data.proveedorNombre || '—' }}</template>
        </Column>
        <Column header="Registró">
          <template #body="{ data }">{{ data.registradoPorNombre || '—' }}</template>
        </Column>
        <template #empty>Sin movimientos.</template>
      </DataTable>
    </Dialog>
  </div>
</template>
