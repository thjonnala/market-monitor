import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { marketApi } from '../api/market';
import { errorMessage } from '../api/client';
import RecommendationBadge from '../components/RecommendationBadge';
import Disclaimer from '../components/Disclaimer';
import { Spinner, ErrorBanner } from '../components/Ui';
import { usd, pct, changeClass } from '../utils/format';

export default function Home() {
  const [shares, setShares] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    marketApi
      .topShares(12)
      .then((data) => active && setShares(data))
      .catch((e) => active && setError(errorMessage(e, 'Could not load top shares.')))
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, []);

  return (
    <div className="page">
      <section className="hero">
        <h1>Spot the market&apos;s strongest signals.</h1>
        <p>
          Market Monitor scores trending stocks with transparent technical rules and lets you
          track a watchlist and a virtual portfolio — all with play money.
        </p>
        <div className="hero-cta">
          <Link to="/register" className="btn primary lg">
            Get started free
          </Link>
          <Link to="/login" className="btn ghost lg">
            Log in
          </Link>
        </div>
      </section>

      <section className="section">
        <div className="section-head">
          <h2>Top Shares to Buy</h2>
          <span className="muted">Curated by our signals engine</span>
        </div>

        <Disclaimer compact />
        <ErrorBanner message={error} />

        {loading ? (
          <Spinner label="Scoring stocks…" />
        ) : (
          <div className="card-grid">
            {shares.map((s) => (
              <Link to={`/stocks/${s.symbol}`} key={s.symbol} className="share-card">
                <div className="share-top">
                  <div>
                    <span className="share-symbol">{s.symbol}</span>
                    <span className="share-name">{s.name}</span>
                  </div>
                  <RecommendationBadge recommendation={s.recommendation} confidence={s.confidence} />
                </div>
                <div className="share-price">
                  <span className="price">{usd(s.price)}</span>
                  <span className={`change ${changeClass(s.percentChange)}`}>
                    {pct(s.percentChange)}
                  </span>
                </div>
                <p className="share-rationale">{s.rationale}</p>
                {s.priceIsMock ? (
                  <span className="mock-tag">mock price</span>
                ) : s.signalIsMock ? (
                  <span className="mock-tag">live price · sim. signal</span>
                ) : (
                  <span className="live-tag">live</span>
                )}
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
