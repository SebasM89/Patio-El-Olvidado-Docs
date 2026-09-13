import api from './api'
import type { CajaRangoReporte, NominaReporte, VentasReporte } from '../types/reporte'

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.click()
  URL.revokeObjectURL(url)
}

function rangoParams(desde: string, hasta: string) {
  return { desde, hasta }
}

export const reportesService = {
  ventas(desde: string, hasta: string) {
    return api.get<VentasReporte>('/api/reportes/ventas', { params: rangoParams(desde, hasta) })
  },
  caja(desde: string, hasta: string) {
    return api.get<CajaRangoReporte>('/api/reportes/caja', { params: rangoParams(desde, hasta) })
  },
  nomina(desde: string, hasta: string) {
    return api.get<NominaReporte>('/api/reportes/nomina', { params: rangoParams(desde, hasta) })
  },
  async exportVentasPdf(desde: string, hasta: string) {
    const { data } = await api.get<Blob>('/api/reportes/ventas/export/pdf', {
      params: rangoParams(desde, hasta),
      responseType: 'blob',
    })
    downloadBlob(data, `ventas-${desde}_${hasta}.pdf`)
  },
  async exportVentasCsv(desde: string, hasta: string) {
    const { data } = await api.get<Blob>('/api/reportes/ventas/export/csv', {
      params: rangoParams(desde, hasta),
      responseType: 'blob',
    })
    downloadBlob(data, `ventas-${desde}_${hasta}.csv`)
  },
  async exportCajaPdf(desde: string, hasta: string) {
    const { data } = await api.get<Blob>('/api/reportes/caja/export/pdf', {
      params: rangoParams(desde, hasta),
      responseType: 'blob',
    })
    downloadBlob(data, `caja-rango-${desde}_${hasta}.pdf`)
  },
  async exportCajaCsv(desde: string, hasta: string) {
    const { data } = await api.get<Blob>('/api/reportes/caja/export/csv', {
      params: rangoParams(desde, hasta),
      responseType: 'blob',
    })
    downloadBlob(data, `caja-rango-${desde}_${hasta}.csv`)
  },
  async exportNominaPdf(desde: string, hasta: string) {
    const { data } = await api.get<Blob>('/api/reportes/nomina/export/pdf', {
      params: rangoParams(desde, hasta),
      responseType: 'blob',
    })
    downloadBlob(data, `nomina-${desde}_${hasta}.pdf`)
  },
  async exportNominaCsv(desde: string, hasta: string) {
    const { data } = await api.get<Blob>('/api/reportes/nomina/export/csv', {
      params: rangoParams(desde, hasta),
      responseType: 'blob',
    })
    downloadBlob(data, `nomina-${desde}_${hasta}.csv`)
  },
}
