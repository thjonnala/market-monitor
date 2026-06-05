/** Tiny shared presentational helpers. */

export function Spinner({ label = 'Loading…' }) {
  return (
    <div className="spinner" role="status" aria-live="polite">
      <span className="spinner-dot" />
      {label}
    </div>
  );
}

export function ErrorBanner({ message }) {
  if (!message) return null;
  return <div className="error-banner" role="alert">{message}</div>;
}

export function Empty({ children }) {
  return <div className="empty-state">{children}</div>;
}
