import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

/** Gates a route behind authentication, preserving the intended destination. */
export default function ProtectedRoute({ children }) {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }
  return children;
}
