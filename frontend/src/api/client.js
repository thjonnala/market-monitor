import axios from 'axios';

const TOKEN_KEY = 'mm_token';

/** Shared Axios instance pointed at the Market Monitor API. */
const client = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080/api',
  headers: { 'Content-Type': 'application/json' },
});

// Attach the JWT (if present) to every request.
client.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// On 401, clear the stale token so the app falls back to the public experience.
client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem(TOKEN_KEY);
    }
    return Promise.reject(error);
  },
);

/** Pull a human-readable message out of an Axios error (ProblemDetails-aware). */
export function errorMessage(error, fallback = 'Something went wrong.') {
  const data = error?.response?.data;
  if (typeof data === 'string') return data;
  return data?.detail || data?.title || error?.message || fallback;
}

export { TOKEN_KEY };
export default client;
