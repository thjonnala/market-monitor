import client from './client';

/** Thin API wrappers grouped by area. Components call these, not Axios directly. */

export const marketApi = {
  topShares: (limit = 8) =>
    client.get('/market/top-shares', { params: { limit } }).then((r) => r.data),
  quote: (symbol) => client.get(`/market/quote/${symbol}`).then((r) => r.data),
  candles: (symbol, range = '1M') =>
    client.get(`/market/candles/${symbol}`, { params: { range } }).then((r) => r.data),
  suggestion: (symbol) => client.get(`/market/suggestion/${symbol}`).then((r) => r.data),
};

export const authApi = {
  register: (payload) => client.post('/auth/register', payload).then((r) => r.data),
  login: (payload) => client.post('/auth/login', payload).then((r) => r.data),
};

export const watchlistApi = {
  list: () => client.get('/watchlist').then((r) => r.data),
  add: (symbol) => client.post('/watchlist', { symbol }).then((r) => r.data),
  remove: (symbol) => client.delete(`/watchlist/${symbol}`).then((r) => r.data),
};

export const portfolioApi = {
  get: () => client.get('/portfolio').then((r) => r.data),
  buy: (symbol, quantity) =>
    client.post('/portfolio/buy', { symbol, quantity }).then((r) => r.data),
  sell: (symbol, quantity) =>
    client.post('/portfolio/sell', { symbol, quantity }).then((r) => r.data),
};
