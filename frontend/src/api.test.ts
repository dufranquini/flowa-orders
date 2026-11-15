import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, ApiError } from './api'

function jsonResponse(body: unknown, status = 200, statusText = 'OK'): Response {
    return new Response(JSON.stringify(body), {
        status,
        statusText,
        headers: { 'Content-Type': 'application/json' },
    })
}

function mockFetch(response: Response) {
    const fetchMock = vi.fn().mockResolvedValue(response)
    vi.stubGlobal('fetch', fetchMock)
    return fetchMock
}

afterEach(() => {
    vi.unstubAllGlobals()
})

describe('api.status', () => {
    it('call GET /api/status and return the connection status', async () => {
        const fetchMock = mockFetch(jsonResponse({ fixConnected: true }))

        const result = await api.status()

        expect(result).toEqual({ fixConnected: true })
        expect(fetchMock).toHaveBeenCalledWith('/api/status', expect.anything())
    })
})

describe('api.listOrders', () => {
    it('call GET /api/orders and return the list', async () => {
        const orders = [{ clOrdId: 'A', status: 'New' }]
        const fetchMock = mockFetch(jsonResponse(orders))

        const result = await api.listOrders()

        expect(result).toEqual(orders)
        expect(fetchMock).toHaveBeenCalledWith('/api/orders', expect.anything())
    })
})

describe('api.submitOrder', () => {
    it('call POST /api/orders with the order in JSON', async () => {
        const order = { symbol: 'PETR4', side: 'Buy' as const, quantity: 100, price: 35.42 }
        const fetchMock = mockFetch(jsonResponse({ status: 'New' }))

        await api.submitOrder(order)

        const [url, init] = fetchMock.mock.calls[0]
        expect(url).toBe('/api/orders')
        expect(init.method).toBe('POST')
        expect(JSON.parse(init.body)).toEqual(order)
        expect(init.headers['Content-Type']).toBe('application/json')
    })

    it('return the API response when the order is accepted', async () => {
        mockFetch(jsonResponse({ clOrdId: 'X', status: 'New', text: 'Order accepted.' }))

        const result = await api.submitOrder({ symbol: 'PETR4', side: 'Buy', quantity: 1, price: 1 })

        expect(result.status).toBe('New')
        expect(result.text).toBe('Order accepted.')
    })
})

describe('error handling', () => {
    it('throws ApiError with the message from ProblemDetails', async () => {
        mockFetch(jsonResponse({ title: 'Error sending order', detail: 'Not connected to FIX session' }, 500))

        const error = await api.submitOrder({ symbol: 'PETR4', side: 'Buy', quantity: 1, price: 1 }).catch((e) => e)

        expect(error).toBeInstanceOf(ApiError)
        expect(error.message).toBe('Not connected to FIX session')
        expect(error.problem.status).toBe(500)
    })
})