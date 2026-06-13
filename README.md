# 📈 Market Monitor

A full-stack demo app for tracking stocks, scoring them with transparent technical
signals, and managing a **virtual** (play-money) portfolio.

> ⚠️ **Not financial advice.** Market Monitor is an educational project. All suggestions
> are produced by simple, automated technical rules and may run on delayed or mock data.
> Portfolios use virtual cash — **no real money and no real trades are ever involved.**

---

## Tech stack

| Layer        | Technology |
|--------------|------------|
| Frontend     | React (JavaScript) + Vite, React Router, Axios, Recharts |
| Backend      | .NET 10 Web API (C#), layered API / Application / Domain / Infrastructure |
| ORM          | Entity Framework Core (code-first migrations) + Npgsql |
| Database     | PostgreSQL (open source) — local via Docker · managed Postgres in production |
| Auth         | ASP.NET Core Identity + JWT bearer tokens (hashed passwords) |
| Market data  | Finnhub (free tier) behind an `IMarketDataProvider` abstraction, with caching, 429 backoff, and mock fallback |

```
market-monitor/
├── backend/
│   ├── MarketMonitor.sln
│   ├── src/
│   │   ├── MarketMonitor.Domain/          # entities, enums, value objects (no deps)
│   │   ├── MarketMonitor.Application/      # interfaces, DTOs, suggestions engine + service
│   │   ├── MarketMonitor.Infrastructure/   # EF DbContext, migrations, Finnhub/mock, JWT, services
│   │   └── MarketMonitor.Api/              # controllers, DI, auth, CORS, exception handler
│   └── tests/MarketMonitor.Tests/          # suggestions-engine unit tests
└── frontend/                               # Vite React app (Home, Login, Watchlist, Portfolio, Stock detail)
```

---

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` → 10.x)
- **Node.js 18+** and npm
- **PostgreSQL** running locally (easiest: `docker compose -f backend/docker-compose.dev.yml up -d`)
- *(optional)* A free **Finnhub** API key — without one the app runs on deterministic mock data

---

## 1. Backend setup

All commands run from `backend/`.

### a. Start PostgreSQL and configure the connection

Start a local Postgres (Docker):

```bash
docker compose -f backend/docker-compose.dev.yml up -d
```

Local dev uses the connection string in
[`src/MarketMonitor.Api/appsettings.Development.json`](backend/src/MarketMonitor.Api/appsettings.Development.json),
which already matches the compose file:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=marketmonitor;Username=postgres;Password=postgres"
}
```

The provider is selected by `Database:Provider` (`Postgres`). The same EF model is used
locally and in production — only the connection string differs (see
[Deploying the API to Render](#deploying-the-api-to-render-docker--postgresql)). A
`postgres://…` URL is also accepted and normalized for Npgsql automatically.

### b. Set secrets (local) — JWT key and Finnhub key

A dev-only JWT signing key ships in `appsettings.Development.json` so the app runs out of
the box. To override it (recommended) and to add a live market-data key, use user-secrets:

```bash
cd src/MarketMonitor.Api
dotnet user-secrets init        # first time only
dotnet user-secrets set "Jwt:SigningKey" "<any-random-string-32+chars>"
dotnet user-secrets set "MarketData:ApiKey" "<your-finnhub-api-key>"
```

> **Where to get a free key:** sign up at <https://finnhub.io> → the dashboard shows your
> API key. Leave `MarketData:ApiKey` **empty** to run entirely on mock data.
>
> 🔑 **Never commit any API key.** Keys live in user-secrets locally and in environment
> variables in production.

### c. Apply migrations + seed, then run

The API **applies migrations and seeds sample data automatically on startup**
(`SeedOnStartup: true`). Just run it:

```bash
cd backend
dotnet run --project src/MarketMonitor.Api --urls http://localhost:5080
```

The API will be on **http://localhost:5080** (OpenAPI at `/openapi/v1.json`, health at `/health`).

Seeding creates 10 sample symbols and a **demo user**:

| Email                        | Password    |
|------------------------------|-------------|
| `demo@marketmonitor.local`   | `Demo1234!` |

#### Managing migrations manually (optional)

```bash
# add a new migration
dotnet ef migrations add <Name> \
  --project src/MarketMonitor.Infrastructure \
  --startup-project src/MarketMonitor.Api \
  --output-dir Persistence/Migrations

# apply migrations without running the app
dotnet ef database update \
  --project src/MarketMonitor.Infrastructure \
  --startup-project src/MarketMonitor.Api
```

### d. Run the tests

```bash
cd backend
dotnet test
```

---

## 2. Frontend setup

All commands run from `frontend/`.

```bash
cd frontend
npm install
npm run dev          # http://localhost:5173
```

The API base URL is read from `VITE_API_BASE_URL` in `frontend/.env`
(defaults to `http://localhost:5080/api`). Copy `.env.example` to `.env` to customize.

CORS on the backend already allows `http://localhost:5173`
(configurable via `Cors:AllowedOrigins`).

---

## 3. Using the app

1. Open **http://localhost:5173**.
2. The public **Home** page shows *Top Shares to Buy* with a BUY/SELL/HOLD badge,
   price, % change, and rationale.
3. **Sign up**, or **Log in** with the demo account (the login page has a *Use demo
   account* button).
4. Add symbols to your **Watchlist**, build a **virtual Portfolio** (you start with
   $100,000 of play money), and open any **Stock detail** page for a live quote,
   price chart (selectable ranges), and the explainable suggestion panel.

---

## How the suggestions engine works

Located in `MarketMonitor.Application/Suggestions`. It is a **modular rule engine**: each
`ISignalRule` independently votes BUY/SELL/HOLD with a weight and a plain-English
rationale, and `SuggestionEngine` aggregates the votes into a final recommendation plus a
confidence score. Add a rule by implementing `ISignalRule` — nothing else changes.

The shipped rules follow a **momentum / trend philosophy** (fitting an app about
"trending, strong-signal stocks"):

- **SMA 20/50 crossover** — short SMA above long SMA ⇒ bullish trend.
- **RSI(14)** — above the mid-line ⇒ bullish momentum; overbought/oversold readings are
  still directional but damped and flagged.
- **% of 30-day range** — price pushing the top of its range ⇒ momentum (Buy), lagging at
  the bottom ⇒ Sell. A deadzone ignores tiny ranges so flat stocks read as Hold.

Unit tests for the engine and indicators live in `backend/tests/MarketMonitor.Tests`.

---

## Market-data resilience (free-tier friendly)

`IMarketDataProvider` is implemented by `FinnhubMarketDataProvider` (raw HTTP) wrapped by
`ResilientMarketDataProvider`, which adds:

- **Caching** with a short TTL (quotes 30s, candles 5m) to stay under free-tier limits.
- **Rate-limit backoff** — after an HTTP 429 it stops calling the live API for a cooldown.
- **Mock fallback** — if the live API is unreachable, errors, or no key is configured, it
  serves deterministic mock data so the app always runs. Mock data is tagged in the UI.

> **Note:** Finnhub's `/stock/candle` endpoint is premium-only on the free tier and returns
> 403. When that happens the chart and indicators transparently fall back to mock candles.
> Swap in another provider (Alpha Vantage, Twelve Data) by implementing
> `IMarketDataProvider` — no business-logic changes required.

---

## Deploying the frontend to GitHub Pages (custom domain)

The frontend (Vite SPA) deploys to **GitHub Pages** via GitHub Actions and is served at the
custom domain **https://marketmonitor.thiruapps.com**.

How it's wired:
- [`.github/workflows/deploy-frontend.yml`](.github/workflows/deploy-frontend.yml) builds
  `frontend/` on every push to `main` (that touches `frontend/`) and publishes `frontend/dist`.
- [`frontend/public/CNAME`](frontend/public/CNAME) holds `marketmonitor.thiruapps.com`, so the
  custom domain persists across deploys.
- The workflow copies `index.html` → `404.html` so client-side routes (e.g. `/stocks/AAPL`)
  work on refresh (GitHub Pages has no rewrite rules).
- The API base URL is baked in from [`frontend/.env.production`](frontend/.env.production)
  (`VITE_API_BASE_URL` → the Render API). Vite's `base` stays `/` since the site is
  served at the domain root.

**One-time GitHub setup:**
1. Repo **Settings → Pages → Build and deployment → Source: `GitHub Actions`**.
2. **Settings → Pages → Custom domain:** `marketmonitor.thiruapps.com` (and enable *Enforce HTTPS*).
3. DNS: a `CNAME` record for `marketmonitor` → `thjonnala.github.io`.

The deployed origin (`https://marketmonitor.thiruapps.com`) is already allowed by the API's
CORS (`Cors:AllowedOrigins: ["*"]` in production).

## Deploying the API to Render (Docker + PostgreSQL)

The backend runs as a **Docker** web service on **Render**, backed by **Render's managed
PostgreSQL** — a fully open-source stack (no Azure). Everything is described in
[`render.yaml`](render.yaml) and [`backend/Dockerfile`](backend/Dockerfile).

**One-time deploy:**
1. In the [Render dashboard](https://dashboard.render.com): **New → Blueprint** → connect this
   GitHub repo. Render reads `render.yaml` and provisions the **web service** + **Postgres**.
2. On the `market-monitor-api` service, set **`MarketData__ApiKey`** (Environment tab) to your
   Finnhub key for live prices — or leave it empty to run on mock data.
3. **Apply.** Render builds the image, the API connects to Postgres, applies EF migrations,
   seeds sample data, and `/health` returns 200.

The blueprint wires these env vars automatically (no secrets hard-coded):

| Setting | Source |
|---------|--------|
| `Database__Provider` = `Postgres` | render.yaml |
| `ConnectionStrings__DefaultConnection` | injected from the managed Postgres DB |
| `Jwt__SigningKey` | generated by Render |
| `Cors__AllowedOrigins__0` | `https://marketmonitor.thiruapps.com` |
| `MarketData__ApiKey` | set in the dashboard (secret) |

> A `postgres://…` connection URL is accepted and normalized to Npgsql format automatically.
> **Free-tier notes:** the web service sleeps after ~15 min idle (first request cold-starts in
> ~1 min); Render's free Postgres expires after ~30 days — for a permanent free DB, point
> `ConnectionStrings__DefaultConnection` at **Neon** or **Supabase** instead.

After the API is live, set `VITE_API_BASE_URL` in
[`frontend/.env.production`](frontend/.env.production) to the Render URL and push (the GitHub
Pages workflow rebuilds the frontend against it).

---

## API reference (summary)

| Method & route | Auth | Description |
|----------------|------|-------------|
| `POST /api/auth/register` | – | Create an account, returns a JWT |
| `POST /api/auth/login` | – | Log in, returns a JWT |
| `GET /api/market/top-shares?limit=8` | – | Curated home-page list |
| `GET /api/market/quote/{symbol}` | – | Latest quote |
| `GET /api/market/candles/{symbol}?range=1M` | – | OHLC candles (`1D,1W,1M,3M,1Y`) |
| `GET /api/market/suggestion/{symbol}` | – | BUY/SELL/HOLD + rationale |
| `GET /api/watchlist` · `POST` · `DELETE /{symbol}` | ✅ | Personal watchlist |
| `GET /api/portfolio` · `POST /buy` · `POST /sell` | ✅ | Virtual portfolio |
