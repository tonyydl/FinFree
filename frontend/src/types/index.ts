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
}

export interface UpdateTransactionRequest {
  amount?: number
  type?: TransactionType
  categoryId?: number
  description?: string
  date?: string
}

export interface TransactionResponse {
  id: number
  amount: number
  type: TransactionType
  categoryId: number
  categoryName: string
  description: string | null
  date: string
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

// ===== Statistics =====

export interface StatisticsResponse {
  totalIncome: number
  totalExpense: number
  balance: number
  transactionCount: number
}
