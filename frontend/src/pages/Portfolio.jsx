import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { portfolioApi } from '../api/market';
import { errorMessage } from '../api/client';
import { Spinner, ErrorBanner, Empty } from '../components/Ui';
import Disclaimer from '../components/Disclaimer';
import { usd, pct, changeClass } from '../utils/format';

export default function Portfolio() {
  const [portfolio, setPortfolio] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [symbol, setSymbol] = useState('');
  const [qty, setQty] = useState('');
  const [side, setSide] = useState('buy');
  const [trading, setTrading] = useState(false);

  const load = async () => {
    try {
      setPortfolio(await portfolioApi.get());
    } catch (e) {
      setError(errorMessage(e, 'Could not load your portfolio.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const trade = async (e) => {
    e.preventDefault();
    const sym = symbol.trim().toUpperCase();
    const quantity = Number(qty);
    if (!sym || !quantity || quantity <= 0) {
      setError('Enter a symbol and a positive quantity.');
      return;
    }
    setTrading(true);
    setError('');
    try {
      const result = side === 'buy'
        ? await portfolioApi.buy(sym, quantity)
        : await portfolioApi.sell(sym, quantity);
      setPortfolio(result);
      setSymbol('');
      setQty('');
    } catch (e) {
      setError(errorMessage(e, 'Trade failed.'));
    } finally {
      setTrading(false);
    }
  };

  if (loading) return <div className="page"><Spinner /></div>;

  return (
    <div className="page">
      <div className="section-head">
        <h2>Virtual Portfolio</h2>
        <span className="muted">Play money — no real trades</span>
      </div>

      <Disclaimer compact />
      <ErrorBanner message={error} />

      {portfolio && (
        <>
          <div className="stat-row">
            <Stat label="Total value" value={usd(portfolio.totalValue)} />
            <Stat label="Cash" value={usd(portfolio.cashBalance)} />
            <Stat label="Holdings" value={usd(portfolio.holdingsValue)} />
            <Stat
              label="Total return"
              value={`${usd(portfolio.totalReturn)} (${pct(portfolio.totalReturnPercent)})`}
              tone={changeClass(portfolio.totalReturn)}
            />
          </div>

          <form className="trade-form" onSubmit={trade}>
            <select value={side} onChange={(e) => setSide(e.target.value)}>
              <option value="buy">Buy</option>
              <option value="sell">Sell</option>
            </select>
            <input
              placeholder="Symbol"
              value={symbol}
              onChange={(e) => setSymbol(e.target.value)}
              maxLength={12}
            />
            <input
              type="number"
              min="1"
              step="1"
              placeholder="Qty"
              value={qty}
              onChange={(e) => setQty(e.target.value)}
            />
            <button className="btn primary" type="submit" disabled={trading}>
              {trading ? 'Placing…' : side === 'buy' ? 'Buy' : 'Sell'}
            </button>
          </form>

          {portfolio.holdings.length === 0 ? (
            <Empty>No holdings yet. Buy a stock above to start your virtual portfolio.</Empty>
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  <th>Symbol</th>
                  <th className="num">Qty</th>
                  <th className="num">Avg cost</th>
                  <th className="num">Price</th>
                  <th className="num">Mkt value</th>
                  <th className="num">Unrealized P/L</th>
                </tr>
              </thead>
              <tbody>
                {portfolio.holdings.map((h) => (
                  <tr key={h.symbol}>
                    <td>
                      <Link to={`/stocks/${h.symbol}`} className="symbol-link">
                        {h.symbol}
                      </Link>
                    </td>
                    <td className="num">{h.quantity}</td>
                    <td className="num">{usd(h.averageCost)}</td>
                    <td className="num">{usd(h.currentPrice)}</td>
                    <td className="num">{usd(h.marketValue)}</td>
                    <td className={`num change ${changeClass(h.unrealizedPnL)}`}>
                      {usd(h.unrealizedPnL)} ({pct(h.unrealizedPnLPercent)})
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      )}
    </div>
  );
}

function Stat({ label, value, tone }) {
  return (
    <div className="stat">
      <span className="stat-label">{label}</span>
      <span className={`stat-value ${tone ?? ''}`}>{value}</span>
    </div>
  );
}
