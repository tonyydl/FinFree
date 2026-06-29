# Copy Previous Month Budgets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a one-click action that copies the previous month's budget setup into an empty target month.

**Architecture:** Implement the business rule in `BudgetService`, expose it through `BudgetsController`, and call it from the existing Pinia budget store. The budget page owns the confirmation dialog and refresh behavior.

**Tech Stack:** ASP.NET Core 9, EF Core 9, PostgreSQL, xUnit, Vue 3, TypeScript, Pinia, Element Plus.

---

### Task 1: Backend Service Tests

**Files:**
- Create: `backend/FinFree.Api.Tests/Services/BudgetServiceTests.cs`

- [ ] Write a test where April budgets copy into May and preserve `CategoryId` and `Amount`.
- [ ] Write a test where copying into a month with existing budgets throws `InvalidOperationException`.
- [ ] Write a test where copying from an empty previous month throws `InvalidOperationException`.
- [ ] Run `dotnet test backend/FinFree.Api.Tests/FinFree.Api.Tests.csproj -p:BaseOutputPath=C:\Users\S2403004\code\FinFree\tmp\test-bin\` and confirm the tests fail because `CopyPreviousMonthAsync` is missing.

### Task 2: Backend Service and API

**Files:**
- Modify: `backend/FinFree.Api/Services/Interfaces/IBudgetService.cs`
- Modify: `backend/FinFree.Api/Services/Implementations/BudgetService.cs`
- Modify: `backend/FinFree.Api/Controllers/BudgetsController.cs`

- [ ] Add `Task<IEnumerable<BudgetResponse>> CopyPreviousMonthAsync(int userId, int year, int month)` to `IBudgetService`.
- [ ] Implement previous-month calculation with January rolling back to December of the previous year.
- [ ] Reject invalid target year/month.
- [ ] Reject target months that already contain budgets.
- [ ] Reject empty previous months.
- [ ] Copy `CategoryId` and `Amount`, save once, and return `GetByMonthAsync`.
- [ ] Add `POST /api/budgets/{year}/{month}/copy-previous`.
- [ ] Run backend tests and confirm they pass.

### Task 3: Frontend Store and Types

**Files:**
- Modify: `frontend/src/stores/budget.ts`

- [ ] Add `copyPreviousMonth(year: number, month: number): Promise<boolean>`.
- [ ] Call `POST /budgets/${year}/${month}/copy-previous`.
- [ ] Replace `budgets.value` with the returned list.
- [ ] Preserve existing error handling pattern.

### Task 4: Budget Page UI

**Files:**
- Modify: `frontend/src/views/BudgetsView.vue`

- [ ] Add a `handleCopyPreviousMonth` function with an Element Plus confirmation dialog.
- [ ] Add a `複製上月` button beside `新增預算`.
- [ ] Add a secondary empty-state button for copying previous month budgets.
- [ ] Show success and error messages using existing `ElMessage` patterns.

### Task 5: Verification

- [ ] Run `dotnet test backend/FinFree.Api.Tests/FinFree.Api.Tests.csproj -p:BaseOutputPath=C:\Users\S2403004\code\FinFree\tmp\test-bin\`.
- [ ] Run `dotnet build backend/FinFree.Api/FinFree.Api.csproj -c Release`.
- [ ] Run `npm run build` from `frontend`.
