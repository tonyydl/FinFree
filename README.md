# FinFree - 個人記帳應用

一個使用 ASP.NET Core 9 + Vue 3 開發的全端個人財務管理應用。

## 功能

- 使用者註冊／登入（JWT 認證）
- 多帳戶管理（現金、銀行、信用卡、投資等）
- 交易記錄新增／編輯／刪除，支援關鍵字、分類、帳戶、日期篩選
- 週期性交易（每日／每週／每月／每年自動建立）
- 預算管理，超標／即將用完通知
- 月份報表（收支摘要、分類圓餅圖、每日趨勢圖）
- Dashboard 本月收支摘要、帳戶餘額
- CSV 交易記錄匯入
- 深色模式
- PWA 支援（可安裝至桌面）

## 技術棧

### 後端
- ASP.NET Core 9.0 Web API
- Entity Framework Core 9.0 + PostgreSQL 16
- JWT 認證
- Repository + UnitOfWork + Service 架構

### 前端
- Vue 3 + TypeScript + Vite
- Pinia（狀態管理）
- Element Plus（UI）
- Chart.js（圖表）
- Axios

## 專案結構

```
FinFree/
├── backend/
│   ├── Dockerfile
│   └── FinFree.Api/
│       ├── Controllers/         # API 控制器
│       ├── Models/              # 資料模型
│       ├── DTOs/                # 請求／回應 DTO
│       ├── Data/                # AppDbContext
│       ├── Repositories/        # Repository Pattern
│       ├── Services/            # 商業邏輯層
│       ├── Migrations/          # EF Core Migrations
│       └── Program.cs
├── frontend/
│   ├── Dockerfile
│   ├── nginx.conf
│   └── src/
│       ├── views/               # 頁面
│       ├── components/          # 元件
│       ├── stores/              # Pinia stores
│       ├── router/              # Vue Router
│       ├── api/                 # Axios 設定
│       └── types/               # TypeScript 型別
├── docker-compose.yml
├── .env.example
└── README.md
```

## 快速開始

### 前置需求

- Docker Desktop

### 一鍵啟動（生產模式）

```bash
# 1. 複製環境變數範本
cp .env.example .env
# 編輯 .env，設定安全的密碼與 JWT Key

# 2. 啟動所有服務（首次或程式碼有更新時）
docker compose up -d --build

# 單純重啟（程式碼沒變）
docker compose up -d
```

啟動後開啟瀏覽器：**http://localhost**

> 首次啟動會自動執行資料庫 migration，建立所有資料表。

### 停止服務

```bash
docker compose down
```

> 資料存放於 Docker volume，停止後不會消失。若要同時刪除資料：`docker compose down -v`

---

## 本地開發

### 前置需求

- .NET 9.0 SDK
- Docker Desktop（用於 PostgreSQL / Redis）
- Node.js 22+

### 1. 啟動本地開發基礎設施

```bash
docker compose up -d postgres redis
```

本地開發建議只用 Docker 啟動 PostgreSQL 與 Redis，後端用 `dotnet run`、前端用 Vite dev server 啟動，方便熱更新與除錯。

開發環境使用獨立的資料庫 `finfree_dev`，與正式環境 `finfree` 完全分離。首次開發前需手動建立：

```bash
docker exec finfree-postgres psql -U postgres -c "CREATE DATABASE finfree_dev;"
```

### 2. 啟動後端

```bash
cd backend/FinFree.Api
dotnet run
```

`dotnet run` 預設使用 `ASPNETCORE_ENVIRONMENT=Development`，會自動載入 `appsettings.Development.json`，連線至 `finfree_dev` 資料庫。

API 運行於 `http://localhost:5016`，Swagger UI：`http://localhost:5016/swagger`

### 3. 啟動前端

```bash
cd frontend
npm install
npm run dev
```

前端運行於 `http://localhost:5173`

### 4. 塞測試假資料（可選）

後端啟動後，執行腳本快速建立測試帳號與假資料：

```powershell
.\scripts\seed-dev-data.ps1
```

建立內容：

| 項目 | 內容 |
|------|------|
| 測試帳號 | `test@finfree.dev` / `Test1234!` |
| 帳戶 | 現金、玉山銀行、國泰信用卡、股票帳戶 |
| 交易 | 近 3 個月約 70 筆收支記錄 |
| 預算 | 本月 6 筆分類預算 |
| 定期交易 | 薪資、房租、保險、訂閱服務共 5 筆 |

**重置假資料：**

```powershell
# 1. 刪除測試帳號（會 cascade 清除所有關聯資料）
docker exec finfree-postgres psql -U postgres -d finfree_dev -c 'DELETE FROM "Users" WHERE "Email" = '"'"'test@finfree.dev'"'"';'

# 2. 重新塞資料
.\scripts\seed-dev-data.ps1
```

---

## 環境變數

參考 `.env.example`：

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your-secure-password
JWT_KEY=your-super-secret-key-minimum-32-characters-long
```

---

## API 端點

| 方法 | 路徑 | 說明 |
|------|------|------|
| POST | `/api/auth/register` | 使用者註冊 |
| POST | `/api/auth/login` | 使用者登入 |
| GET/POST | `/api/accounts` | 帳戶列表／新增 |
| PUT/DELETE | `/api/accounts/{id}` | 更新／刪除帳戶 |
| POST | `/api/accounts/transfer` | 帳戶轉帳 |
| GET/POST | `/api/transactions` | 交易列表／新增 |
| PUT/DELETE | `/api/transactions/{id}` | 更新／刪除交易 |
| POST | `/api/transactions/import` | CSV 匯入 |
| GET | `/api/transactions/report` | 月份報表 |
| GET/POST | `/api/categories` | 分類列表／新增 |
| GET/POST | `/api/budgets` | 預算列表／新增 |
| GET/POST | `/api/recurring-transactions` | 週期交易列表／新增 |
| PUT | `/api/auth/username` | 修改使用者名稱 |
| PUT | `/api/auth/password` | 修改密碼 |

---

## 常見問題

**Q: Docker 無法啟動？**
啟動 Docker Desktop，等待圖示變為綠色後重試。

**Q: Port 被佔用？**
```bash
# 修改 docker-compose.yml 中的 port mapping，例如改為 8080:80
```

**Q: 如何重置資料庫？**
```bash
docker compose down -v   # 刪除 volume
docker compose up -d     # 重新啟動並自動建立資料表
```

**Q: 如何新增 EF Core Migration？**
```bash
cd backend/FinFree.Api
dotnet ef migrations add 描述名稱
dotnet ef database update
```

---

## 授權

MIT License
