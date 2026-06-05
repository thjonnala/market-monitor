import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { watchlistApi } from '../api/market';
import { errorMessage } from '../api/client';
import { Spinner, ErrorBanner, Empty } from '../components/Ui';
import { usd, pct, changeClass } from '../utils/format';

export default function Watchlist() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [symbol, setSymbol] = useState('');
  const [adding, setAdding] = useState(false);

  const load = async () => {
    setError('');
    try {
      setItems(await watchlistApi.list());
    } catch (e) {
      setError(errorMessage(e, 'Could not load your watchlist.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const add = async (e) => {
    e.preventDefault();
    const sym = symbol.trim().toUpperCase();
    if (!sym) return;
    setAdding(true);
    setError('');
    try {
      await watchlistApi.add(sym);
      setSymbol('');
      await load();
    } catch (e) {
      setError(errorMessage(e, `Could not add ${sym}.`));
    } finally {
      setAdding(false);
    }
  };

  const remove = async (sym) => {
    setError('');
    try {
      await watchlistApi.remove(sym);
      setItems((prev) => prev.filter((i) => i.symbol !== sym));
    } catch (e) {
      setError(errorMessage(e, `Could not remove ${sym}.`));
    }
  };

  return (
    <div className="page">
      <div className="section-head">
        <h2>Your Watchlist</h2>
      </div>

      <form className="inline-form" onSubmit={add}>
        <input
          placeholder="Add symbol, e.g. AAPL"
          value={symbol}
          onChange={(e) => setSymbol(e.target.value)}
          maxLength={12}
        />
        <button className="btn primary" type="submit" disabled={adding}>
          {adding ? 'Adding…' : 'Add'}
        </button>
      </form>

      <ErrorBanner message={error} />

      {loading ? (
        <Spinner />
      ) : items.length === 0 ? (
        <Empty>Your watchlist is empty. Add a symbol above to start tracking it.</Empty>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Symbol</th>
              <th>Name</th>
              <th className="num">Price</th>
              <th className="num">Change</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {items.map((i) => (
              <tr key={i.symbol}>
                <td>
                  <Link to={`/stocks/${i.symbol}`} className="symbol-link">
                    {i.symbol}
                  </Link>
                </td>
                <td className="muted">{i.name ?? '—'}</td>
                <td className="num">{usd(i.price)}</td>
                <td className={`num change ${changeClass(i.percentChange)}`}>{pct(i.percentChange)}</td>
                <td className="num">
                  <button className="btn ghost sm" onClick={() => remove(i.symbol)}>
                    Remove
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
