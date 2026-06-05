import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { usd } from '../utils/format';

/** Area chart of closing prices. Expects [{ date, close }, ...]. */
export default function PriceChart({ data }) {
  if (!data || data.length === 0) {
    return <div className="chart-empty">No price data available.</div>;
  }

  const points = data.map((c) => ({
    date: new Date(c.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
    close: c.close,
  }));

  const closes = points.map((p) => p.close);
  const min = Math.min(...closes);
  const max = Math.max(...closes);
  const pad = (max - min) * 0.05 || 1;
  const rising = closes[closes.length - 1] >= closes[0];
  const color = rising ? '#16a34a' : '#dc2626';

  return (
    <ResponsiveContainer width="100%" height={320}>
      <AreaChart data={points} margin={{ top: 10, right: 16, left: 0, bottom: 0 }}>
        <defs>
          <linearGradient id="priceFill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor={color} stopOpacity={0.35} />
            <stop offset="95%" stopColor={color} stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" stroke="#e3e6eb" />
        <XAxis dataKey="date" tick={{ fill: '#6b7280', fontSize: 12 }} minTickGap={28} />
        <YAxis
          domain={[min - pad, max + pad]}
          tick={{ fill: '#6b7280', fontSize: 12 }}
          tickFormatter={(v) => usd(v)}
          width={72}
        />
        <Tooltip
          formatter={(v) => [usd(v), 'Close']}
          contentStyle={{ background: '#ffffff', border: '1px solid #e3e6eb', borderRadius: 8 }}
          labelStyle={{ color: '#1a1d23' }}
        />
        <Area
          type="monotone"
          dataKey="close"
          stroke={color}
          strokeWidth={2}
          fill="url(#priceFill)"
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}
