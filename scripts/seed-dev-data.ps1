# FinFree 開發環境假資料腳本
# 用法: .\seed-dev-data.ps1 [-BaseUrl "http://localhost:5016"]

param(
    [string]$BaseUrl = "http://localhost:5016"
)

$ApiBase = "$BaseUrl/api"
$Headers = @{ "Content-Type" = "application/json" }

function Invoke-Api {
    param($Method, $Path, $Body = $null, $AuthToken = $null)
    $h = @{ "Content-Type" = "application/json" }
    if ($AuthToken) { $h["Authorization"] = "Bearer $AuthToken" }
    $uri = "$ApiBase$Path"
    try {
        if ($Body) {
            $json = $Body | ConvertTo-Json -Depth 5
            return Invoke-RestMethod -Method $Method -Uri $uri -Headers $h -Body $json
        } else {
            return Invoke-RestMethod -Method $Method -Uri $uri -Headers $h
        }
    } catch {
        $errDetail = $_.ErrorDetails.Message
        Write-Warning "[$Method $Path] $($_.Exception.Message) $errDetail"
        return $null
    }
}

# ── 1. 註冊測試帳號 ────────────────────────────────────────
Write-Host "`n[1] 建立測試使用者..." -ForegroundColor Cyan

$registerBody = @{
    username = "testuser"
    email    = "test@finfree.dev"
    password = "Test1234!"
}
$auth = Invoke-Api POST "/auth/register" $registerBody

if (-not $auth) {
    Write-Host "    使用者已存在，嘗試登入..." -ForegroundColor Yellow
    $loginBody = @{ email = "test@finfree.dev"; password = "Test1234!" }
    $auth = Invoke-Api POST "/auth/login" $loginBody
}

if (-not $auth -or -not $auth.token) {
    Write-Error "無法取得 token，請確認後端服務已啟動於 $BaseUrl"
    exit 1
}

$token = $auth.token
Write-Host "    登入成功，使用者: $($auth.username)" -ForegroundColor Green

# ── 2. 建立帳戶 ───────────────────────────────────────────
Write-Host "`n[2] 建立帳戶..." -ForegroundColor Cyan

$accounts = @(
    @{ name = "現金"; accountType = 0 }         # Cash
    @{ name = "玉山銀行"; accountType = 1 }      # Bank
    @{ name = "國泰信用卡"; accountType = 2 }    # CreditCard
    @{ name = "股票帳戶"; accountType = 3 }      # Investment
)

$accountIds = @{}
foreach ($a in $accounts) {
    $res = Invoke-Api POST "/accounts" $a $token
    if ($res) {
        $accountIds[$a.name] = $res.id
        Write-Host "    建立帳戶: $($a.name) (id=$($res.id))" -ForegroundColor Green
    }
}

$cashId   = $accountIds["現金"]
$bankId   = $accountIds["玉山銀行"]
$ccId     = $accountIds["國泰信用卡"]
$stockId  = $accountIds["股票帳戶"]

# ── 3. 取得系統分類 ─────────────────────────────────────────
Write-Host "`n[3] 讀取系統分類..." -ForegroundColor Cyan

$categories = Invoke-Api GET "/categories" -AuthToken $token
$catMap = @{}
foreach ($c in $categories) { $catMap[$c.name] = $c.id }

# System category IDs (seeded in AppDbContext)
# Income: 薪資=1, 獎金=2, 投資收益=3, 其他收入=4
# Expense: 飲食=5, 交通=6, 娛樂=7, 購物=8, 醫療=9, 其他支出=10

Write-Host "    分類載入完成 ($($categories.Count) 筆)" -ForegroundColor Green

# ── 4. 建立使用者自訂分類 ──────────────────────────────────
Write-Host "`n[4] 建立自訂分類..." -ForegroundColor Cyan

$customCats = @(
    @{ name = "房租"; type = 1 }        # Expense
    @{ name = "訂閱服務"; type = 1 }    # Expense
    @{ name = "保險"; type = 1 }        # Expense
    @{ name = "副業收入"; type = 0 }    # Income
)
foreach ($c in $customCats) {
    $res = Invoke-Api POST "/categories" $c $token
    if ($res) {
        $catMap[$c.name] = $res.id
        Write-Host "    建立分類: $($c.name) (id=$($res.id))" -ForegroundColor Green
    }
}

# ── 5. 建立近 3 個月交易 ──────────────────────────────────
Write-Host "`n[5] 建立交易資料..." -ForegroundColor Cyan

$today = Get-Date
$txCount = 0

function Add-Tx {
    param($Amount, $Type, $CatName, $Desc, $DaysAgo, $AccId)
    $cat = $catMap[$CatName]
    if (-not $cat) { Write-Warning "找不到分類: $CatName"; return }
    $date = $today.AddDays(-$DaysAgo).ToString("yyyy-MM-ddT00:00:00.000Z")
    $body = @{
        amount      = $Amount
        type        = $Type  # 0=Income, 1=Expense
        categoryId  = $cat
        description = $Desc
        date        = $date
        accountId   = $AccId
    }
    $res = Invoke-Api POST "/transactions" $body $token
    if ($res) { $script:txCount++ }
}

# 月薪（每月）—— 說明動態產生，對應實際日期月份
$m1 = $today.AddDays(-85).Month; Add-Tx 55000  0 "薪資"  "$($m1)月薪資"  85  $bankId
$m2 = $today.AddDays(-55).Month; Add-Tx 55000  0 "薪資"  "$($m2)月薪資"  55  $bankId
$m3 = $today.AddDays(-25).Month; Add-Tx 55000  0 "薪資"  "$($m3)月薪資"  25  $bankId
$m4 = $today.AddDays(-5).Month;  Add-Tx 55000  0 "薪資"  "$($m4)月薪資"   5  $bankId

# 獎金
Add-Tx 30000  0 "獎金"    "年中績效獎金"  60  $bankId

# 副業
Add-Tx  8000  0 "副業收入" "freelance 設計案"  40  $cashId
Add-Tx  5000  0 "副業收入" "家教收入"          15  $cashId

# 投資
Add-Tx 12000  0 "投資收益" "台積電股利"   30  $stockId
Add-Tx  3500  0 "投資收益" "ETF 配息"    10  $stockId

# 房租（每月固定）
Add-Tx 18000  1 "房租"    "7月房租"      5   $bankId
Add-Tx 18000  1 "房租"    "6月房租"     35   $bankId
Add-Tx 18000  1 "房租"    "5月房租"     65   $bankId

# 飲食
Add-Tx  320  1 "飲食" "午餐便當"      2   $cashId
Add-Tx  580  1 "飲食" "晚餐聚餐"      3   $cashId
Add-Tx  150  1 "飲食" "早餐"          4   $cashId
Add-Tx 1200  1 "飲食" "家庭聚餐"      7   $ccId
Add-Tx  450  1 "飲食" "外送平台"      8   $ccId
Add-Tx  280  1 "飲食" "超商消費"      9   $cashId
Add-Tx  680  1 "飲食" "火鍋"         12   $ccId
Add-Tx  220  1 "飲食" "早餐"         14   $cashId
Add-Tx 1500  1 "飲食" "烤肉食材"     20   $cashId
Add-Tx  380  1 "飲食" "午餐"         22   $ccId
Add-Tx  560  1 "飲食" "晚餐"         25   $ccId
Add-Tx  900  1 "飲食" "朋友聚餐"     30   $ccId
Add-Tx  310  1 "飲食" "便當"         35   $cashId
Add-Tx  420  1 "飲食" "滷肉飯外送"   40   $ccId
Add-Tx  270  1 "飲食" "早午餐"       45   $cashId
Add-Tx 1800  1 "飲食" "尾牙聚餐"     50   $ccId
Add-Tx  350  1 "飲食" "下午茶"       55   $cashId
Add-Tx  480  1 "飲食" "晚餐"         60   $cashId
Add-Tx  290  1 "飲食" "便利商店"     65   $cashId
Add-Tx  720  1 "飲食" "日式料理"     70   $ccId

# 交通
Add-Tx  100  1 "交通" "悠遊卡加值"    3   $cashId
Add-Tx 1200  1 "交通" "高鐵台北→台中" 15  $ccId
Add-Tx  800  1 "交通" "計程車"        18  $ccId
Add-Tx  200  1 "交通" "停車費"        22  $cashId
Add-Tx 2800  1 "交通" "機票（國內）"  45  $ccId
Add-Tx  150  1 "交通" "公車"          50  $cashId
Add-Tx  600  1 "交通" "Uber"          58  $ccId
Add-Tx  180  1 "交通" "捷運月票"      62  $cashId

# 娛樂
Add-Tx  380  1 "娛樂" "電影票"        5   $ccId
Add-Tx 1200  1 "娛樂" "KTV"          10   $ccId
Add-Tx  450  1 "娛樂" "Switch 遊戲"  20   $ccId
Add-Tx  800  1 "娛樂" "演唱會門票"   35   $ccId
Add-Tx  250  1 "娛樂" "桌遊店"       48   $ccId
Add-Tx  600  1 "娛樂" "Netflix 年費" 70   $ccId

# 購物
Add-Tx  2400  1 "購物" "Nike 球鞋"     6   $ccId
Add-Tx   890  1 "購物" "書籍（技術書）" 11  $ccId
Add-Tx  1500  1 "購物" "UNIQLO 衣服"  16   $ccId
Add-Tx   350  1 "購物" "文具"         24   $cashId
Add-Tx  4800  1 "購物" "耳機"         38   $ccId
Add-Tx   620  1 "購物" "生活用品"     52   $cashId
Add-Tx  1200  1 "購物" "運動服裝"     68   $ccId

# 醫療
Add-Tx   250  1 "醫療" "診所掛號費"   13   $cashId
Add-Tx   180  1 "醫療" "藥局買藥"     28   $cashId
Add-Tx  1200  1 "醫療" "健康檢查"     55   $bankId

# 訂閱服務
Add-Tx   190  1 "訂閱服務" "Spotify"           3   $ccId
Add-Tx   390  1 "訂閱服務" "ChatGPT Plus"       3   $ccId
Add-Tx   270  1 "訂閱服務" "Adobe CC（學生）"  32   $ccId
Add-Tx   190  1 "訂閱服務" "Spotify"           33   $ccId
Add-Tx   390  1 "訂閱服務" "ChatGPT Plus"      33   $ccId
Add-Tx   270  1 "訂閱服務" "Adobe CC（學生）"  62   $ccId
Add-Tx   190  1 "訂閱服務" "Spotify"           63   $ccId

# 保險
Add-Tx  3200  1 "保險" "人壽保險（月繳）"  8   $bankId
Add-Tx  3200  1 "保險" "人壽保險（月繳）" 38   $bankId
Add-Tx  3200  1 "保險" "人壽保險（月繳）" 68   $bankId

Write-Host "    新增交易: $txCount 筆" -ForegroundColor Green

# ── 6. 建立本月預算 ──────────────────────────────────────
Write-Host "`n[6] 建立預算..." -ForegroundColor Cyan

$year  = $today.Year
$month = $today.Month

$budgets = @(
    @{ categoryId = $null;              amount = 40000; year = $year; month = $month }  # 整體預算
    @{ categoryId = $catMap["飲食"];    amount = 10000; year = $year; month = $month }
    @{ categoryId = $catMap["交通"];    amount =  3000; year = $year; month = $month }
    @{ categoryId = $catMap["娛樂"];    amount =  5000; year = $year; month = $month }
    @{ categoryId = $catMap["購物"];    amount =  6000; year = $year; month = $month }
    @{ categoryId = $catMap["訂閱服務"]; amount = 1000; year = $year; month = $month }
)

$budgetCount = 0
foreach ($b in $budgets) {
    $res = Invoke-Api POST "/budgets" $b $token
    if ($res) {
        $budgetCount++
        $name = if ($b.categoryId) { ($catMap.GetEnumerator() | Where-Object { $_.Value -eq $b.categoryId } | Select-Object -First 1).Key } else { "整體" }
        Write-Host "    建立預算: $name = $($b.amount)" -ForegroundColor Green
    }
}

# ── 7. 建立定期交易 ──────────────────────────────────────
Write-Host "`n[7] 建立定期交易..." -ForegroundColor Cyan

$startOf2024 = (Get-Date -Year $today.Year -Month 1 -Day 1).ToString("yyyy-MM-ddT00:00:00.000Z")

$recurring = @(
    @{
        amount = 55000; type = 0; categoryId = $catMap["薪資"]
        description = "每月薪資"; frequency = 0  # Monthly
        startDate = $startOf2024; accountId = $bankId
    }
    @{
        amount = 18000; type = 1; categoryId = $catMap["房租"]
        description = "每月房租"; frequency = 0
        startDate = $startOf2024; accountId = $bankId
    }
    @{
        amount = 3200; type = 1; categoryId = $catMap["保險"]
        description = "人壽保險月繳"; frequency = 0
        startDate = $startOf2024; accountId = $bankId
    }
    @{
        amount = 190; type = 1; categoryId = $catMap["訂閱服務"]
        description = "Spotify"; frequency = 0
        startDate = $startOf2024; accountId = $ccId
    }
    @{
        amount = 390; type = 1; categoryId = $catMap["訂閱服務"]
        description = "ChatGPT Plus"; frequency = 0
        startDate = $startOf2024; accountId = $ccId
    }
)

$recurCount = 0
foreach ($r in $recurring) {
    $res = Invoke-Api POST "/recurring-transactions" $r $token
    if ($res) {
        $recurCount++
        Write-Host "    建立定期: $($r.description)" -ForegroundColor Green
    }
}

# ── 完成 ─────────────────────────────────────────────────
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host " 假資料建立完成！" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " 使用者  : test@finfree.dev / Test1234!"
Write-Host " 帳戶    : $($accountIds.Count) 個"
Write-Host " 交易    : $txCount 筆"
Write-Host " 預算    : $budgetCount 筆"
Write-Host " 定期交易: $recurCount 筆"
Write-Host ""
