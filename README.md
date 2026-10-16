# Northwind Support Desk

A support ticket management system: raise tickets, search and filter them, assign them to agents,
escalate them, and move them through their lifecycle. Agents sign in to use it.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo. The assignment
is in [`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md). More detail on Task 1 and Task 2 is in
[`IMPLEMENTATION.md`](IMPLEMENTATION.md).

---

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`; 20.19+ works), Docker.

```bash
cp .env.example .env          # optional, every value has a working default
npm install                   # installs the web app
npm run db:up                 # SQL Server 2022 in Docker on localhost:1433
npm run api                   # terminal 1: migrates, seeds, then serves the API
npm run web                   # terminal 2: React dev server
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger |
| Health | http://localhost:5043/health |

### Development login

All seeded agents share one password, **`LocalDev-Only-1!`**. It is a local fixture, hashed by the
seeder at startup, and is never used outside a developer machine.

| Agent | Email | Notes |
| --- | --- | --- |
| Sara Lindqvist | `sara.lindqvist@northwind-support.example` | General, Billing |
| Marcus Doyle | `marcus.doyle@northwind-support.example` | Billing, Security; at their ticket limit |
| Alex Turner | `alex.turner@northwind-support.example` | General, Technical |
| Ben Osei | `ben.osei@northwind-support.example` | Technical, Outage |
| Priya Nair | `priya.nair@northwind-support.example` | Outage, Security |
| Yuki Tanaka | `yuki.tanaka@northwind-support.example` | Technical; inactive, cannot sign in |

---

## What was implemented

| Task | State |
| --- | --- |
| 1.1 DEFECT-117 | Fixed at the cause in `useTicketList`. |
| 1.2 Server-side filtering | All filters except `unassignedOnly` run in SQL before counting and paging. |
| 2 Escalation & assignment | Triage on create, escalation with history, SLA status and filter, full UI. |
| 3 Authentication | Email/password login, JWT, every endpoint protected, login UI. |
| 4 Docker | Multi-stage image, compose service on port 5080, `/health`. |
| 2.5 SLA report (stretch) | Not attempted. |

### Rules as understood

- **Priority:** the requested priority, else Medium. Categories flagged "always Critical" (Security,
  Outage) force Critical.
- **SLA window:** base hours per priority (Low 72, Medium 24, High 8, Critical 4), halved for Premium
  customers, never below 1 hour. Read from the `Sla` config section.
- **Assignment:** an active agent who specialises in the category and is under their open-ticket
  limit. Fewest open tickets wins, ties go to the lowest agent id. Nobody eligible means the ticket
  is created unassigned, with a reason.
- **Escalation:** one priority step up, due date recalculated from now, assignee kept if still
  eligible. Critical, Resolved and Closed tickets cannot be escalated (409). Every escalation writes
  a history row.
- **SLA status** is never stored. It is worked out on read: Met, Breached, At risk (25% or less of
  the window left), Within SLA, or Not applicable.

---

## Design decisions

- **Rules as small policy classes in the domain** (`Domain/Triage`): `PriorityPolicy`, `SlaPolicy`,
  `AgentSelector` and `SlaEvaluator`. They have no database access and no side effects. Create and
  escalate both use them through `TicketTriage`, so no rule is written twice.
  *Rejected:* putting the rules in the handlers, which would duplicate them across create and escalate.
- **Storing `SlaStartedAtUtc`.** "At risk" needs the window start, and after an escalation that is no
  longer `CreatedAtUtc`. Only the start is stored; the status is still calculated.
  *Rejected:* joining the latest escalation on every list query.
- **SLA status in C# and in SQL.** The badge uses `SlaEvaluator`, which is easy to unit test. The
  `slaStatus` filter must run in the database, so `TicketQueries.SlaStatusIs` expresses the same
  boundaries as an EF expression.
- **Credentials as a column on `Agent`** (`PasswordHash`), not a separate Users table. Every user is
  an agent and there are no roles, so a second table would only add a join. A Users table would make
  sense once non-agent users or several login methods exist.
- **Generic login failures.** Unknown email, wrong password and inactive agent all return the same
  401 and message. When the email is unknown, a dummy hash is still verified, so the response time
  does not reveal which emails exist.
- **Central authorization.** A fallback policy requires an authenticated user everywhere. Only login,
  Swagger and `/health` opt out with `[AllowAnonymous]` / `AllowAnonymous()`. A new endpoint is
  protected by default.
- **`escalatedBy` from the token.** The request body no longer has the field; the controller passes
  the signed-in agent's name. A client cannot record an escalation under someone else's name.
- **Migrations on startup behind a switch** (`Database:MigrateAndSeedOnStartup`). It is on in
  Development and in docker compose, and off by default.

### Patterns

- **Policy / Strategy:** each triage rule is one class with one job, tested in isolation.
- **Options pattern:** the `Sla` and `Jwt` config sections bind to options objects that validate
  themselves at startup, so bad config stops the API before the first request.
- **Specification:** `SlaStatusIs` builds one EF filter expression per SLA status, translated to SQL.
- **Repository and Unit of Work** were already in the codebase. An escalation and its ticket change
  are saved in one `SaveChangesAsync`, so both are written or neither is.

---

## The defect (DEFECT-117)

**Root cause.** The effect in `apps/web/src/features/tickets/hooks/useTicketList.ts` that loads
tickets only listed `search`, `page` and `pageSize` as dependencies. A lint suppression hid the
missing ones, with a comment claiming the filters object changed identity every render. It does
not: `filters` is React state. So changing Status updated the state but sent no request. The next
search then read the latest state and carried the "stale" status along, which is why filters only
*sometimes* seemed to work.

**Fix.** One effect depends on the whole `filters` object, so any filter change requests straight
away. The search box text is kept separately and copied into the filters with `debounce` 300 ms after
typing stops, which also resets to page 1. Responses for out-of-date filters are ignored. The lint
suppression is gone.

**Verify.** Open the ticket list with the Network tab open. Changing Status sends
`GET /api/tickets?...status=...` immediately; typing sends one request after you stop, still
including the status.

## Filtering

- **Where:** `apps/api/src/SupportDesk.Infrastructure/Queries/TicketQueries.cs`, `GetPagedAsync`. All
  filters are applied to the `IQueryable` before `CountAsync`, `Skip` and `Take`, so `totalCount` and
  the pager describe the filtered set.
- **Behaviour:** `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId` and `slaStatus`
  are exact matches. `search` matches title, reference or customer name. Filters combine with AND.
- **How the SQL was checked:** with EF command logging at Information, the generated `SELECT COUNT`
  and paged `SELECT` both contain the `WHERE` clauses, and paging is `OFFSET ... FETCH` after them.
- **Adding the next filter:** one more `Where` on the query before the count, as `slaStatus` shows:

  ```csharp
  if (query.SlaStatus is { } slaStatus)
  {
      tickets = tickets.Where(SlaStatusIs(slaStatus, now, threshold));
  }
  ```

---

## Authentication

| Item | Choice |
| --- | --- |
| Credential storage | `Agents.PasswordHash` column, nvarchar(256), migration `AddAgentPasswordHash` |
| Hashing | ASP.NET Core Identity `PasswordHasher` (PBKDF2 with HMAC-SHA512, random salt, iteration count stored in the hash) |
| Token | JWT signed with HMAC-SHA256, issued by `POST /api/auth/login` with its expiry and the agent's name and email |
| Lifetime | **8 hours** (`Jwt:LifetimeMinutes = 480`), roughly one working shift, no refresh tokens |
| Signing key | `Jwt__SigningKey` from configuration or environment (see `.env.example`). Development has a throwaway key in `appsettings.Development.json`; `appsettings.json` is empty and the API refuses to start with a key under 32 bytes |
| Current user | `GET /api/auth/me` returns the agent or 401 |
| Web storage | `sessionStorage` (`northwind-support.session`). It survives a refresh and is cleared when the tab closes. **Trade-off:** any script running on the page (an XSS bug) could read it. An httpOnly cookie would hide it from scripts but needs CSRF protection |
| Web behaviour | `client.ts` attaches the token to every call. Protected routes send visitors to `/login` and back to the page they wanted. Any 401 clears the session and goes to login. The header shows the agent's name and a log out button |

**Changes for production:** keep the signing key in a secret store and rotate it (or switch to
asymmetric keys), use HTTPS only, shorten the lifetime and add refresh tokens, add lockout or rate
limiting on login, and consider httpOnly cookies instead of `sessionStorage`.

---

## Docker

```bash
docker compose up --build     # SQL Server + API, migrated and seeded
```

The API is then at **http://localhost:5080** (`/health`, `/api/...`). To use the web app against it:

```bash
VITE_API_PROXY_TARGET=http://localhost:5080 npm run web
```

On Windows `cmd`: `set "VITE_API_PROXY_TARGET=http://localhost:5080" && npm run web`.

- **Image:** `apps/api/Dockerfile`, built from the repo root. The SDK stage copies `global.json`,
  `Directory.Build.props`, `Directory.Packages.props` and the four `.csproj` files, restores, then
  copies the source and publishes. The runtime stage is `mcr.microsoft.com/dotnet/aspnet:10.0`,
  runs as the image's non-root `app` user on HTTP port 8080, and holds no certificates.
- **Nothing baked in:** `.dockerignore` excludes `bin/`, `obj/`, `node_modules/`, `.git/`, `.env`,
  the web app, the tests and `appsettings.Development.json`.
- **Environment variables** (compose reads them from `.env`, all have local defaults):

  | Variable | Purpose |
  | --- | --- |
  | `MSSQL_SA_PASSWORD` | SA password; also used to build the API's connection string to `sqlserver` |
  | `API_ENVIRONMENT` | `ASPNETCORE_ENVIRONMENT` in the container, default `Production` (no Swagger) |
  | `API_JWT_SIGNING_KEY` | `Jwt__SigningKey` in the container |
  | `API_MIGRATE_AND_SEED_ON_STARTUP` | Apply migrations and seed demo data on start, default `true` |

- **Migrations in a real deployment.** Migrating on startup is fine for a demo but not for
  production: several instances would race for the schema, the app would need DDL rights, and a
  failed migration would take the app down with it. Instead, generate an idempotent script
  (`dotnet ef migrations script --idempotent`) or a migration bundle, review it, and run it as a
  separate pipeline step with a privileged account before the new version is deployed. The app then
  runs with a least-privilege account and `API_MIGRATE_AND_SEED_ON_STARTUP=false`. Demo data would
  not be seeded at all.

### How it was checked

| Check | Result |
| --- | --- |
| `docker compose down -v && docker compose up --build` | Migrations applied on an empty volume, `/health` returned `Healthy`, `/api/tickets` returned 401 without a token and 40 seeded tickets after login |
| Change one `.cs` file and rebuild | The `dotnet restore` layer was `CACHED`; only publish re-ran (about 11 s) |
| Image size | `supportdesk-api` **455 MB** vs `dotnet/sdk:10.0` **1.3 GB** (125 MB vs 351 MB compressed) |
| `docker compose exec api whoami` | `app` (uid 1654), not root |
| `VITE_API_PROXY_TARGET=http://localhost:5080 npm run web` | Login and ticket list worked through the proxy |

`docker compose down -v` also deletes the local development database volume. It is re-seeded on the
next start.

---

## Database

| Change | Migration |
| --- | --- |
| `Tickets.SlaStartedAtUtc` (nullable), backfilled from `CreatedAtUtc` | `AddTicketEscalationsAndSlaStart` |
| `TicketEscalations` table: from/to priority, agent and due date, reason, who, when. Check constraints on priority, `Restrict` foreign keys so history is never cascaded away | `AddTicketEscalationsAndSlaStart` |
| `Agents.PasswordHash` | `AddAgentPasswordHash` |

**Index:** `IX_TicketEscalations_TicketId_EscalatedAtUtc` (TicketId, EscalatedAtUtc descending).
History is always read for one ticket, newest first, so this one index serves both the filter and
the sort without a separate sort step. The ticket filters reuse the existing indexes on status and
priority, customer, category, assigned agent and due date.

Reads use `AsNoTracking` and project straight to DTOs. Agent open-ticket counts for triage come back
in one grouped query, not one query per agent.

---

## Testing

```bash
npm test                      # dotnet test, then the web tests (Vitest)
```

Neither needs a database. Currently 30 backend tests (unit and architecture) and 2 web tests pass.
The architecture tests check the layer rules and that the migrations match the EF model.

**Checked by hand rather than automated:** the DEFECT-117 behaviour in the browser, every triage
scenario in [`IMPLEMENTATION.md`](IMPLEMENTATION.md#how-to-verify-1) against the seeded data, and
the auth rules against the running API (401 without a token, identical 401 for wrong email, wrong
password and inactive agent, 400 for empty fields, tampered token rejected, escalation recorded under
the signed-in agent even when the body tried to spoof it).

**Not yet written:** the DEFECT-117 regression test, the Task 1.2 filter tests, the Task 2.4 rule and
UI tests, and the Task 3 behavioural tests. These are the main gap in this submission.

---

## AI usage

- **Tools:** Claude Code (CLI) for investigation, implementation and verification, with me reviewing
  every change and doing all the commits myself.
- **A workflow that worked:** asking for the investigation first with "find it, don't fix it", so I
  understood DEFECT-117 before any code changed, then asking for a plan for each task before
  implementing it.
- **Accepted as-is:** the triage policy classes and the Dockerfile layer structure, after reading
  them and checking them against the spec.
- **Changed or rejected:** I rejected generated code comments and an unrequested temporary test
  file; I asked for the search debounce to use the `debounce` package instead of a hand-written
  timer; I kept my own backend `.Where` filtering rather than a rewrite.
- **Bugs found or introduced:** the AI helped trace the paging bug (filters applied after `Take`, so
  pages were empty and counts wrong) and the stale-closure cause of DEFECT-117. Its first auth
  version could loop between `/login` and a protected page when a stored session had expired; that
  was caught and fixed by only treating unexpired sessions as signed in. A package downgrade error
  (NU1109) between JWT packages was fixed by aligning versions.
- **Verification:** builds, the existing test suites, lint and type checks after each step, plus
  manual calls with `curl` against the local API and the container for every auth and Docker
  requirement.

---

## Known limitations

- `unassignedOnly=true` is accepted but not applied, and `search` is not trimmed.
- Debug `Console.WriteLine` calls are still in `TicketQueries.cs`. EF SQL logging is set to
  `Critical` in `appsettings.Development.json`, which hides the generated SQL.
- `debounce` is declared in the root `package.json`, not `apps/web/package.json`.
- `db/seed.sql` does not set passwords; use the API's seeder for a working login.
- Two tickets created at the same moment could both go to an agent with one free slot (counts are
  read without a lock).
- SLA settings are read once at startup.
- Ticket references use max id + 1, safe only with one API instance.
- Auth: no refresh tokens, lockout, rate limiting, password reset, registration, roles, MFA or SSO
  (all out of scope). A token stays valid until it expires even after logout or deactivation.
- Docker: HTTP only, no container health check on the `api` service, and compose falls back to a
  throwaway local signing key when none is set.
- The SLA summary report (2.5) was not attempted.

---

## Commands

| Command | What it does |
| --- | --- |
| `npm run db:up` / `db:down` / `db:reset` | Start / stop / wipe SQL Server |
| `npm run api` / `npm run web` | Run the API / the React app |
| `npm run build` | Build both |
| `npm run lint` / `typecheck` / `format` | Web lint and types / `dotnet format` |
| `npm run ef -- migrations add <Name>` | Add an EF Core migration |

## API at a glance

All endpoints except login, Swagger and `/health` need `Authorization: Bearer <token>`.

| Endpoint | Description |
| --- | --- |
| `POST /api/auth/login` · `GET /api/auth/me` | Sign in / current agent |
| `GET /api/tickets` | Paged list with `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `slaStatus`, `sortBy`, `sortDirection` |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket; returns the ticket and its triage result |
| `POST /api/tickets/{id}/escalate` · `GET /api/tickets/{id}/escalations` | Escalate / escalation history |
| `PATCH /api/tickets/{id}/status` · `/assignment` | Change status / assignee |
| `GET /api/customers` · `/api/agents` · `/api/categories` | Reference data |
| `GET /health` | Database reachability |

Errors are `ProblemDetails`: `400` validation, `401` not signed in, `404` not found, `409` conflict
or business rule.

**Seed data:** 5 categories, 6 customers (3 Premium), 6 agents (one inactive, one at their limit)
and 40 tickets across every status, priority and SLA state.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API cannot reach the database | `npm run db:up`, wait ~20 s, check `docker compose ps` |
| Port 1433 already in use | Change the port in `docker-compose.yml` and `.env` |
| Browser warns about the certificate | `dotnet dev-certs https --trust`, or use the web app, which proxies |
| Web app shows "Unable to load tickets" | The API is not running, or `VITE_API_PROXY_TARGET` points at the wrong port |
| Every call returns 401 | Sign in again; the token lasts 8 hours and is cleared when the tab closes |
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |
