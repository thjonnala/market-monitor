# Market Monitor — Product Backlog (Epic → Features → User Stories)

> Agile breakdown for the Market Monitor application. One Epic, the required Features
> under it, and the required User Stories (with acceptance criteria) under each Feature.
>
> ⚠️ **Not financial advice.** Market Monitor is an educational product. All money is
> virtual, no real trades are placed, and signals are simple, explainable heuristics.

---

## EPIC

### EPIC-1 — Market Monitor: Virtual Stock Tracking & Signals Platform

**Summary:** Provide retail/novice investors a safe, educational web app to discover
trending stocks, track them, practice investing with virtual money, and understand simple
BUY/SELL/HOLD signals — without risking real capital.

**Problem / Opportunity:** New investors lack a risk-free way to learn how markets move,
test ideas, and interpret common technical signals. Existing tools are either real-money
brokerages or opaque "black box" tip services.

**Business value:**
- Drives engagement and learning through a no-risk virtual portfolio.
- Differentiates via **transparent, explainable** signals (every recommendation shows its
  rationale and confidence).
- Extensible foundation (pluggable market-data providers and signal rules).

**In scope:** Auth, public discovery (Top Shares), watchlist, virtual portfolio, stock
detail + charts, explainable suggestions engine, resilient market-data integration,
secure deployment.

**Out of scope:** Real brokerage/trading, real money movement, options/derivatives,
personalized financial advice, social/community features (future epic).

**Success metrics:**
- % of registered users who create a portfolio and place ≥1 virtual trade.
- Watchlist adds per active user; D7/D30 retention.
- Home → Stock-detail click-through rate.
- API availability ≥ 99% and p95 quote latency < 500 ms (cached).

**Non-functional requirements:** secure auth (hashed passwords, JWT), input validation,
graceful market-data degradation, responsive UI, and a visible "not financial advice"
disclaimer on every signal surface.

---

## FEATURES & USER STORIES

| Feature | Title |
|---------|-------|
| FEAT-1 | User Authentication & Account Management |
| FEAT-2 | Public Home & "Top Shares to Buy" Discovery |
| FEAT-3 | Personal Watchlist |
| FEAT-4 | Virtual Portfolio & Paper Trading |
| FEAT-5 | Stock Detail & Price Charts |
| FEAT-6 | Suggestions / Signals Engine |
| FEAT-7 | Market-Data Integration & Resilience |
| FEAT-8 | Platform, Security, Compliance & Deployment (cross-cutting) |

---

### FEAT-1 — User Authentication & Account Management

*Goal: let users securely register, sign in, and access protected features.*

#### US-1.1 — Register an account
**As a** new visitor, **I want** to register with email, display name, and password, **so that**
I can save a watchlist and portfolio.
**Acceptance criteria:**
- Given valid email, display name (3–40 chars), and password (≥8 chars), when I submit, then an
  account is created and I'm logged in (JWT issued).
- Passwords are stored **hashed** (never plaintext).
- Duplicate email returns a clear "account already exists" error (HTTP 409).
- Invalid input shows field-level validation messages.

#### US-1.2 — Log in
**As a** registered user, **I want** to log in with email and password, **so that** I can access my data.
**Acceptance criteria:**
- Valid credentials return a signed JWT with expiry and my display name.
- Invalid email or password returns the same generic "invalid email or password" message (no user enumeration).
- A "Use demo account" shortcut pre-fills seeded demo credentials in non-production.

#### US-1.3 — Log out
**As a** logged-in user, **I want** to log out, **so that** my session token is cleared from the device.
**Acceptance criteria:**
- Logging out removes the stored token and returns me to the public experience.
- Protected routes become inaccessible after logout.

#### US-1.4 — Protected access & session expiry
**As a** user, **I want** protected pages and APIs to require authentication, **so that** my data stays private.
**Acceptance criteria:**
- Frontend routes `/watchlist` and `/portfolio` redirect unauthenticated users to login (preserving intended destination).
- API endpoints under watchlist/portfolio return 401 without a valid token.
- An expired/invalid token is rejected and silently logs the user out client-side.

---

### FEAT-2 — Public Home & "Top Shares to Buy" Discovery

*Goal: a public landing page that surfaces a curated, signal-ranked list of stocks.*

#### US-2.1 — View Top Shares (public)
**As a** visitor (no login), **I want** to see a "Top Shares to Buy" list, **so that** I can discover strong-signal stocks.
**Acceptance criteria:**
- Page loads without authentication.
- Each card shows symbol, company name, current price, % change, a BUY/SELL/HOLD badge, confidence %, and a one-line rationale.
- List is ranked (BUY first by confidence, then HOLD, then SELL).
- A "not financial advice" disclaimer is visible.

#### US-2.2 — Data-source transparency on cards
**As a** visitor, **I want** to know whether a card uses live or simulated data, **so that** I can trust it appropriately.
**Acceptance criteria:**
- Each card indicates data state: "live", "live price · sim. signal", or "mock price".
- The flag accurately reflects whether the quote and/or the signal inputs were live vs mock.

#### US-2.3 — Navigate to stock detail
**As a** visitor, **I want** to click a Top Share to open its detail page, **so that** I can learn more.
**Acceptance criteria:**
- Clicking a card navigates to `/stocks/{symbol}` and loads that symbol's detail.

---

### FEAT-3 — Personal Watchlist

*Goal: let users track symbols of interest, saved per user.*

#### US-3.1 — Add a symbol to watchlist
**As a** logged-in user, **I want** to add a symbol, **so that** I can monitor it.
**Acceptance criteria:**
- Adding a valid symbol persists it to my watchlist (DB) and shows current price/% change.
- Duplicate add returns a clear "already on your watchlist" message.
- Unknown/invalid symbol is rejected with a helpful error.

#### US-3.2 — View my watchlist
**As a** logged-in user, **I want** to see my watchlist with live prices, **so that** I can monitor at a glance.
**Acceptance criteria:**
- Watchlist shows symbol, name, price, % change, ordered by most recently added.
- Each row links to the stock detail page.
- Empty state guides me to add my first symbol.

#### US-3.3 — Remove a symbol
**As a** logged-in user, **I want** to remove a symbol, **so that** my list stays relevant.
**Acceptance criteria:**
- Removing a symbol deletes it from my watchlist and updates the UI immediately.
- Removing a symbol not on my list returns 404.

---

### FEAT-4 — Virtual Portfolio & Paper Trading

*Goal: practice investing with virtual cash; track holdings, cost basis, and P/L.*

#### US-4.1 — Auto-create a virtual portfolio
**As a** logged-in user, **I want** a portfolio with starting virtual cash, **so that** I can begin paper trading.
**Acceptance criteria:**
- On first visit, a portfolio is created with a fixed starting virtual cash balance (e.g., $100,000).
- The portfolio is private to me.

#### US-4.2 — Buy shares with virtual cash
**As a** logged-in user, **I want** to buy N shares of a symbol, **so that** I can build a position.
**Acceptance criteria:**
- Buy executes at the current quote price; cash is debited by price × quantity.
- A buy exceeding available cash is rejected with an "insufficient virtual cash" message.
- Buying more of an existing holding updates the **weighted-average cost basis**.
- Quantity must be a positive number; invalid input is rejected.
- The trade is recorded (audit trail).

#### US-4.3 — Sell shares
**As a** logged-in user, **I want** to sell shares I hold, **so that** I can realize gains/losses.
**Acceptance criteria:**
- Sell credits cash by price × quantity and reduces the position.
- Selling more than I hold is rejected.
- A holding that reaches zero quantity is removed.

#### US-4.4 — View holdings, value & P/L
**As a** logged-in user, **I want** to see my holdings with current value and unrealized P/L, **so that** I can judge performance.
**Acceptance criteria:**
- For each holding: quantity, average cost, current price, market value, unrealized P/L ($ and %).
- Portfolio summary: total value, cash, holdings value, total return ($ and %).
- Values use live quotes when available; gains/losses are color-coded.
- A clear "play money / no real trades" disclaimer is shown.

---

### FEAT-5 — Stock Detail & Price Charts

*Goal: a per-symbol page with quote, chart, and the suggestion panel.*

#### US-5.1 — View live quote
**As a** user, **I want** to see a symbol's live quote and day stats, **so that** I understand its current state.
**Acceptance criteria:**
- Shows price, absolute & % change, open, high, low, previous close, and data freshness/mock flag.

#### US-5.2 — View price chart over selectable ranges
**As a** user, **I want** a price chart with range options, **so that** I can see the trend.
**Acceptance criteria:**
- Range selector supports 1D, 1W, 1M, 3M, 1Y; chart re-loads on change.
- Chart renders closing prices; empty/unavailable data shows a friendly message.
- If candles are simulated, a "simulated history" note is shown.

#### US-5.3 — Add to watchlist / act from detail
**As a** logged-in user, **I want** to add the symbol to my watchlist from the detail page, **so that** I can track it without leaving.
**Acceptance criteria:**
- An "Add to watchlist" action is available to authenticated users and confirms success/failure inline.

---

### FEAT-6 — Suggestions / Signals Engine

*Goal: produce explainable BUY/SELL/HOLD recommendations with confidence and rationale.*

#### US-6.1 — Generate a recommendation per symbol
**As a** user, **I want** a BUY/SELL/HOLD recommendation for a symbol, **so that** I get a quick read.
**Acceptance criteria:**
- Engine returns a recommendation, a confidence score (0–100%), and a one-line summary.
- Recommendation is derived by aggregating multiple independent signal rules.

#### US-6.2 — See the rationale (explainability)
**As a** user, **I want** to see *why* a recommendation was made, **so that** I can learn and trust it.
**Acceptance criteria:**
- The suggestion panel lists each contributing signal (e.g., SMA crossover, RSI, % of range) with its lean, weight, and plain-English rationale.
- The headline rationale matches the final recommendation's direction.

#### US-6.3 — Modular, extensible rules
**As a** developer/product owner, **I want** to add/modify signal rules without touching the engine, **so that** the logic can evolve.
**Acceptance criteria:**
- New rules implement a common interface and are registered into the rule set.
- Adding/removing a rule changes output without modifying aggregation code.
- Rules and indicators are covered by unit tests.

#### US-6.4 — Insufficient-data handling
**As a** user, **I want** a sensible result when data is thin, **so that** I'm not misled.
**Acceptance criteria:**
- With insufficient history, the engine returns HOLD with 0 confidence and an explanatory message.

#### US-6.5 — Disclaimer on all signal surfaces
**As a** compliance owner, **I want** a "not financial advice" disclaimer wherever signals appear, **so that** the product is responsible.
**Acceptance criteria:**
- Disclaimer is visible on Home, Stock Detail, and Portfolio surfaces that show signals.

---

### FEAT-7 — Market-Data Integration & Resilience

*Goal: pluggable, resilient market data that respects free-tier limits and never breaks the app.*

#### US-7.1 — Pluggable provider abstraction
**As a** developer, **I want** market data behind a single interface, **so that** providers can be swapped without business-logic changes.
**Acceptance criteria:**
- Quote, batch-quote, and candle access go through one provider interface.
- Providers (e.g., Finnhub, Twelve Data, Mock) are interchangeable via configuration.

#### US-7.2 — Caching & rate-limit handling
**As an** operator, **I want** caching and backoff, **so that** we stay within free-tier limits.
**Acceptance criteria:**
- Quotes and candles are cached with sensible TTLs.
- On HTTP 429, the provider backs off and serves cached/mock data temporarily, then self-heals.

#### US-7.3 — Graceful mock fallback
**As a** user, **I want** the app to keep working when live data is unavailable, **so that** I'm never blocked.
**Acceptance criteria:**
- With no API key or an unreachable provider, the app serves deterministic mock data.
- Mock vs live state is surfaced honestly in the UI (price and signal separately).

#### US-7.4 — Configuration-driven provider & secrets
**As an** operator, **I want** to choose the provider and supply keys via config/secrets, **so that** no keys are hard-coded.
**Acceptance criteria:**
- Provider and API key come from configuration (user-secrets/env vars), never committed to source.
- Switching providers requires no code change.

---

### FEAT-8 — Platform, Security, Compliance & Deployment (cross-cutting)

*Goal: a secure, deployable, well-engineered foundation.*

#### US-8.1 — Secure auth & secrets
**As a** security owner, **I want** hashed passwords, signed JWTs, and externalized secrets, **so that** the app is safe.
**Acceptance criteria:**
- Passwords hashed via Identity; JWT signing key and DB/API secrets come from secrets/env, not git.

#### US-8.2 — Validation & global error handling
**As a** user, **I want** clear errors and stable behavior, **so that** failures are graceful.
**Acceptance criteria:**
- Inputs are validated; business errors return appropriate 4xx with messages; unexpected errors return a safe 500 (details only in non-prod).

#### US-8.3 — Database provisioning & migrations
**As an** operator, **I want** code-first migrations and seed data, **so that** the DB is reproducible.
**Acceptance criteria:**
- The same EF model targets local SQL Express and Azure SQL via configuration.
- Migrations apply and sample symbols + a demo user seed on startup.

#### US-8.4 — CORS & API/SPA integration
**As a** developer, **I want** the SPA to call the API across origins, **so that** frontend and backend integrate cleanly.
**Acceptance criteria:**
- CORS allows the configured frontend origin(s); the SPA reads the API base URL from configuration per environment.

#### US-8.5 — Cloud deployment
**As an** operator, **I want** the API on Azure App Service (+ Azure SQL) and the SPA on Vercel, **so that** the app is publicly hosted.
**Acceptance criteria:**
- API deploys to Azure App Service and connects to Azure SQL; health endpoint returns 200.
- Frontend builds on Vercel (Vite) and targets the deployed API via environment configuration.

---

## Backlog Summary — Priority (MoSCoW) & Estimates (story points)

| ID | Story | Feature | Priority | Points |
|----|-------|---------|----------|:------:|
| US-1.1 | Register an account | FEAT-1 Auth | Must | 3 |
| US-1.2 | Log in | FEAT-1 Auth | Must | 3 |
| US-1.3 | Log out | FEAT-1 Auth | Must | 1 |
| US-1.4 | Protected access & session expiry | FEAT-1 Auth | Must | 3 |
| US-2.1 | View Top Shares (public) | FEAT-2 Home | Must | 5 |
| US-2.2 | Data-source transparency | FEAT-2 Home | Should | 2 |
| US-2.3 | Navigate to stock detail | FEAT-2 Home | Must | 1 |
| US-3.1 | Add a symbol to watchlist | FEAT-3 Watchlist | Must | 3 |
| US-3.2 | View my watchlist | FEAT-3 Watchlist | Must | 3 |
| US-3.3 | Remove a symbol | FEAT-3 Watchlist | Must | 2 |
| US-4.1 | Auto-create a virtual portfolio | FEAT-4 Portfolio | Must | 2 |
| US-4.2 | Buy shares with virtual cash | FEAT-4 Portfolio | Must | 5 |
| US-4.3 | Sell shares | FEAT-4 Portfolio | Must | 3 |
| US-4.4 | View holdings, value & P/L | FEAT-4 Portfolio | Must | 5 |
| US-5.1 | View live quote | FEAT-5 Detail | Must | 2 |
| US-5.2 | Price chart with selectable ranges | FEAT-5 Detail | Must | 5 |
| US-5.3 | Add to watchlist from detail | FEAT-5 Detail | Should | 2 |
| US-6.1 | Generate a recommendation | FEAT-6 Signals | Must | 5 |
| US-6.2 | See the rationale (explainability) | FEAT-6 Signals | Must | 3 |
| US-6.3 | Modular, extensible rules | FEAT-6 Signals | Should | 5 |
| US-6.4 | Insufficient-data handling | FEAT-6 Signals | Should | 2 |
| US-6.5 | Disclaimer on signal surfaces | FEAT-6 Signals | Must | 1 |
| US-7.1 | Pluggable provider abstraction | FEAT-7 Market Data | Must | 5 |
| US-7.2 | Caching & rate-limit handling | FEAT-7 Market Data | Must | 3 |
| US-7.3 | Graceful mock fallback | FEAT-7 Market Data | Must | 3 |
| US-7.4 | Config-driven provider & secrets | FEAT-7 Market Data | Must | 2 |
| US-8.1 | Secure auth & secrets | FEAT-8 Platform | Must | 3 |
| US-8.2 | Validation & global error handling | FEAT-8 Platform | Must | 3 |
| US-8.3 | DB provisioning & migrations | FEAT-8 Platform | Must | 5 |
| US-8.4 | CORS & API/SPA integration | FEAT-8 Platform | Must | 2 |
| US-8.5 | Cloud deployment | FEAT-8 Platform | Should | 5 |

**Totals:** 31 stories · **97 story points** · 24 Must / 7 Should.
MoSCoW: *Must* = MVP-critical; *Should* = important but shippable later.

---

## Suggested labels / fields (for Jira / Azure DevOps import)
- **Work item types:** Epic → Feature → User Story (→ Task/Test as needed).
- **Per story:** Priority (Must/Should/Could), Estimate (story points), Acceptance Criteria, Component (Auth, Market Data, Portfolio, Signals, Platform).
- **Definition of Done:** code + unit tests, validation, error handling, disclaimer present (for signal surfaces), deployed to an environment, AC verified.
