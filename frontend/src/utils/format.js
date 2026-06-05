/** Small display formatters shared across pages. */

export const usd = (value) =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(
    Number.isFinite(value) ? value : 0,
  );

export const pct = (value) => {
  const n = Number.isFinite(value) ? value : 0;
  const sign = n > 0 ? '+' : '';
  return `${sign}${n.toFixed(2)}%`;
};

/** Tailwind-free helper: a CSS class for positive/negative/neutral values. */
export const changeClass = (value) =>
  value > 0 ? 'up' : value < 0 ? 'down' : 'flat';
