# Account Initial Balance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add editable account initial balances and include them in all account balance calculations.

**Architecture:** Store `InitialBalance` on `Account`, expose it through account DTOs, and fold it into `AccountService` balance calculations. The frontend account dialog owns editing the value while existing account summaries continue to use computed `balance`.

**Tech Stack:** ASP.NET Core 9, EF Core 9, PostgreSQL, xUnit, Vue 3, TypeScript, Element Plus.

---

### Task 1: Backend Test Coverage

**Files:**
- Create: `backend/FinFree.Api.Tests/FinFree.Api.Tests.csproj`
- Create: `backend/FinFree.Api.Tests/Services/AccountServiceTests.cs`
- Modify: `backend/FinFree.Api/FinFree.Api.sln`

- [ ] Create an xUnit test project referencing `FinFree.Api` and EF Core InMemory.
- [ ] Write tests proving balances include initial balance and transfer validation uses computed balances.
- [ ] Run `dotnet test backend/FinFree.Api.Tests/FinFree.Api.Tests.csproj` and confirm tests fail because `InitialBalance` does not exist yet.

### Task 2: Backend Implementation

**Files:**
- Modify: `backend/FinFree.Api/Models/Account.cs`
- Modify: `backend/FinFree.Api/Data/AppDbContext.cs`
- Modify: `backend/FinFree.Api/DTOs/Requests/CreateAccountRequest.cs`
- Modify: `backend/FinFree.Api/DTOs/Requests/UpdateAccountRequest.cs`
- Modify: `backend/FinFree.Api/DTOs/Responses/AccountResponse.cs`
- Modify: `backend/FinFree.Api/Services/Implementations/AccountService.cs`
- Create: EF Core migration under `backend/FinFree.Api/Migrations`

- [ ] Add `InitialBalance` to model and DTOs.
- [ ] Configure decimal precision.
- [ ] Add initial balance to list, detail, create, update, and transfer balance calculations.
- [ ] Add EF migration with default value `0`.
- [ ] Run backend tests and confirm they pass.

### Task 3: Frontend Implementation

**Files:**
- Modify: `frontend/src/types/index.ts`
- Modify: `frontend/src/views/AccountsView.vue`

- [ ] Add `initialBalance` to account request and response types.
- [ ] Add initial balance field to account form state.
- [ ] Populate the field while editing.
- [ ] Send the field on create and update.
- [ ] Run frontend build.

### Task 4: Verification

- [ ] Run `dotnet test backend/FinFree.Api.Tests/FinFree.Api.Tests.csproj`.
- [ ] Run `dotnet build backend/FinFree.Api/FinFree.Api.csproj`.
- [ ] Run `npm run build` from `frontend`.
