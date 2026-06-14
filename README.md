# EA Playmate Group Database

IIS 與 GitHub Actions CI/CD 設定請參考
[IIS_DEPLOYMENT.md](IIS_DEPLOYMENT.md)。

這個資料夾先放 SQL Server 版資料庫規劃。

## 執行順序

在 SSMS 20 依序執行：

1. `database/01_create_app_login.sql`
2. `database/02_create_tables.sql`
3. `database/03_seed_sample_data.sql`

## 密碼位置

不要把密碼貼到聊天或 commit 進 Git。

需要自己填的地方：

- `database/01_create_app_login.sql`
  - `請在這裡輸入強密碼`
- `appsettings.example.json`
  - `請在這裡輸入密碼`

正式專案請複製成 `appsettings.Development.json` 或用環境變數保存密碼。

## 連線資訊

目前規劃：

- Server: `192.168.0.180`
- Database: `EAPlaymateGroup`
- User Id: `ea_playmate_app`

目前 `appsettings.example.json` 沒有指定 port，會讓 SQL Server Client 使用預設連線方式。

本機開發暫時使用：

```text
Encrypt=False
```

正式上線或對外部署時，建議改回憑證正常的加密連線。

## DTO 檔案

C# DTO 已建立在：

```text
Models/DTO
```

目前包含：

- `UserDtos.cs`
- `OrderDtos.cs`
- `OrderMemberDtos.cs`
- `PaymentDtos.cs`
- `AuditLogDtos.cs`
- `DashboardDtos.cs`

這些 DTO 對應目前 SQL Server 的五張核心表，另外補了首頁統計、排行榜、月收入查詢會用到的回傳模型。

## Entity 檔案

EF Core 風格 Entity 已建立在：

```text
Models/Entities
```

目前包含：

- `User.cs`
- `Order.cs`
- `OrderMember.cs`
- `Payment.cs`
- `AuditLog.cs`

Entity 使用 navigation properties 保留資料表關聯，後續可直接接 `DbContext` 的 Fluent API 設定。

## DbContext

EF Core DbContext 已建立在：

```text
Data/EAPlaymateGroupDbContext.cs
```

需要 NuGet package：

```text
Microsoft.EntityFrameworkCore.SqlServer
```

ASP.NET Core 註冊範例：

```csharp
builder.Services.AddDbContext<EAPlaymateGroupDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

## Web API

ASP.NET Core Web API 專案已建立。

啟動：

```powershell
dotnet run
```

預設網址：

```text
http://localhost:5177
```

管理畫面：

```text
http://localhost:5177/
```

前端檔案：

- `wwwroot/index.html`
- `wwwroot/styles.css`
- `wwwroot/app.js`

第一批 API：

- `GET /api/health`
- `GET /api/users`
- `GET /api/users/{id}`
- `GET /api/users/players`
- `GET /api/users/bosses`
- `POST /api/users`
- `PUT /api/users/{id}`
- `POST /api/users/{id}/deactivate`
- `POST /api/users/{id}/activate`
- `POST /api/users/{id}/leave`
- `GET /api/orders`
- `GET /api/orders/{id}`
- `POST /api/orders`
- `PUT /api/orders/{id}`
- `POST /api/orders/{id}/cancel`
- `POST /api/orders/{id}/status`
- `POST /api/orders/{id}/customer-payment-status`
- `GET /api/dashboard/summary`
- `GET /api/dashboard/ranking`
- `GET /api/payments`
- `GET /api/payments/{id}`
- `POST /api/payments/generate-monthly`
- `PUT /api/payments/{id}`
- `POST /api/payments/{id}/mark-paid`
- `GET /api/auditlogs`
- `GET /api/auditlogs/{id}`
- `POST /api/auditlogs`

## Smoke Test

端到端 API 測試腳本：

```powershell
.\Scripts\api-smoke-test.ps1
```

腳本會建立 `E2E_` 前綴的測試團員、老闆、訂單，並產生當月月結，用來確認整條 API 流程正常。

目前會自動寫入 audit log 的操作：

- 新增使用者
- 修改使用者
- 啟用使用者
- 停用使用者
- 使用者離團
- 新增訂單
- 修改訂單
- 取消訂單
- 修改訂單狀態
- 修改客戶付款狀態
- 產生月結薪資
- 修改發薪紀錄
- 標記已發薪

## Error Format

API 的一般驗證錯誤會回傳：

```json
{
  "code": "validation_error",
  "message": "Validation failed.",
  "errors": {
    "nickname": ["Nickname is required."]
  }
}
```

領域狀態值集中在：

```text
Common/DomainValues.cs
```

## 價目表與服務項目

價目表圖片放在：

```text
wwwroot\價目表
```

目前服務頁會展示下列圖片：

```text
wwwroot\價目表\預存.jpg
wwwroot\價目表\代打.jpg
wwwroot\價目表\陪玩.png
wwwroot\價目表\代肝.jpg
```

後台左側「服務」頁會先顯示圖片版價目表，下面再顯示資料庫中的文字版服務清單。
「禮物」已獨立成左側頁面，訂單與禮物流程分開處理。
左側「禮物」頁會展示：

```text
wwwroot\價目表\禮物.png
```

禮物頁目前包含兩種資料：

```text
service_items
gift_records
```

`service_items` 裡的 `category = gift` 是禮物價目表。
`gift_records` 是實際送禮 / 打賞紀錄，用來記：

```text
日期
哪個老闆送
送給哪個團員
送什麼禮物或打賞
金額
數量
是否收款
備註
```

所以如果老闆送的是價目表禮物，就選禮物項目；如果是直接給某個陪陪多少錢，就選「自訂打賞」並輸入名稱與金額。

## 組織與部門

左側「組織」頁用來管理部門與部門成員。

目前不做 KPI、考核、獎懲制度，只先記錄組織歸屬。

資料表：

```text
departments
department_members
```

`departments` 存部門資料：

```text
部門名稱
英文名稱
職責說明
排序
啟用狀態
```

`department_members` 存成員歸屬：

```text
部門
成員
職稱
是否主管
加入時間
離開時間
```

初始化會先建立下列部門：

```text
管理層
營運部
人資部
客服部
陪玩部
財務部
行銷部
美術設計部
資訊部
品管部
商務部
數據分析部
```

服務項目資料表：

```text
service_items
```

目前初始化會自動匯入：

- 代打：段位、牌子
- 代肝：週上限、低保、週常、刷敗場
- 陪玩：娛樂陪、技術陪、教學陪
- 禮物：香氛蠟燭、星空瓶、糖果罐、愛心早餐、小鹿抱枕、巴斯克蛋糕
- 預存：1000、5000、10000 以上福利

API：

```text
GET /api/serviceitems
```

下一階段會把非禮物類的 `service_items` 接進訂單明細，讓新增訂單可以選服務項目、數量、指定團員，並自動加總金額。

### 服務項目規則

服務清單頁目前用四個分類按鈕切換，禮物改到左側「禮物」頁：

```text
代打 / 代肝 / 陪玩 / 預存
```

表格欄位固定為：

```text
類型 | 項目 | 價格 | 單位 | 備註 | 狀態
```

因為已經有「單位」欄，價格欄原則上只顯示數字：

```text
價格  單位
200   週
25    日
100   小時 / 人
20    星
```

特殊價格可以保留文字，例如：

```text
另議
存 1000 得 1100
存 5000 得 6000
```

代打規則：

```text
段位 1-3 階：20 / 星
段位 3-4 階：40 / 星
段位 4-5 階：60 / 星
段位 5-6 階：100 / 星
段位 7 階：110 / 星
角色代打 / 牌子：另議
```

`代打 - 段位` 這種泛用另議項目已停用，畫面只顯示分階段位價格。

代肝週上限備註：

```text
每 1w：週末單改每 1w +150
4.2w 打滿：不接週末單
5.4w 季末倒數打滿：季末週末單 +500；不接週日單
```

## Windows Installer

本專案可以使用 Inno Setup 打包成 Windows 安裝檔。

打包指令：

```powershell
cd /d E:\ling\EA_Playmate_Group
PowerShell -ExecutionPolicy Bypass -File .\Scripts\Build-Installer.ps1
```

輸出位置：

```text
artifacts\installer\EAPlaymateGroup_Setup_0.1.0.exe
```

安裝包設定檔：

```text
installer\EAPlaymateGroup.iss
```

版本號在 `installer\EAPlaymateGroup.iss` 的 `MyAppVersion` 調整。

更新方式：

1. 修改程式。
2. 調整 `MyAppVersion`。
3. 執行 `Scripts\Build-Installer.ps1`。
4. 將新的 `EAPlaymateGroup_Setup_x.x.x.exe` 給使用者安裝覆蓋。

更新安裝時會保留已安裝資料夾內既有的 `appsettings.json`，避免覆蓋資料庫連線設定。

資料本身存在 SQL Server 的 `EAPlaymateGroup` 資料庫，重新安裝或更新程式不會清空資料庫。

## Service Layer

目前已建立：

- `Services/ServiceResult.cs`
- `Services/UserService.cs`
- `Services/UserMapper.cs`
- `Services/OrderService.cs`
- `Services/OrderMapper.cs`
- `Services/PaymentService.cs`
- `Services/PaymentMapper.cs`
- `Services/AuditLogWriter.cs`

`OrdersController` 的寫入流程已改成呼叫 `OrderService`，Controller 保留 HTTP request/response 與查詢端點。
`PaymentsController` 的月結、修改發薪、標記已發薪流程已改成呼叫 `PaymentService`。
`UsersController` 的新增、修改、啟用、停用、離團流程已改成呼叫 `UserService`。
