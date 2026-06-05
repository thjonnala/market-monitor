import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { marketApi, watchlistApi } from '../api/market';
import { errorMessage } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import PriceChart from '../components/PriceChart';
import SuggestionPanel from '../components/SuggestionPanel';
import Disclaimer from '../components/Disclaimer';
import { Spinner, ErrorBanner } from '../components/Ui';
import { usd, pct, changeClass } from '../utils/format';

const RANGES = ['1D', '1W', '1M', '3M', '1Y'];

export default function StockDetail() {
  const { symbol } = useParams();
  const sym = (symbol ?? '').toUpperCase();
  const { isAuthenticated } = useAuth();

  const [quote, setQuote] = useState(null);
  const [suggestion, setSuggestion] = useState(null);
  const [candles, setCandles] = useState([]);
  const [range, setRange] = useState('1M');
  const [loading, setLoading] = useState(true);
  const [chartLoading, setChartLoading] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  // Load quote + suggestion once per symbol.
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    Promise.all([marketApi.quote(sym), marketApi.suggestion(sym)])
      .then(([q, s]) => {
        if (!active) return;
        setQuote(q);
        setSuggestion(s);
      })
      .catch((e) => active && setError(errorMessage(e, `Could not load ${sym}.`)))
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, [sym]);

  // Reload candles when symbol or range changes.
  useEffect(() => {
    let active = true;
    setChartLoading(true);
    marketApi
      .candles(sym, range)
      .then((c) => active && setCandles(c))
      .catch(() => active && setCandles([]))
      .finally(() => active && setChartLoading(false));
    return () => {
      active = false;
    };
  }, [sym, range]);

  const addToWatchlist = async () => {
    setNotice('');
    try {
      await watchlistApi.add(sym);
      setNotice(`${sym} added to your watchlist.`);
    } catch (e) {
      setNotice(errorMessage(e, `Could not add ${sym}.`));
    }
  };

  if (loading) return <div className="page"><Spinner label={`Loading ${sym}…`} /></div>;

  return (
    <div className="page">
      <ErrorBanner message={error} />

      {quote && (
        <div className="detail-head">
          <div>
            <h1 className="detail-symbol">{quote.symbol}</h1>
            {quote.name && <span className="detail-name">{quote.name}</span>}
          </div>
          <div className="detail-price">
            <span className="price-lg">{usd(quote.price)}</span>
            <span className={`change ${changeClass(quote.percentChange)}`}>
              {usd(quote.change)} ({pct(quote.percentChange)})
            </span>
            {quote.isMock && <span className="mock-tag">mock data</span>}
          </div>
          {isAuthenticated && (
            <button className="btn ghost" onClick={addToWatchlist}>
              ☆ Add to watchlist
            </button>
          )}
        </div>
      )}

      {notice && <div className="notice">{notice}</div>}

      <div className="detail-grid">
        <div className="detail-chart card">
          <div className="range-tabs">
            {RANGES.map((r) => (
              <button
                key={r}
                className={r === range ? 'range active' : 'range'}
                onClick={() => setRange(r)}
              >
                {r}
              </button>
            ))}
          </div>
          {chartLoading ? <Spinner label="Loading chart…" /> : <PriceChart data={candles} />}
          {!chartLoading && candles.some((c) => c.isMock) && (
            <p className="mock-note">
              Chart shows simulated history — live historical candles aren&apos;t available on the
              current market-data plan.
            </p>
          )}

          {quote && (
            <div className="quote-stats">
              <Stat label="Open" value={usd(quote.open)} />
              <Stat label="High" value={usd(quote.high)} />
              <Stat label="Low" value={usd(quote.low)} />
              <Stat label="Prev close" value={usd(quote.previousClose)} />
            </div>
          )}
        </div>

        <aside className="detail-aside card">
          <SuggestionPanel suggestion={suggestion} />
        </aside>
      </div>

      <Disclaimer />
    </div>
  );
}

function Stat({ label, value }) {
  return (
    <div className="qstat">
      <span className="qstat-label">{label}</span>
      <span className="qstat-value">{value}</span>
    </div>
  );
}
