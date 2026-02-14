// ===== Enums =====

export enum TransactionType {
  Income = 0,
  Expense = 1,
}

// ===== Auth =====

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  username: string
  email: string
  password: string
}

export interface AuthResponse {
  token: string
  username: string
  email: string
}

// ===== Transactions =====

export interface CreateTransactionRequest {
  amount: number
  type: TransactionType
  categoryId: number
  description?: string
  date: string
  accountId: number
}

export interface UpdateTransactionRequest {
  amount?: number
  type?: TransactionType
  categoryId?: number
  description?: string
  date?: string
  accountId?: number
}

export interface TransactionResponse {
  id: number
  amount: number
  type: TransactionType
  categoryId: number
  categoryName: string
  description: string | null
  date: string
  accountId: number | null
  accountName: string | null
  createdAt: string
}

// ===== Categories =====

export interface CreateCategoryRequest {
  name: string
  type: TransactionType
}

export interface UpdateCategoryRequest {
  name: string
}

export interface CategoryResponse {
  id: number
  name: string
  type: TransactionType
  isSystemDefault: boolean
}

// ===== User =====

export interface UpdateProfileRequest {
  username: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface ProfileResponse {
  username: string
  email: string
  createdAt: string
}

// ===== Budgets =====

export interface CreateBudgetRequest {
  categoryId?: number | null
  amount: number
  year: number
  month: number
}

export interface UpdateBudgetRequest {
  amount: number
}

export interface BudgetResponse {
  id: number
  categoryId: number | null
  categoryName: string | null
  amount: number
  spent: number
  year: number
  month: number
}

// ===== Recurring Transactions =====

export enum RecurrenceFrequency {
  Monthly = 0,
  Weekly = 1,
  Daily = 2,
  Yearly = 3,
}

export interface CreateRecurringTransactionRequest {
  amount: number
  type: TransactionType
  categoryId: number
  description?: string
  frequency: RecurrenceFrequency
  startDate: string
  endDate?: string | null
  accountId: number
}

export interface UpdateRecurringTransactionRequest {
  amount?: number
  description?: string
  endDate?: string | null
  isActive?: boolean
  accountId?: number
}

export interface RecurringTransactionResponse {
  id: number
  amount: number
  type: TransactionType
  categoryId: number
  categoryName: string
  description: string | null
  frequency: RecurrenceFrequency
  startDate: string
  nextOccurrenceDate: string
  endDate: string | null
  isActive: boolean
  accountId: number | null
  accountName: string | null
  createdAt: string
}

// ===== Accounts =====

export interface CreateAccountRequest {
  name: string
}

export interface UpdateAccountRequest {
  name: string
}

export interface AccountResponse {
  id: number
  name: string
  balance: number
  createdAt: string
}

export interface TransferRequest {
  fromAccountId: number
  toAccountId: number
  amount: number
  date: string
  description?: string
}

// ===== Statistics =====

export interface StatisticsResponse {
  totalIncome: number
  totalExpense: number
  balance: number
  transactionCount: number
}
