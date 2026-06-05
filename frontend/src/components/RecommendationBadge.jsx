/** Colored BUY / SELL / HOLD pill, optionally with a confidence percentage. */
export default function RecommendationBadge({ recommendation, confidence }) {
  const rec = (recommendation ?? 'HOLD').toUpperCase();
  const cls = rec === 'BUY' ? 'badge buy' : rec === 'SELL' ? 'badge sell' : 'badge hold';
  return (
    <span className={cls}>
      {rec}
      {confidence != null && <em>{Math.round(confidence * 100)}%</em>}
    </span>
  );
}
