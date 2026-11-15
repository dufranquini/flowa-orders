export type Side = 'Buy' | 'Sell'
export type OrderStatus = 'New' | 'Rejected'

export interface NewOrderRequest {
    symbol: string
    side: Side
    quantity: number
    price: number
}

export interface OrderResponse {
    clOrdId: string
    orderId: string
    execId: string
    status: OrderStatus
    symbol: string
    side: Side
    quantity: number
    price: number
    text: string | null
    rejectReason: number | null
    receivedAt: string
}

export interface ProblemDetails {
    title?: string
    detail?: string
    status?: number
    errors?: Record<string, string[]>
}


export class ApiError extends Error {
    readonly problem: ProblemDetails

    constructor(problem: ProblemDetails) {
        super(problem.detail ?? problem.title ?? 'Erro inesperado')
        this.problem = problem
    }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await fetch(path, {
        ...init,
        headers: { 'Content-Type': 'application/json', ...init?.headers },
    })

    if (!response.ok) {
        let problem: ProblemDetails = { status: response.status, title: response.statusText }
        
        try {
            problem = { ...problem, ...(await response.json()) }
        } catch {
            // TODO: Log error
            }
            throw new ApiError(problem)
        }
        return (await response.json()) as T
}


export const api = {
    status: () => request<{ fixConnected: boolean }>('/api/status'),
    listOrders: () => request<OrderResponse[]>('/api/orders'),
    submitOrder: (order: NewOrderRequest) =>
        request<OrderResponse>('/api/orders', { method: 'POST', body: JSON.stringify(order) }), 
}