import api from './api'
import type { CajaDia } from '../types/pago'

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.click()
  URL.revokeObjectURL(url)
}

export const cajaService = {
  hoy() {
    return api.get<CajaDia>('/api/caja/hoy')
  },
  byFecha(fecha?: string) {
    return api.get<CajaDia>('/api/caja', { params: fecha ? { fecha } : undefined })
  },
  async exportPdf(fecha?: string) {
    const { data } = await api.get<Blob>('/api/caja/export/pdf', {
      params: fecha ? { fecha } : undefined,
      responseType: 'blob',
    })
    const name = fecha ? `caja-${fecha}.pdf` : 'caja.pdf'
    downloadBlob(data, name)
  },
  async exportCsv(fecha?: string) {
    const { data } = await api.get<Blob>('/api/caja/export/csv', {
      params: fecha ? { fecha } : undefined,
      responseType: 'blob',
    })
    const name = fecha ? `caja-${fecha}.csv` : 'caja.csv'
    downloadBlob(data, name)
  },
}
