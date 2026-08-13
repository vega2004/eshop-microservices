import axios from 'axios'
import { TICKETS_API_URL } from '../../../config/apiConfig.js'

const ticketsApi = axios.create({
  baseURL: TICKETS_API_URL,
})

export async function getOrderTicket(orderId, accessToken, signal) {
  const response = await ticketsApi.get(`/api/tickets/orders/${orderId}`, {
    responseType: 'blob',
    signal,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  return response.data
}

export function openTicketPdf(ticketBlob, orderNumber = 'ticket') {
  const url = URL.createObjectURL(new Blob([ticketBlob], { type: 'application/pdf' }))
  const openedWindow = window.open(url, '_blank', 'noopener,noreferrer')

  if (!openedWindow) {
    const link = document.createElement('a')
    link.href = url
    link.download = `ticket-${orderNumber}.pdf`
    document.body.appendChild(link)
    link.click()
    link.remove()
  }

  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
}
