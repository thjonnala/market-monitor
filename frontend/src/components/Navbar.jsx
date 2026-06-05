import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export default function Navbar() {
  const { isAuthenticated, user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  const linkClass = ({ isActive }) => (isActive ? 'nav-link active' : 'nav-link');

  return (
    <header className="navbar">
      <Link to="/" className="brand">
        📈 Market<span>Monitor</span>
      </Link>

      <nav className="nav-links">
        <NavLink to="/" className={linkClass} end>
          Home
        </NavLink>
        {isAuthenticated && (
          <>
            <NavLink to="/watchlist" className={linkClass}>
              Watchlist
            </NavLink>
            <NavLink to="/portfolio" className={linkClass}>
              Portfolio
            </NavLink>
          </>
        )}
      </nav>

      <div className="nav-account">
        {isAuthenticated ? (
          <>
            <span className="nav-user">{user.displayName}</span>
            <button className="btn ghost" onClick={handleLogout}>
              Log out
            </button>
          </>
        ) : (
          <>
            <NavLink to="/login" className="btn ghost">
              Log in
            </NavLink>
            <NavLink to="/register" className="btn primary">
              Sign up
            </NavLink>
          </>
        )}
      </div>
    </header>
  );
}
