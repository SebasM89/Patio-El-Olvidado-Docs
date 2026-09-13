export interface VentasDia {
  fecha: string
  totalEfectivo: number
  totalTarjeta: number
  totalTransferencia: number
  total: number
  cantidadPagos: number
}

export interface VentasReporte {
  desde: string
  hasta: string
  totalEfectivo: number
  totalTarjeta: number
  totalTransferencia: number
  total: number
  cantidadPagos: number
  cantidadPedidos: number
  dias: VentasDia[]
}

export interface CajaRangoReporte {
  desde: string
  hasta: string
  totalEfectivo: number
  totalTarjeta: number
  totalTransferencia: number
  total: number
  dias: {
    fecha: string
    totalEfectivo: number
    totalTarjeta: number
    totalTransferencia: number
    total: number
  }[]
}

export interface NominaLinea {
  id: number
  empleadoId: number
  empleadoNombre: string
  periodoDesde: string
  periodoHasta: string
  horas: number
  tarifaHoraSnapshot: number
  monto: number
  generadaEnUtc: string
}

export interface NominaReporte {
  desde: string
  hasta: string
  totalHoras: number
  totalMonto: number
  lineas: NominaLinea[]
}

export type ReporteTab = 'ventas' | 'caja' | 'nomina'
