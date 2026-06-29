# Account Initial Balance Design

## Goal

Let users set an initial balance for each account so FinFree can show real account balances even when historical transactions are incomplete.

## Scope

- Add `InitialBalance` to accounts.
- Include initial balance in account create, update, and response DTOs.
- Calculate account balance as `InitialBalance + income transactions - expense transactions`.
- Show and edit initial balance in the account management dialog.
- Keep existing accounts compatible by defaulting initial balance to `0`.

## Backend Design

`Account` gains a decimal `InitialBalance` property with database precision `18,2`.

`CreateAccountRequest`, `UpdateAccountRequest`, and `AccountResponse` expose `InitialBalance`.

`AccountService` includes the account's initial balance in `GetAllAsync`, `GetByIdAsync`, create, update, and transfer balance checks. Transfer validation uses the full computed balance.

## Frontend Design

`AccountResponse`, `CreateAccountRequest`, and `UpdateAccountRequest` gain `initialBalance`.

`AccountsView.vue` adds an initial balance number field to the add/edit account dialog. Existing account lists continue to highlight computed current balance.

## Compatibility

Existing accounts receive `InitialBalance = 0` through migration defaults. Existing transaction and report behavior remains unchanged.

## Tests

Add focused backend tests for:

- Account list balances include initial balance plus transactions.
- Transfer validation allows transfers funded by initial balance.
