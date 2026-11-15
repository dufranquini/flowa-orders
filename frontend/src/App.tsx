import { useEffect, useState, type FormEvent } from 'react'
import { api, ApiError, type OrderResponse, type Side } from './api'

const SYMBOLS = ['PETR4', 'VALE3', 'VIIA4']
const SIDE_LABEL: Record<Side, string> = { Buy: 'Compra', Sell: 'Venda' }
const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export default function App() {
  const [connected, setConnected] = useState<boolean | null>(null)

  const [symbol, setSymbol] = useState('PETR4')
  const [side, setSide] = useState<Side>('Buy')
  const [quantity, setQuantity] = useState('')
  const [price, setPrice] = useState('')

  const [submitting, setSubmitting] = useState(false)
  const [result, setResult] = useState<OrderResponse | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.status()
      .then((data) => setConnected(data.fixConnected))
      .catch(() => setConnected(false))
  }, [])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setResult(null)
    setError(null)

    try {
      const response = await api.submitOrder({
        symbol,
        side,
        quantity: Number(quantity),
        price: Number(price),
      })
      setResult(response)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Falha de comunicação com a API.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="page">
      <header className="topbar">
        <div>
          <h1>OrderGenerator</h1>
          <p className="muted">Envio de ordens via FIX 4.4</p>
        </div>
        <span className={`badge ${connected ? 'on' : 'off'}`}>
          {connected === null ? 'Verificando…' : connected ? 'FIX conectado' : 'FIX desconectado'}
        </span>
      </header>

      <main className="grid">
        <section className="card">
          <h2>Nova ordem</h2>

          <form onSubmit={handleSubmit}>
            <label className="field">
              <span className="label">Símbolo</span>
              <select value={symbol} onChange={(e) => setSymbol(e.target.value)}>
                {SYMBOLS.map((s) => (
                  <option key={s} value={s}>{s}</option>
                ))}
              </select>
            </label>

            <div className="field">
              <span className="label">Lado</span>
              <div className="segmented">
                {(['Buy', 'Sell'] as const).map((s) => (
                  <button
                    key={s}
                    type="button"
                    className={`segment ${s.toLowerCase()} ${side === s ? 'active' : ''}`}
                    onClick={() => setSide(s)}
                  >
                    {SIDE_LABEL[s]}
                  </button>
                ))}
              </div>
            </div>

            <div className="row">
              <label className="field">
                <span className="label">Quantidade</span>
                <input
                  type="number" min={1} max={99999} step={1} required placeholder="100"
                  value={quantity}
                  onChange={(e) => setQuantity(e.target.value)}
                />
                <span className="hint">1 a 99.999</span>
              </label>

              <label className="field">
                <span className="label">Preço (R$)</span>
                <input
                  type="number" min={0.01} max={999.99} step={0.01} required placeholder="35.42"
                  value={price}
                  onChange={(e) => setPrice(e.target.value)}
                />
                <span className="hint">0,01 a 999,99</span>
              </label>
            </div>

            <button className="submit" type="submit" disabled={submitting}>
              {submitting ? 'Enviando…' : `Enviar ordem de ${SIDE_LABEL[side]}`}
            </button>
          </form>
        </section>

        <section className="card">
          <h2>Resposta</h2>

          {!result && !error && (
            <p className="muted">Envie uma ordem para ver o ExecutionReport retornado.</p>
          )}

          {result && (
            <div className={`result ${result.status === 'New' ? 'ok' : 'rejected'}`}>
              <span className={`pill ${result.status === 'New' ? 'ok' : 'rejected'}`}>
                {result.status === 'New' ? 'Aceita (Nova)' : 'Rejeitada'}
              </span>
              {result.text && <p>{result.text}</p>}
              <dl>
                <dt>Ordem</dt>
                <dd>
                  {SIDE_LABEL[result.side]} {result.quantity} {result.symbol} a {brl.format(result.price)}
                </dd>
                <dt>Financeiro</dt>
                <dd>{brl.format(result.price * result.quantity)}</dd>
                <dt>OrderID</dt>
                <dd className="mono">{result.orderId}</dd>
                <dt>ClOrdID</dt>
                <dd className="mono">{result.clOrdId}</dd>
              </dl>
            </div>
          )}

          {error && (
            <div className="result rejected">
              <span className="pill rejected">Erro</span>
              <p>{error}</p>
            </div>
          )}
        </section>
      </main>
    </div>
  )
}