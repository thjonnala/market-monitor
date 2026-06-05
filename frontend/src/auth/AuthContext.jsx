import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { authApi } from '../api/market';
import { TOKEN_KEY } from '../api/client';

const USER_KEY = 'mm_user';
const AuthContext = createContext(null);

/** Provides auth state + actions to the app and persists the JWT in localStorage. */
export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  });

  // Keep React state and localStorage in sync across tabs.
  useEffect(() => {
    const onStorage = (e) => {
      if (e.key === TOKEN_KEY && !e.newValue) setUser(null);
    };
    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  }, []);

  const persist = (auth) => {
    localStorage.setItem(TOKEN_KEY, auth.token);
    const u = { email: auth.email, displayName: auth.displayName, expiresAt: auth.expiresAt };
    localStorage.setItem(USER_KEY, JSON.stringify(u));
    setUser(u);
  };

  const login = async (email, password) => {
    const auth = await authApi.login({ email, password });
    persist(auth);
    return auth;
  };

  const register = async (displayName, email, password) => {
    const auth = await authApi.register({ displayName, email, password });
    persist(auth);
    return auth;
  };

  const logout = () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    setUser(null);
  };

  const value = useMemo(
    () => ({ user, isAuthenticated: !!user, login, register, logout }),
    [user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
