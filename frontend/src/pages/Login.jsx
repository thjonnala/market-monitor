import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../api/client';
import { ErrorBanner } from '../components/Ui';

const DEMO = { email: 'demo@marketmonitor.local', password: 'Demo1234!' };

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const redirectTo = location.state?.from?.pathname || '/';

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e) => {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await login(email, password);
      navigate(redirectTo, { replace: true });
    } catch (err) {
      setError(errorMessage(err, 'Login failed.'));
    } finally {
      setBusy(false);
    }
  };

  const fillDemo = () => {
    setEmail(DEMO.email);
    setPassword(DEMO.password);
  };

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={submit}>
        <h1>Welcome back</h1>
        <p className="muted">Log in to manage your watchlist and portfolio.</p>

        <ErrorBanner message={error} />

        <label>
          Email
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoComplete="email"
          />
        </label>
        <label>
          Password
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            autoComplete="current-password"
          />
        </label>

        <button className="btn primary lg" type="submit" disabled={busy}>
          {busy ? 'Logging in…' : 'Log in'}
        </button>

        <button type="button" className="btn ghost" onClick={fillDemo}>
          Use demo account
        </button>

        <p className="auth-alt">
          No account? <Link to="/register">Create one</Link>
        </p>
      </form>
    </div>
  );
}
