# Date Range Reports Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add custom date-range reporting while preserving the existing monthly report workflow.

**Architecture:** Add a backend range-report DTO and service method that reuse the existing report aggregation shape. Update `ReportsView.vue` to select between monthly and range modes and normalize report data for shared charts, summary cards, and CSV export.

**Tech Stack:** ASP.NET Core 9, EF Core 9, xUnit, Vue 3, TypeScript, Element Plus, Chart.js.

---

### Task 1: Backend Range Report Tests

**Files:**
- Create: `backend/FinFree.Api.Tests/Services/TransactionServiceTests.cs`

- [ ] Write a failing service test for `GetRangeReportAsync` that includes transactions on both start and end dates.
- [ ] Include one transaction before the range and one after the range and assert they are excluded.
- [ ] Assert total income, total expense, balance, transaction count, daily row count, and expense category percentage.
- [ ] Run `dotnet test backend/FinFree.Api.Tests/FinFree.Api.Tests.csproj -p:BaseOutputPath=C:\Users\S2403004\code\FinFree\tmp\test-bin\` and confirm failure because `GetRangeReportAsync` is missing.

### Task 2: Backend DTO, Service, and API

**Files:**
- Create: `backend/FinFree.Api/DTOs/Responses/RangeReportResponse.cs`
- Modify: `backend/FinFree.Api/Services/Interfaces/ITransactionService.cs`
- Modify: `backend/FinFree.Api/Services/Implementations/TransactionService.cs`
- Modify: `backend/FinFree.Api/Controllers/TransactionsController.cs`

- [ ] Add `RangeReportResponse`.
- [ ] Add `Task<RangeReportResponse> GetRangeReportAsync(DateTime startDate, DateTime endDate, int userId)`.
- [ ] Implement inclusive end-date filtering by using `< endDate.Date.AddDays(1)`.
- [ ] Aggregate income, expense, balance, transaction count, category breakdowns, and daily trends.
- [ ] Add `GET /api/transactions/report/range`.
- [ ] Return `400` when `startDate > endDate` or years are outside `2000-2100`.
- [ ] Run backend tests and confirm they pass.

### Task 3: Frontend Types

**Files:**
- Modify: `frontend/src/types/index.ts`

- [ ] Add `RangeReportResponse`.
- [ ] Keep `MonthlyReportResponse` unchanged.

### Task 4: Reports Page UI

**Files:**
- Modify: `frontend/src/views/ReportsView.vue`

- [ ] Add report mode state: `month` or `range`.
- [ ] Add a segmented control for modes.
- [ ] Add range date picker for range mode.
- [ ] Fetch monthly or range endpoint based on mode.
- [ ] Normalize report data into a shared computed object for cards, charts, category lists, empty state, and CSV export.
- [ ] Update CSV export title and filename per mode.

### Task 5: Verification

- [ ] Run `dotnet test backend/FinFree.Api.Tests/FinFree.Api.Tests.csproj -p:BaseOutputPath=C:\Users\S2403004\code\FinFree\tmp\test-bin\`.
- [ ] Run `dotnet build backend/FinFree.Api/FinFree.Api.csproj -c Release`.
- [ ] Run `npm run build` from `frontend`.
