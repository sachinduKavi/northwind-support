# Implementation notes

What has been built so far for the assignment in `ASSIGNMENT_CANDIDATE.md`, how it works, the decisions behind it, and what is still open. `README.md` covers how to run the project.

| Task | State |
| --- | --- |
| 1.1 DEFECT-117 | Fixed. Regression test still to write. |
| 1.2 Server-side filtering | Mostly done. `unassignedOnly`, search trimming and tests still to do. |
| 2 Escalation & assignment | Backend and frontend done and checked by hand. Tests still to write. |
| 3 Authentication | Not started. |
| 4 Docker | Not started. |

---

## Task 1.1: DEFECT-117, filters sometimes not taking effect

### Root cause

The effect in `useTicketList` that loads tickets re-ran only when `search`, `page` or `pageSize` changed. A lint suppression hid the missing fields, and its comment ("filters object identity changes every render") was wrong: `filters` is React state, so it only changes when `setFilters` is called. Changing Status updated the state but never triggered a request. The next search request then read the latest state and carried the "stale" status along with it.

### Fix

`apps/web/src/features/tickets/hooks/useTicketList.ts`

- The text typed into the search box is kept separately (`searchText`). It's copied into `filters` by a function from the `debounce` package 300 ms after typing stops, and that also goes back to page 1.
- A single effect depends on the whole `filters` object, so any filter change sends a request straight away. The hand-written field list and the lint suppression are gone.
- A response that arrives after the filters have changed again is ignored.
- `isLoading` is derived by checking which filters the last response was for, rather than set at the start of the effect. The React lint rules don't allow calling `setState` there.
- Task 2's SLA filter needed no extra wiring to work, which is one of the acceptance criteria.

### How to verify

Open the ticket list with the browser's Network tab open:

1. Changing Status sends `GET /api/tickets?...status=InProgress` immediately.
2. Typing in search sends one request once you stop typing, and it still includes the status.
3. Clearing the search and then changing another dropdown sends a request immediately.

---

## Task 1.2: Server-side filtering

`apps/api/src/SupportDesk.Infrastructure/Queries/TicketQueries.cs`, `GetPagedAsync`

- The filters are applied to the query **before** `CountAsync`, `Skip` and `Take`. So `totalCount`, `totalPages` and the pager describe the filtered set, and paging walks through the filtered rows. Previously the `.Where` ran after `Take` and the count ignored the filters, which left pages empty and the totals wrong.
- `status`, `priority`, `categoryId`, `customerId` and `assignedAgentId` are exact matches. `search` matches the title, the reference or the customer name. All filters combine with AND, and everything runs in SQL.
- Adding a filter is one extra `Where`, as the Task 2 SLA filter shows:

  ```csharp
  if (query.SlaStatus is { } slaStatus)
  {
      tickets = tickets.Where(SlaStatusIs(slaStatus, now, threshold));
  }
  ```

---

## Task 2: Ticket escalation & assignment

### Where the rules live

The rules are small classes in `apps/api/src/SupportDesk.Domain/Triage` that don't touch the database. The create and escalate handlers use the same ones through `TicketTriage`.

| Rule | Class |
| --- | --- |
| BR-1 priority (requested, else Medium; Critical if the category forces it) | `PriorityPolicy` |
| BR-2 / BR-3 SLA window (base window × premium multiplier, with the 1 h floor) | `SlaPolicy` with `SlaSettings` |
| BR-4 / BR-5 agent selection (eligibility, fewest open tickets, tie-break, nobody eligible) | `AgentSelector` with `AgentCandidate` |
| BR-7 escalation (one step up, rejections, history row) | `Ticket.Escalate`, `TicketEscalation` |
| BR-8 SLA status | `SlaEvaluator` |
| Loading what the rules need and running them | `Application/Features/Tickets/Triage/TicketTriage` |

Every decision comes with a plain-English reason ("Security tickets are always Critical.", "Unassigned because no active Billing specialist has capacity."). These reasons are returned to the client in the `triage` object.

### Endpoints

| Method | Path | Result |
| --- | --- | --- |
| `POST` | `/api/tickets` | **201** with `{ ticket, triage }`. Still 201, unassigned with a reason, when nobody is eligible. |
| `POST` | `/api/tickets/{id}/escalate` | **200** with `{ ticket, escalation, triage }`. **400** for a reason outside 5–500 characters (after trimming) or a missing `escalatedBy`. **404** for an unknown ticket. **409** for Critical or Resolved/Closed, with a message saying which. |
| `GET` | `/api/tickets/{id}/escalations` | History, newest first. **404** for an unknown ticket. |
| `GET` | `/api/tickets?slaStatus=...` | `WithinSla`, `AtRisk`, `Breached`, `Met` or `NotApplicable`. Filtered in SQL before counting and paging. |

### Configuration (BR-6)

The `Sla` section is bound to `SlaOptions` at startup and turned into `SlaSettings`, which checks itself: every priority needs a positive window, the multiplier must be above 0 and at most 1, the floor must be positive, and the threshold must be between 0 and 100. Bad or missing config stops the API at startup instead of failing on the first request. The section is in `appsettings.Development.json`, as the spec asks, and also in `appsettings.json`, so the API can start outside Development (Task 4's container needs this). No category names appear in code; the rules read the category flags.

### Database

Migration `20261016083139_AddTicketEscalationsAndSlaStart`:

- **`Tickets.SlaStartedAtUtc`** (nullable `datetime2(3)`). The migration fills it with `CreatedAtUtc` for existing tickets that have a due date.
- **`TicketEscalations`**: Id, TicketId, From/To Priority, From/To AgentId, From/To DueAtUtc, Reason (500), EscalatedBy (100), EscalatedAtUtc.
  - Both priority columns have check constraints limiting them to 1–4.
  - All foreign keys use `Restrict`, so history can't be lost by deleting a ticket or an agent.
  - The From/To agent and due-date columns are nullable, because a ticket can be unassigned or have no SLA.
  - Rows can't be changed after they're written: the entity has private setters and no methods that update it.
- **Index `IX_TicketEscalations_TicketId_EscalatedAtUtc`** (TicketId ascending, EscalatedAtUtc descending). History is always read for one ticket, newest first, so this index serves both the filter and the sort without a separate sort step.

### Decisions

- **Storing when the SLA window starts.** "At risk" means 25% or less of the window is left, so the window's start has to be known. After an escalation the window restarts from the escalation time, so `CreatedAtUtc` stops being correct. Storing the start date doesn't break "SLA status is never stored": the status is still worked out on every read.
  *Rejected:* looking up the latest escalation for each ticket. Every list query would need that join, and the SQL filter would be much harder to write.
- **Keeping the current agent on escalation.** The ticket being escalated doesn't count against its current agent's limit, so an agent at exactly their limit can keep it. Counting it would push the ticket away from the agent who already has it, only because they have it.
- **Tie-break.** Fewest open tickets first, then the lowest agent id. That's stable and easy to explain.
- **SLA boundaries.**
  - Exactly at the due time is still AtRisk, not Breached; Breached starts after the due time.
  - Exactly 25% left counts as AtRisk.
  - Resolved exactly at the due time counts as Met.
- **SLA status in C# and in SQL.** The badge value comes from `SlaEvaluator`, which is plain C# and easy to unit test. The `slaStatus` filter has to run in SQL, so `TicketQueries.SlaStatusIs` expresses the same rule with `EF.Functions.DateDiffSecond`. The two use the same boundaries and agreed on every seeded ticket. The one difference: `DATEDIFF` counts second boundaries crossed, so the two could disagree only within the same second.
- **Loading agents.** All agents and their open-ticket counts come back in one query, with the counts worked out in SQL, so there are no per-agent queries. Choosing among them happens in the domain. The agents table is small, so loading all of it is cheap.
- **Escalation reason.** It's checked in three places: the request validator (gives the 400), the domain entity (so the rule can't be bypassed), and the web form (so invalid input never reaches the API). All three use the same 5 and 500 limits from `TicketEscalation`.
- **`escalatedBy`** is a request field for now. Task 3 will take it from the token instead, and nothing in the domain depends on where it comes from.

### Patterns

- **Policy / Strategy:** `PriorityPolicy`, `SlaPolicy` and `AgentSelector` each hold one rule, have no side effects, and can be tested on their own.
- **Options pattern:** the `Sla` config section becomes a validated `SlaSettings` object at startup.
- **Specification:** `SlaStatusIs` builds one filter expression per SLA status for EF Core to translate to SQL.
- **Repository and Unit of Work:** these were already in the codebase. Escalations are saved in the same `SaveChangesAsync` call as the ticket change, so both are written or neither is.

### Frontend (`apps/web/src/features/tickets`)

- **`SlaIndicator`** is the single SLA badge, used in the list (`TicketTable`) and on the ticket page (`TicketSummary`). Each state has a symbol and a text label (○ Within SLA, ! At risk, ✕ Breached, ✓ Met, – No SLA), so colour isn't the only signal. The states are defined in `sla.ts`.
- **SLA filter** in the filter bar, wired through the same `useTicketList` hook as the other filters.
- **`TriageResult`** appears after a ticket is created. It shows the applied priority, due date, SLA status and agent, or why the ticket was left unassigned, each with the reason behind it.
- **`EscalationPanel`** is on the ticket page.
  - Its validation in `escalation.ts` matches the server, so a request with an invalid reason is never sent.
  - It shows a pending state, and any server error, including 409.
  - On success it updates the ticket and the history in place, without a page reload.
  - It's disabled for Critical, Resolved and Closed tickets, and still handles a rejection from the server.
- **`EscalationHistory`** lists each escalation: the priority, agent and due-date changes, the reason, who escalated and when. It uses a `useEscalations` hook built on the existing `useAsyncData`.

### How to verify

Use the seeded data with the API running (`npm run api`):

| New ticket | Expected owner | Expected priority / window |
| --- | --- | --- |
| General, Standard customer (Fabrikam), no priority | Sara Lindqvist (fewest open tickets) | Medium, 24 h |
| Security, Premium customer (Contoso), Low requested | Priya Nair (Marcus is full) | Critical, 2 h |
| Billing, Premium customer | Sara Lindqvist (Marcus is full) | Medium, 12 h |
| Outage | Ben Osei (fewer open tickets than Priya) | Critical |

Then escalate the General ticket:

- It should go from Medium to High, keep Sara, get a new due date 8 h from now, and add a history row.
- Escalating it again should take it to Critical.
- After that, it should be refused with a 409.

Finally, check that `GET /api/tickets?slaStatus=AtRisk` returns only tickets whose badge says "At risk".

---

## Not done yet

- **Tests:**
  - the DEFECT-117 regression test;
  - the Task 1.2 filter tests (exact-match priority, a combination of filters, a search on customer name);
  - the Task 2 tests in section 2.4.

  The existing tests were updated only so they compile with the new signatures. All 30 backend tests and both web tests pass.
- **Task 1.2:** `unassignedOnly=true` is still ignored, and `search` isn't trimmed before matching.
- **Debug leftovers:**
  - `Console.WriteLine` calls in `TicketQueries.cs`;
  - log levels set to `Critical` in `appsettings.json`;
  - EF SQL logging set to `Critical` in `appsettings.Development.json`, which hides the SQL that shows the filters run in the database.
- **`debounce` dependency:** it's declared in the root `package.json` instead of `apps/web/package.json`. It only resolves because of how npm workspaces install packages.

## Known limitations

- Two tickets created at the same moment could both be given to an agent who has only one free slot. Open-ticket counts are read and then acted on without a lock. A serializable transaction or a row-version check on the agent would close this.
- SLA settings are read once at startup, so a change needs a restart.
- Ticket references use the highest id so far plus one. That's safe only while a single API instance creates tickets (this was already the case before this work).
