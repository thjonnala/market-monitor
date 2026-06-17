// Creates the Market Monitor Epic -> Features -> User Stories in Azure DevOps.
// Usage:
//   ADO_ORG=thjonnala ADO_PROJECT=pocs ADO_PAT=<token> node scripts/create-azure-devops-workitems.mjs
//
// The PAT needs scope: Work Items (Read & Write). Nothing is committed; the token
// is read from the environment only.

const ORG = process.env.ADO_ORG || 'thjonnala';
const PROJECT = process.env.ADO_PROJECT || 'pocs';
const PAT = process.env.ADO_PAT;
const API = '7.0';

if (!PAT) {
  console.error('Missing ADO_PAT environment variable.');
  process.exit(1);
}

const auth = 'Basic ' + Buffer.from(':' + PAT).toString('base64');
const base = `https://dev.azure.com/${ORG}/${encodeURIComponent(PROJECT)}`;

// Priority: Azure uses 1 (highest) .. 4. Map MoSCoW.
const prio = (m) => (m === 'Must' ? 1 : m === 'Should' ? 2 : 3);

// ---- Backlog data -----------------------------------------------------------

const epic = {
  title: 'Market Monitor: Virtual Stock Tracking & Signals Platform',
  description:
    'Educational web app to discover trending stocks, track them, practice investing with ' +
    'virtual money, and understand explainable BUY/SELL/HOLD signals. No real money or trades. ' +
    'Not financial advice.',
  priority: 1,
};

const features = [
  { key: 'F1', title: 'User Authentication & Account Management', moscow: 'Must',
    description: 'Secure register, login, logout, and protected access with JWT.' },
  { key: 'F2', title: 'Public Home & "Top Shares to Buy" Discovery', moscow: 'Must',
    description: 'Public, signal-ranked list of curated stocks with data-source transparency.' },
  { key: 'F3', title: 'Personal Watchlist', moscow: 'Must',
    description: 'Per-user add/view/remove of tracked symbols with live prices.' },
  { key: 'F4', title: 'Virtual Portfolio & Paper Trading', moscow: 'Must',
    description: 'Virtual cash, buy/sell, cost basis, holdings, and unrealized P/L.' },
  { key: 'F5', title: 'Stock Detail & Price Charts', moscow: 'Must',
    description: 'Per-symbol quote, selectable-range chart, and suggestion panel.' },
  { key: 'F6', title: 'Suggestions / Signals Engine', moscow: 'Must',
    description: 'Explainable BUY/SELL/HOLD with confidence, rationale, and modular rules.' },
  { key: 'F7', title: 'Market-Data Integration & Resilience', moscow: 'Must',
    description: 'Pluggable providers, caching, rate-limit backoff, and mock fallback.' },
  { key: 'F8', title: 'Platform, Security, Compliance & Deployment', moscow: 'Must',
    description: 'Secure auth/secrets, validation, migrations, CORS, and cloud deployment.' },
];

const stories = [
  // FEAT-1
  { f: 'F1', id: 'US-1.1', title: 'Register an account', moscow: 'Must', points: 3,
    asA: 'new visitor', want: 'to register with email, display name, and password',
    soThat: 'I can save a watchlist and portfolio',
    ac: ['Valid input creates an account and logs me in (JWT issued)', 'Passwords are stored hashed, never plaintext',
         'Duplicate email returns HTTP 409 with a clear message', 'Invalid input shows field-level validation'] },
  { f: 'F1', id: 'US-1.2', title: 'Log in', moscow: 'Must', points: 3,
    asA: 'registered user', want: 'to log in with email and password', soThat: 'I can access my data',
    ac: ['Valid credentials return a signed JWT with expiry and display name',
         'Invalid email or password returns a generic error (no user enumeration)',
         'A demo-account shortcut exists in non-production'] },
  { f: 'F1', id: 'US-1.3', title: 'Log out', moscow: 'Must', points: 1,
    asA: 'logged-in user', want: 'to log out', soThat: 'my session token is cleared from the device',
    ac: ['Logging out removes the stored token and returns to the public experience',
         'Protected routes become inaccessible after logout'] },
  { f: 'F1', id: 'US-1.4', title: 'Protected access & session expiry', moscow: 'Must', points: 3,
    asA: 'user', want: 'protected pages and APIs to require authentication', soThat: 'my data stays private',
    ac: ['Frontend /watchlist and /portfolio redirect unauthenticated users to login',
         'Protected API endpoints return 401 without a valid token',
         'Expired/invalid tokens are rejected and log the user out client-side'] },
  // FEAT-2
  { f: 'F2', id: 'US-2.1', title: 'View Top Shares (public)', moscow: 'Must', points: 5,
    asA: 'visitor', want: 'to see a "Top Shares to Buy" list', soThat: 'I can discover strong-signal stocks',
    ac: ['Loads without authentication', 'Each card shows symbol, name, price, % change, BUY/SELL/HOLD badge, confidence, rationale',
         'List ranked BUY (by confidence) > HOLD > SELL', '"Not financial advice" disclaimer is visible'] },
  { f: 'F2', id: 'US-2.2', title: 'Data-source transparency', moscow: 'Should', points: 2,
    asA: 'visitor', want: 'to know whether a card uses live or simulated data', soThat: 'I can trust it appropriately',
    ac: ['Each card shows data state: live, live price · sim. signal, or mock price',
         'Flag accurately reflects live vs mock for quote and signal separately'] },
  { f: 'F2', id: 'US-2.3', title: 'Navigate to stock detail', moscow: 'Must', points: 1,
    asA: 'visitor', want: 'to click a Top Share to open its detail page', soThat: 'I can learn more',
    ac: ['Clicking a card navigates to /stocks/{symbol} and loads that symbol'] },
  // FEAT-3
  { f: 'F3', id: 'US-3.1', title: 'Add a symbol to watchlist', moscow: 'Must', points: 3,
    asA: 'logged-in user', want: 'to add a symbol', soThat: 'I can monitor it',
    ac: ['Valid symbol persists to my watchlist with current price/% change',
         'Duplicate add returns a clear "already on your watchlist" message', 'Unknown symbol is rejected'] },
  { f: 'F3', id: 'US-3.2', title: 'View my watchlist', moscow: 'Must', points: 3,
    asA: 'logged-in user', want: 'to see my watchlist with live prices', soThat: 'I can monitor at a glance',
    ac: ['Shows symbol, name, price, % change, newest first', 'Each row links to stock detail', 'Helpful empty state'] },
  { f: 'F3', id: 'US-3.3', title: 'Remove a symbol', moscow: 'Must', points: 2,
    asA: 'logged-in user', want: 'to remove a symbol', soThat: 'my list stays relevant',
    ac: ['Removing deletes from watchlist and updates UI immediately', 'Removing a non-existent item returns 404'] },
  // FEAT-4
  { f: 'F4', id: 'US-4.1', title: 'Auto-create a virtual portfolio', moscow: 'Must', points: 2,
    asA: 'logged-in user', want: 'a portfolio with starting virtual cash', soThat: 'I can begin paper trading',
    ac: ['On first visit a portfolio is created with fixed starting virtual cash (e.g., $100,000)', 'Portfolio is private to me'] },
  { f: 'F4', id: 'US-4.2', title: 'Buy shares with virtual cash', moscow: 'Must', points: 5,
    asA: 'logged-in user', want: 'to buy N shares of a symbol', soThat: 'I can build a position',
    ac: ['Buy executes at current price; cash debited by price × qty', 'Buy beyond cash is rejected (insufficient funds)',
         'Adding to a holding updates weighted-average cost basis', 'Quantity must be positive', 'Trade recorded for audit'] },
  { f: 'F4', id: 'US-4.3', title: 'Sell shares', moscow: 'Must', points: 3,
    asA: 'logged-in user', want: 'to sell shares I hold', soThat: 'I can realize gains/losses',
    ac: ['Sell credits cash by price × qty and reduces position', 'Selling more than held is rejected',
         'A holding at zero quantity is removed'] },
  { f: 'F4', id: 'US-4.4', title: 'View holdings, value & P/L', moscow: 'Must', points: 5,
    asA: 'logged-in user', want: 'to see holdings with current value and unrealized P/L', soThat: 'I can judge performance',
    ac: ['Per holding: qty, avg cost, current price, market value, unrealized P/L ($ and %)',
         'Summary: total value, cash, holdings value, total return ($ and %)', 'Play-money disclaimer shown'] },
  // FEAT-5
  { f: 'F5', id: 'US-5.1', title: 'View live quote', moscow: 'Must', points: 2,
    asA: 'user', want: "to see a symbol's live quote and day stats", soThat: 'I understand its current state',
    ac: ['Shows price, absolute & % change, open, high, low, previous close, and freshness/mock flag'] },
  { f: 'F5', id: 'US-5.2', title: 'Price chart with selectable ranges', moscow: 'Must', points: 5,
    asA: 'user', want: 'a price chart with range options', soThat: 'I can see the trend',
    ac: ['Ranges 1D/1W/1M/3M/1Y; chart reloads on change', 'Renders closing prices; friendly empty state',
         'Shows a "simulated history" note when candles are mock'] },
  { f: 'F5', id: 'US-5.3', title: 'Add to watchlist from detail', moscow: 'Should', points: 2,
    asA: 'logged-in user', want: 'to add the symbol to my watchlist from the detail page', soThat: 'I can track it without leaving',
    ac: ['Authenticated users see an "Add to watchlist" action that confirms success/failure inline'] },
  // FEAT-6
  { f: 'F6', id: 'US-6.1', title: 'Generate a recommendation', moscow: 'Must', points: 5,
    asA: 'user', want: 'a BUY/SELL/HOLD recommendation for a symbol', soThat: 'I get a quick read',
    ac: ['Returns recommendation, confidence (0–100%), and a one-line summary',
         'Derived by aggregating multiple independent signal rules'] },
  { f: 'F6', id: 'US-6.2', title: 'See the rationale (explainability)', moscow: 'Must', points: 3,
    asA: 'user', want: 'to see why a recommendation was made', soThat: 'I can learn and trust it',
    ac: ['Panel lists each signal (SMA crossover, RSI, % of range) with lean, weight, and plain-English rationale',
         'Headline rationale matches the final recommendation direction'] },
  { f: 'F6', id: 'US-6.3', title: 'Modular, extensible rules', moscow: 'Should', points: 5,
    asA: 'developer/PO', want: 'to add/modify signal rules without touching the engine', soThat: 'the logic can evolve',
    ac: ['New rules implement a common interface and register into the rule set',
         'Adding/removing a rule changes output without modifying aggregation', 'Rules and indicators are unit-tested'] },
  { f: 'F6', id: 'US-6.4', title: 'Insufficient-data handling', moscow: 'Should', points: 2,
    asA: 'user', want: 'a sensible result when data is thin', soThat: "I'm not misled",
    ac: ['With insufficient history the engine returns HOLD with 0 confidence and an explanatory message'] },
  { f: 'F6', id: 'US-6.5', title: 'Disclaimer on signal surfaces', moscow: 'Must', points: 1,
    asA: 'compliance owner', want: 'a "not financial advice" disclaimer wherever signals appear', soThat: 'the product is responsible',
    ac: ['Disclaimer visible on Home, Stock Detail, and Portfolio signal surfaces'] },
  // FEAT-7
  { f: 'F7', id: 'US-7.1', title: 'Pluggable provider abstraction', moscow: 'Must', points: 5,
    asA: 'developer', want: 'market data behind a single interface', soThat: 'providers can be swapped without business-logic changes',
    ac: ['Quote, batch-quote, and candle access go through one interface', 'Providers (Finnhub, Twelve Data, Mock) are interchangeable via config'] },
  { f: 'F7', id: 'US-7.2', title: 'Caching & rate-limit handling', moscow: 'Must', points: 3,
    asA: 'operator', want: 'caching and backoff', soThat: 'we stay within free-tier limits',
    ac: ['Quotes and candles cached with sensible TTLs', 'On HTTP 429 the provider backs off, serves cached/mock, then self-heals'] },
  { f: 'F7', id: 'US-7.3', title: 'Graceful mock fallback', moscow: 'Must', points: 3,
    asA: 'user', want: 'the app to keep working when live data is unavailable', soThat: "I'm never blocked",
    ac: ['No key or unreachable provider serves deterministic mock data', 'Mock vs live state surfaced honestly (price and signal separately)'] },
  { f: 'F7', id: 'US-7.4', title: 'Config-driven provider & secrets', moscow: 'Must', points: 2,
    asA: 'operator', want: 'to choose the provider and supply keys via config/secrets', soThat: 'no keys are hard-coded',
    ac: ['Provider and API key come from configuration (user-secrets/env), never committed', 'Switching providers needs no code change'] },
  // FEAT-8
  { f: 'F8', id: 'US-8.1', title: 'Secure auth & secrets', moscow: 'Must', points: 3,
    asA: 'security owner', want: 'hashed passwords, signed JWTs, and externalized secrets', soThat: 'the app is safe',
    ac: ['Passwords hashed via Identity', 'JWT signing key and DB/API secrets come from secrets/env, not git'] },
  { f: 'F8', id: 'US-8.2', title: 'Validation & global error handling', moscow: 'Must', points: 3,
    asA: 'user', want: 'clear errors and stable behavior', soThat: 'failures are graceful',
    ac: ['Inputs validated; business errors return appropriate 4xx', 'Unexpected errors return a safe 500 (details only in non-prod)'] },
  { f: 'F8', id: 'US-8.3', title: 'DB provisioning & migrations', moscow: 'Must', points: 5,
    asA: 'operator', want: 'code-first migrations and seed data', soThat: 'the DB is reproducible',
    ac: ['Same EF model targets local SQL Express and Azure SQL via config', 'Migrations apply and sample symbols + demo user seed on startup'] },
  { f: 'F8', id: 'US-8.4', title: 'CORS & API/SPA integration', moscow: 'Must', points: 2,
    asA: 'developer', want: 'the SPA to call the API across origins', soThat: 'frontend and backend integrate cleanly',
    ac: ['CORS allows configured frontend origin(s)', 'SPA reads API base URL from configuration per environment'] },
  { f: 'F8', id: 'US-8.5', title: 'Cloud deployment', moscow: 'Should', points: 5,
    asA: 'operator', want: 'the API on Azure App Service (+ Azure SQL) and the SPA on Vercel', soThat: 'the app is publicly hosted',
    ac: ['API deploys to Azure App Service, connects to Azure SQL; health returns 200', 'Frontend builds on Vercel (Vite) targeting the deployed API'] },
];

// ---- API helpers ------------------------------------------------------------

async function req(method, url, body, contentType = 'application/json') {
  const res = await fetch(url, {
    method,
    headers: { Authorization: auth, 'Content-Type': contentType, Accept: 'application/json' },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  if (!res.ok) {
    throw new Error(`${method} ${url} -> ${res.status}\n${text.slice(0, 500)}`);
  }
  return text ? JSON.parse(text) : {};
}

function acHtml(ac) {
  return '<ul>' + ac.map((x) => `<li>${escapeHtml(x)}</li>`).join('') + '</ul>';
}
function escapeHtml(s) {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

async function createWorkItem(type, fields, parentId) {
  const ops = Object.entries(fields).map(([path, value]) => ({ op: 'add', path: `/fields/${path}`, value }));
  if (parentId) {
    ops.push({
      op: 'add', path: '/relations/-',
      value: { rel: 'System.LinkTypes.Hierarchy-Reverse', url: `https://dev.azure.com/${ORG}/_apis/wit/workItems/${parentId}` },
    });
  }
  const url = `${base}/_apis/wit/workitems/$${encodeURIComponent(type)}?api-version=${API}`;
  const wi = await req('POST', url, ops, 'application/json-patch+json');
  return wi.id;
}

// ---- Main -------------------------------------------------------------------

async function main() {
  // Detect the project's process and map our 3 levels to available types.
  const types = await req('GET', `${base}/_apis/wit/workitemtypes?api-version=${API}`);
  const names = new Set(types.value.map((t) => t.name));

  const epicType = names.has('Epic') ? 'Epic' : null;
  const featureType = names.has('Feature') ? 'Feature' : names.has('Issue') ? 'Issue' : null;
  const storyType = names.has('User Story') ? 'User Story'
    : names.has('Product Backlog Item') ? 'Product Backlog Item'
    : names.has('Task') ? 'Task' : null;

  if (!epicType || !featureType || !storyType || featureType === storyType) {
    throw new Error(`Cannot map 3-level hierarchy. Available types: ${[...names].join(', ')}`);
  }

  // Rich processes (Agile/Scrum) have Story Points + Acceptance Criteria fields;
  // Basic (Task/Issue) does not, so we fold that info into the description/tags.
  const rich = storyType === 'User Story' || storyType === 'Product Backlog Item';
  console.log(`Process map -> Epic: ${epicType}, Feature: ${featureType}, Story: ${storyType} ` +
    `(${rich ? 'rich fields' : 'Basic process — fields folded into description'})`);

  // Epic
  const epicFields = {
    'System.Title': epic.title,
    'System.Description': `<p>${escapeHtml(epic.description)}</p>`,
    'System.Tags': 'MarketMonitor',
  };
  if (rich) epicFields['Microsoft.VSTS.Common.Priority'] = epic.priority;
  const epicId = await createWorkItem(epicType, epicFields);
  console.log(`Epic #${epicId}  ${epic.title}`);

  // Features
  const featureIds = {};
  for (const f of features) {
    const ff = {
      'System.Title': rich ? f.title : `[Feature] ${f.title}`,
      'System.Description': `<p>${escapeHtml(f.description)}</p>`,
      'System.Tags': `MarketMonitor; MoSCoW: ${f.moscow}`,
    };
    if (rich) ff['Microsoft.VSTS.Common.Priority'] = prio(f.moscow);
    const id = await createWorkItem(featureType, ff, epicId);
    featureIds[f.key] = id;
    console.log(`  ${featureType} #${id}  ${f.title}`);
  }

  // Stories
  let count = 0;
  for (const s of stories) {
    const story = `<p><b>As a</b> ${escapeHtml(s.asA)}, <b>I want</b> ${escapeHtml(s.want)}, ` +
      `<b>so that</b> ${escapeHtml(s.soThat)}.</p>`;
    const sf = {
      'System.Title': `${s.id} ${s.title}`,
      'System.Tags': `MarketMonitor; MoSCoW: ${s.moscow}; Points: ${s.points}`,
    };
    if (rich) {
      sf['System.Description'] = story;
      sf['Microsoft.VSTS.Common.AcceptanceCriteria'] = acHtml(s.ac);
      sf['Microsoft.VSTS.Scheduling.StoryPoints'] = s.points;
      sf['Microsoft.VSTS.Common.Priority'] = prio(s.moscow);
    } else {
      // Basic: fold story + AC + estimate into the description.
      sf['System.Description'] = story +
        `<p><b>Acceptance criteria:</b></p>${acHtml(s.ac)}` +
        `<p><b>Priority:</b> ${s.moscow} &nbsp; <b>Story points:</b> ${s.points}</p>`;
    }
    const id = await createWorkItem(storyType, sf, featureIds[s.f]);
    count++;
    console.log(`    ${storyType} #${id}  ${s.id} ${s.title}  [${s.moscow}, ${s.points}pts]`);
  }

  console.log(`\nDone: 1 ${epicType}, ${features.length} ${featureType}s, ${count} ${storyType}s ` +
    `created in ${ORG}/${PROJECT}.`);
}

main().catch((e) => { console.error('\nFAILED:', e.message); process.exit(1); });
