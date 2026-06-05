import RecommendationBadge from './RecommendationBadge';

/** Shows the engine's recommendation plus the per-rule signal breakdown. */
export default function SuggestionPanel({ suggestion }) {
  if (!suggestion) return null;

  return (
    <div className="suggestion-panel">
      <div className="suggestion-header">
        <h3>Suggestion</h3>
        <RecommendationBadge
          recommendation={suggestion.recommendation}
          confidence={suggestion.confidence}
        />
      </div>

      <p className="suggestion-summary">{suggestion.summary}</p>

      {suggestion.signals?.length > 0 && (
        <ul className="signal-list">
          {suggestion.signals.map((s) => (
            <li key={s.rule}>
              <div className="signal-top">
                <span className="signal-rule">{s.rule}</span>
                <span className={`signal-lean ${s.lean.toLowerCase()}`}>{s.lean}</span>
              </div>
              <p className="signal-rationale">{s.rationale}</p>
              <div className="signal-weight">
                <span style={{ width: `${Math.round(s.weight * 100)}%` }} />
              </div>
            </li>
          ))}
        </ul>
      )}

      {suggestion.basedOnMockData && (
        <p className="mock-note">Based on mock data (no live API key configured).</p>
      )}
    </div>
  );
}
