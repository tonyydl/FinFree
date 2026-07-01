# Date Range Reports Design

## Goal

Let users analyze income and expenses across any date range, not only a single month.

## Scope

- Keep the existing monthly report API and UI behavior.
- Add a date-range report API.
- Reuse the existing report summary, category breakdown, daily trend chart, and CSV export UI.
- Add a report mode switch in `ReportsView.vue`: monthly mode and custom range mode.

## Backend Design

Add `RangeReportResponse` with:

- `StartDate`
- `EndDate`
- `TotalIncome`
- `TotalExpense`
- `Balance`
- `TransactionCount`
- `ExpenseByCategory`
- `IncomeByCategory`
- `DailyExpenses`

Add service method:

```csharp
Task<RangeReportResponse> GetRangeReportAsync(DateTime startDate, DateTime endDate, int userId);
```

Date handling:

- The API accepts date-only values from query strings.
- `endDate` is inclusive for users. Internally, the query uses `< endDate.Date.AddDays(1)`.
- If `startDate > endDate`, return `400`.
- Limit dates to the same broad range used by monthly reports: years `2000-2100`.

Add endpoint:

```text
GET /api/transactions/report/range?startDate=2026-01-01&endDate=2026-06-30
```

The aggregation should match monthly report behavior:

- Total income and expenses come from transactions in the range.
- Percentages are calculated within income or expense totals.
- Daily trend groups by `yyyy-MM-dd`.

## Frontend Design

`ReportsView.vue` gets a mode control:

```text
[月份] [自訂區間]
```

Monthly mode keeps the existing year/month selectors and calls the existing endpoint.

Range mode shows an Element Plus daterange picker and calls:

```text
/transactions/report/range
```

The summary cards, category lists, charts, empty state, and CSV export use a normalized computed report shape so the template does not duplicate large sections.

CSV export:

- Monthly mode title: `月份報表`
- Range mode title: `區間報表`
- Range filename: `FinFree_報表_YYYYMMDD-YYYYMMDD.csv`

## Compatibility

No database schema changes are required. Existing monthly report callers remain unchanged.

## Tests

Add backend service tests for:

- Range totals include transactions on both start and end dates.
- Transactions outside the range are excluded.
- Category breakdown percentages are calculated against the in-range totals.
