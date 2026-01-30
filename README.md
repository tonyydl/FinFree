# FinFree - 個人記帳應用

一個使用 ASP.NET Core 和 Vue.js 開發的全端記帳應用，目標是在一個月內完成 MVP 版本。

## 技術棧

### 後端
- ASP.NET Core 9.0 Web API
- Entity Framework Core 9.0
- PostgreSQL 16
- JWT 認證
- Repository Pattern

### 前端 (規劃中)
- Vue.js 3
- TypeScript
- Pinia (狀態管理)
- Axios (HTTP 客戶端)

## 專案結構

```
FinFree/
├── backend/
│   └── FinFree.Api/
│       ├── Controllers/         # API 控制器
│       ├── Models/              # 資料模型 (Entities)
│       ├── DTOs/                # 資料傳輸物件
│       ├── Data/                # DbContext
│       ├── Repositories/        # Repository Pattern
│       ├── Services/            # 商業邏輯層
│       └── Helpers/             # 輔助類別
├── frontend/                    # (待建立)
├── docker-compose.yml           # Docker 配置
└── README.md
```

## 主要功能

- [x] 使用者註冊/登入 (JWT 認證)
- [x] 新增/編輯/刪除交易記錄
- [x] 查詢交易列表
- [x] 統計資訊 (總收入、總支出、餘額)
- [x] 系統預設分類
- [ ] 前端介面
- [ ] 日期篩選
- [ ] 圖表顯示

## 快速開始

### 前置需求

- .NET 9.0 SDK
- Docker Desktop
- Git

### 1. 啟動資料庫

```bash
# 在專案根目錄執行
docker-compose up -d
```

### 2. 執行資料庫遷移

```bash
cd backend/FinFree.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. 啟動 API

```bash
cd backend/FinFree.Api
dotnet run
```

API 將在 `https://localhost:5001` 和 `http://localhost:5000` 上運行

### 4. 開啟 Swagger UI

瀏覽器訪問: `http://localhost:5000/swagger`

## API 端點

### 認證
- `POST /api/auth/register` - 使用者註冊
- `POST /api/auth/login` - 使用者登入

### 交易 (需要認證)
- `GET /api/transactions` - 取得所有交易
- `GET /api/transactions/{id}` - 取得單筆交易
- `POST /api/transactions` - 新增交易
- `PUT /api/transactions/{id}` - 更新交易
- `DELETE /api/transactions/{id}` - 刪除交易
- `GET /api/transactions/statistics` - 取得統計資訊

### 分類 (需要認證)
- `GET /api/categories` - 取得所有分類

## 資料庫配置

預設連線字串在 `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=finfree;Username=postgres;Password=postgres"
  }
}
```

## 開發進度

- [x] 後端專案架構建立
- [x] Entity Models 設計
- [x] Repository Pattern 實作
- [x] JWT 認證實作
- [x] CRUD API 實作
- [ ] 前端專案建立
- [ ] 前後端整合
- [ ] 單元測試
- [ ] 部署

## 授權

MIT License
