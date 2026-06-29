# Copy Previous Month Budgets Design

## Goal

Let users create the current month's budget setup from the previous month in one action, reducing repetitive monthly setup work.

## Scope

- Add a backend endpoint that copies the previous month's budgets into a target month.
- Allow copying only when the target month has no budgets.
- Copy each previous-month budget's `CategoryId` and `Amount`.
- Return the newly created target-month budgets with current spending values.
- Add a "複製上月" action to the budgets page.

## Backend Design

Add `CopyPreviousMonthAsync(int userId, int year, int month)` to `IBudgetService` and `BudgetService`.

The service computes the previous month from the target `year/month`.

Rules:

- Target `year` must be 2020-2100 and `month` must be 1-12.
- If the target month already has any budgets, throw `InvalidOperationException("本月已有預算，無法複製上月預算")`.
- If the previous month has no budgets, throw `InvalidOperationException("上月沒有可複製的預算")`.
- Create target budgets with the same `CategoryId` and `Amount` as the previous month.
- Return `GetByMonthAsync(userId, year, month)` after saving so `Spent` reflects current target-month transactions.

Add controller endpoint:

```text
POST /api/budgets/{year}/{month}/copy-previous
```

The endpoint returns `400` for invalid month/year or business-rule failures.

## Frontend Design

Add `copyPreviousMonth(year, month)` to `budget.ts`.

In `BudgetsView.vue`, add a `複製上月` button near `新增預算`.

Behavior:

- Disabled while budget store is loading.
- Shows a confirmation dialog before calling the API.
- On success, replace `budgetStore.budgets` with the returned list and show `已複製上月預算`.
- On failure, show the backend error message.

The empty state keeps the existing "設定第一筆預算" button and gains a secondary "複製上月預算" action.

## Compatibility

No database schema changes are required. The existing unique indexes continue to prevent duplicate target-month budgets.

## Tests

Add backend service tests for:

- Copying previous month budgets creates matching target-month budgets.
- Copying is blocked when the target month already has budgets.
- Copying is blocked when the previous month has no budgets.
