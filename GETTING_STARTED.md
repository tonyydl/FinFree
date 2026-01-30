# FinFree 快速開始指南

本指南將協助你在本地環境啟動 FinFree 專案。

## 前置需求

請確保你的電腦已安裝以下工具：

- **.NET 9.0 SDK** - [下載連結](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** - [下載連結](https://www.docker.com/products/docker-desktop)
- **Git** - [下載連結](https://git-scm.com/downloads)

### 驗證安裝

```bash
# 檢查 .NET 版本
dotnet --version
# 應顯示 9.0.x

# 檢查 Docker
docker --version
docker compose version

# 檢查 Git
git --version
```

---

## 步驟 1：啟動 PostgreSQL 資料庫

### 使用 Docker Compose

```bash
# 在專案根目錄執行
docker compose up -d
```

### 驗證資料庫是否啟動成功

```bash
docker ps
```

應該會看到類似以下的輸出：

```
CONTAINER ID   IMAGE                PORTS                    NAMES
abc123def456   postgres:16-alpine   0.0.0.0:5432->5432/tcp   finfree-postgres
```

### 停止資料庫（需要時）

```bash
docker compose down
```

---

## 步驟 2：設定資料庫

### 2.1 安裝 EF Core 工具（首次執行）

```bash
dotnet tool install --global dotnet-ef
```

如果已安裝，可以更新：

```bash
dotnet tool update --global dotnet-ef
```

### 2.2 執行資料庫遷移

```bash
cd backend\FinFree.Api
dotnet ef database update
```

成功後會看到：

```
Build started...
Build succeeded.
Applying migration '20xxxxxx_InitialCreate'.
Done.
```

---

## 步驟 3：啟動 API

```bash
# 確保在 backend/FinFree.Api 目錄
cd backend\FinFree.Api

# 啟動專案（預設使用 HTTP）
dotnet run
```

看到以下訊息表示啟動成功：

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5016
```

> **💡 提示**：如需使用 HTTPS（例如測試第三方 OAuth 登入），執行：
>
> ```bash
> dotnet run --launch-profile https
> ```
>
> 此時會同時啟動 HTTPS (port 7233) 和 HTTP (port 5016)

---

## 步驟 4：測試 API

### 4.1 開啟 Swagger UI

瀏覽器訪問：**http://localhost:5016/swagger**

> 如果使用 HTTPS profile 啟動，也可以訪問：https://localhost:7233/swagger

### 4.2 註冊新使用者

1. 在 Swagger UI 中找到 `POST /api/auth/register`
2. 點擊 **Try it out**
3. 輸入測試資料：

```json
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "Test123456"
}
```

4. 點擊 **Execute**
5. 應該會收到回應，包含 JWT Token：

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "testuser",
  "email": "test@example.com"
}
```

### 4.3 設定 JWT 認證

1. 複製回應中的 `token` 值
2. 點擊 Swagger UI 右上角的 **Authorize** 按鈕（綠色鎖頭圖示）
3. 在彈出視窗中輸入：
   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
   （注意：`Bearer` 後面有一個空格）
4. 點擊 **Authorize**
5. 點擊 **Close**

### 4.4 測試交易 API

#### 取得分類列表

1. 找到 `GET /api/categories`
2. 點擊 **Try it out** → **Execute**
3. 應該會看到系統預設分類（薪資、飲食、交通等）

#### 新增交易記錄

1. 找到 `POST /api/transactions`
2. 點擊 **Try it out**
3. 輸入測試資料：

```json
{
  "amount": 500,
  "type": 1,
  "categoryId": 5,
  "description": "午餐",
  "date": "2024-01-25T12:00:00Z"
}
```

參數說明：

- `amount`: 金額
- `type`: 0=收入, 1=支出
- `categoryId`: 分類 ID（從 categories API 取得）
- `description`: 描述
- `date`: 日期時間

4. 點擊 **Execute**

#### 查詢所有交易

1. 找到 `GET /api/transactions`
2. 點擊 **Try it out** → **Execute**
3. 應該會看到剛才新增的交易記錄

#### 查詢統計資訊

1. 找到 `GET /api/transactions/statistics`
2. 點擊 **Try it out** → **Execute**
3. 會顯示總收入、總支出、餘額等資訊

---

## 常見問題

### Q1: Docker 無法啟動？

**錯誤訊息**: `error during connect: ... dockerDesktopLinuxEngine`

**解決方法**:

1. 啟動 Docker Desktop 應用程式
2. 等待 Docker 引擎完全啟動（圖示變為綠色）
3. 重新執行 `docker compose up -d`

### Q2: 資料庫連線失敗？

**錯誤訊息**: `Npgsql.NpgsqlException: Connection refused`

**解決方法**:

1. 確認 Docker 容器正在運行：`docker ps`
2. 檢查 Port 5432 是否被佔用：`netstat -an | grep 5432`
3. 重新啟動 Docker 容器：
   ```bash
   docker compose down
   docker compose up -d
   ```

### Q3: Migration 執行失敗？

**錯誤訊息**: `Build failed` 或 `Unable to create migration`

**解決方法**:

1. 先執行建置：`dotnet build`
2. 檢查錯誤訊息並修正程式碼
3. 重新執行：`dotnet ef migrations add InitialCreate`

### Q4: JWT Token 認證失敗？

**錯誤訊息**: `401 Unauthorized`

**解決方法**:

1. 確認 Token 格式正確：`Bearer {token}`
2. 檢查 Token 前面的 `Bearer` 後面有空格
3. Token 可能過期（預設 60 分鐘），重新登入取得新 Token

### Q5: 如何重置資料庫？

```bash
cd backend\FinFree.Api

# 刪除所有 Migration
dotnet ef database drop

# 重新建立
dotnet ef database update
```

---

## 開發工具推薦

### 後端開發

- **Visual Studio 2022** - 完整 IDE
- **Visual Studio Code** - 輕量編輯器
  - 推薦擴充功能：
    - C# Dev Kit
    - REST Client
    - Docker

### API 測試

- **Postman** - [下載連結](https://www.postman.com/downloads/)
- **Insomnia** - [下載連結](https://insomnia.rest/download)
- **Swagger UI** - 已內建（http://localhost:5016/swagger）

### 資料庫管理

- **pgAdmin** - [下載連結](https://www.pgadmin.org/download/)
- **DBeaver** - [下載連結](https://dbeaver.io/download/)
- **DataGrip** - [下載連結](https://www.jetbrains.com/datagrip/)

連線資訊：

- Host: `localhost`
- Port: `5432`
- Database: `finfree`
- Username: `postgres`
- Password: `postgres`

---

## 下一步

1. 閱讀 [README.md](README.md) 了解專案架構
2. 查看 [API 文件](http://localhost:5016/swagger) 了解所有端點
3. 開始開發前端介面（Vue.js）

---

## 專案結構

```
FinFree/
├── backend/
│   └── FinFree.Api/
│       ├── Controllers/         # API 控制器
│       ├── Models/              # 資料模型
│       ├── DTOs/                # 資料傳輸物件
│       ├── Data/                # DbContext
│       ├── Repositories/        # Repository Pattern
│       ├── Services/            # 商業邏輯
│       ├── Helpers/             # 輔助類別
│       ├── Migrations/          # EF Core Migrations
│       └── Program.cs           # 應用程式入口
├── docker-compose.yml           # Docker 配置
├── .gitignore                   # Git 忽略檔案
├── README.md                    # 專案說明
└── GETTING_STARTED.md           # 本文件
```
