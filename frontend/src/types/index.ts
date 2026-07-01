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

// ===== Pagination =====

export interface PagedResult<T> {
  items: T[]
  total: number
}

export interface TransactionQueryParams {
  page?: number
  pageSize?: number
  keyword?: string
  type?: TransactionType
  accountId?: number
  startDate?: string | null
  endDate?: string | null
}

export interface RecurringTransactionQueryParams {
  page?: number
  pageSize?: number
  frequency?: RecurrenceFrequency
  isActive?: boolean
}

// ===== Accounts =====

export enum AccountType {
  Cash = 0,
  Bank = 1,
  CreditCard = 2,
  Investment = 3,
  Other = 4,
}

export const AccountTypeLabels: Record<AccountType, string> = {
  [AccountType.Cash]: '現金',
  [AccountType.Bank]: '銀行',
  [AccountType.CreditCard]: '信用卡',
  [AccountType.Investment]: '投資',
  [AccountType.Other]: '其他',
}

export interface CreateAccountRequest {
  name: string
  accountType: AccountType
  initialBalance: number
}

export interface UpdateAccountRequest {
  name: string
  accountType: AccountType
  initialBalance: number
}

export interface AccountResponse {
  id: number
  name: string
  accountType: AccountType
  initialBalance: number
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

// ===== Reports =====

export interface CategoryBreakdown {
  categoryName: string
  amount: number
  percentage: number
}

export interface DailyExpense {
  date: string
  income: number
  expense: number
}

export interface MonthlyReportResponse {
  year: number
  month: number
  totalIncome: number
  totalExpense: number
  balance: number
  transactionCount: number
  expenseByCategory: CategoryBreakdown[]
  incomeByCategory: CategoryBreakdown[]
  dailyExpenses: DailyExpense[]
}

export interface RangeReportResponse {
  startDate: string
  endDate: string
  totalIncome: number
  totalExpense: number
  balance: number
  transactionCount: number
  expenseByCategory: CategoryBreakdown[]
  incomeByCategory: CategoryBreakdown[]
  dailyExpenses: DailyExpense[]
}
