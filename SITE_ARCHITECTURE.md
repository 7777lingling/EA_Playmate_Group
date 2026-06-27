# EA Playmate Group 網站架構分析

## 1. 架構總覽

本專案是 ASP.NET Core + EF Core + 原生前端的管理後台。

整體結構是：

```text
Browser
  -> wwwroot/index.html + styles.css + app.js
  -> /api/* JSON API
  -> Controllers
  -> Services
  -> EAPlaymateGroupDbContext
  -> SQL Server
```

前端是單頁式管理介面，但不是 React/Vue 類框架。所有畫面都預先寫在 `wwwroot/index.html`，透過 `wwwroot/app.js` 切換 `.view`、呼叫 API、渲染 table/form/modal。

後端是傳統分層：

- `Controllers/`：HTTP API、權限標記、HTTP response。
- `Services/`：商業邏輯、資料建立/更新、Audit Log/Money Log 寫入。
- `Models/Entities/`：EF Core 實體。
- `Models/DTO/`：API request/response DTO。
- `Data/EAPlaymateGroupDbContext.cs`：資料表 mapping、關聯、索引、租戶過濾、SaveChanges stamp。
- `Common/`：權限、ProblemDetails、例外處理、API metadata。

## 2. 前端架構

主要檔案：

- `wwwroot/index.html`：所有後台 view、modal、form、table 的 HTML。
- `wwwroot/app.js`：前端狀態、API 呼叫、事件綁定、畫面渲染。
- `wwwroot/styles.css`：後台版面、RWD、表格、modal、表單樣式。
- `wwwroot/價目表/*`：服務價目圖片。

目前主要 view：

| View | DOM id | 用途 |
|---|---|---|
| 總覽 | `dashboardView` | 今日營收、本月營收、本月團抽、待收款、排行榜、近期訂單 |
| 成員 | `usersView` | 團員/老闆資料維護 |
| 帳號管理 | `loginUsersView` | 後台登入帳號、角色、綁定成員、停用/啟用 |
| 組織 | `organizationView` | 組織、部門、部門成員編制 |
| 服務 | `servicesView` | 價目圖片、服務項目清單、點單帶入訂單 |
| 送禮紀錄 | `giftRecordsView` | 禮物/打賞紀錄 |
| 訂單 | `ordersView` | 訂單、分潤、收款狀態 |
| 月結 | `paymentsView` | 產生月結、發薪紀錄、標記已發薪 |
| 紀錄 | `auditView` | 操作紀錄 |
| 金流紀錄 | `moneyLogsView` | 儲值、扣款、退款、禮物收入、月結、手動調帳 |
| 權限管理 | `permissionsView` | 角色權限矩陣、組織清單 |

前端狀態集中在 `app.js` 的 `state`：

```js
{
  users,
  loginUsers,
  serviceItems,
  giftRecords,
  departments,
  players,
  bosses,
  orders,
  payments,
  auditLogs,
  moneyLogs,
  permissionMatrix,
  organizations,
  view,
  auth
}
```

前端流程：

1. `DOMContentLoaded`
2. 綁定側欄、手機導覽、表單、modal、picker
3. `initializeAuth()` 呼叫 `/api/auth/me`
4. 依登入狀態顯示 login 或 main
5. `applyNavigationPermissions()` 根據登入者權限隱藏 nav
6. `refreshAll()` 依目前 `state.view` 載入該頁需要的 API

## 3. 後端入口與 Middleware

入口是 `Program.cs`。

主要設定：

- Console logging。
- Data Protection key 存在 `DataProtectionKeys/`。
- Session cookie：`.EAPlaymateGroup.Session`，HttpOnly，8 小時。
- SQL Server：`ConnectionStrings:DefaultConnection`。
- Swagger：開發環境或 `Swagger:Enabled=true` 時啟用。
- Static files：`wwwroot`。
- 全域錯誤格式：`application/problem+json`。

API 存取流程：

1. `/api/health` 與 `[PublicApi]` 不需要登入。
2. 其他 `/api/*` 先檢查是否需要登入。
3. 若需要登入但 Session 沒有 `SessionUserId`，回 401。
4. 若已登入，必須有 `[RequirePermission]`。
5. `PermissionService.HasPermissionAsync()` 通過才執行 Controller。
6. 啟動時 `ValidateControllerAccessMetadata()` 強制每個 Controller action 必須有 `[PublicApi]` 或 `[RequirePermission]`，避免漏權限。

## 4. API 與 Controller 分工

| Controller | Route | 主要權限 | 職責 |
|---|---|---|---|
| `AuthController` | `/api/auth` | Public | 登入、登出、Discord OAuth、密碼變更、目前登入者 |
| `DashboardController` | `/api/dashboard` | `Order.View` | 營收摘要、排行榜 |
| `UsersController` | `/api/users` | `Member.*` | 成員、團員、老闆 CRUD |
| `LoginUsersController` | `/api/loginusers` | `Account.Manage` | 後台登入帳號 CRUD、啟用/停用 |
| `OrganizationsController` | `/api/organizations` | `Organization.Manage` | 組織管理 |
| `DepartmentsController` | `/api/departments` | `Organization.Manage` | 部門與部門成員 |
| `ServiceItemsController` | `/api/serviceitems` | `Gift.View` | 服務價目項目查詢 |
| `GiftRecordsController` | `/api/giftrecords` | `Gift.*` | 送禮/打賞紀錄 |
| `OrdersController` | `/api/orders` | `Order.*` | 訂單、狀態、收款狀態、取消/刪除 |
| `PaymentsController` | `/api/payments` | `Settlement.*` | 月結產生、發薪、標記已發薪 |
| `AuditLogsController` | `/api/auditlogs` | `Audit.View` | 操作紀錄查詢 |
| `MoneyLogsController` | `/api/moneylogs` | `Audit.View`, `Settlement.Close` | 金流紀錄查詢與手動建立 |
| `PermissionsController` | `/api/permissions` | `Account.Manage` | 角色權限矩陣 |

## 5. Service 分工

| Service | 職責 |
|---|---|
| `AuthService` | 登入、Session user DTO、Discord 綁定、密碼變更、登入/登出紀錄 |
| `LoginUserService` | 登入帳號新增/修改/停用/啟用、重設密碼 |
| `UserService` | 成員資料、離團、啟用/停用 |
| `OrderService` | 訂單建立/修改/取消、分潤處理、Audit Log |
| `GiftRecordService` | 送禮紀錄，並寫入禮物收入 Money Log |
| `PaymentService` | 月結產生、標記已發薪，並寫入月結 Money Log |
| `MoneyLogService` | 金流流水、餘額計算、手動調帳 |
| `DepartmentService` | 部門與部門成員管理 |
| `PermissionService` | 權限矩陣與權限檢查 |
| `PasswordHasher` | 密碼 hash/verify |
| `*Mapper` | Entity 與 DTO 轉換 |
| `AuditLogWriter` | 建立 AuditLog entity，序列化 before/after JSON |

目前商業邏輯大多在 Service；Controller 大多只負責 request/response 與錯誤轉換，這是合理分層。

## 6. 資料模型與關聯

核心資料表：

| Entity | Table | 說明 |
|---|---|---|
| `Organization` | `organizations` | 組織/租戶 |
| `LoginUser` | `login_users` | 後台登入帳號 |
| `User` | `users` | 業務成員，包含團員/老闆身分 |
| `Department` | `departments` | 部門 |
| `DepartmentMember` | `department_members` | 部門成員編制 |
| `ServiceItem` | `service_items` | 服務/價目項目 |
| `Order` | `orders` | 訂單主檔 |
| `OrderMember` | `order_members` | 訂單分潤成員 |
| `GiftRecord` | `gift_records` | 禮物/打賞紀錄 |
| `Payment` | `payments` | 月結/發薪紀錄 |
| `AuditLog` | `audit_logs` | 操作紀錄 |
| `MoneyLog` | `money_logs` | 金流流水 |
| `RolePermission` | `role_permissions` | 角色權限 |

重要關聯：

- `LoginUser.UserId -> User.Id`：登入帳號可綁定一個成員。
- `Order.OwnerUserId -> User.Id`：訂單老闆。
- `OrderMember.OrderId -> Order.Id`。
- `OrderMember.UserId -> User.Id`。
- `GiftRecord.BossUserId -> User.Id`。
- `GiftRecord.RecipientUserId -> User.Id`。
- `Payment.UserId -> User.Id`。
- `AuditLog.LoginUserId -> LoginUser.Id`。
- `AuditLog.UserId -> User.Id`。
- `MoneyLog.UserId -> User.Id`。
- `MoneyLog.LoginUserId -> LoginUser.Id`。

## 7. 多組織與資料隔離

所有主要 entity 都實作或使用 `IOrganizationScoped` 概念，透過 `organization_id` 分組。

`EAPlaymateGroupDbContext` 會從 Session 讀：

- `SessionOrganizationId`
- `SessionSystemRole`
- `SessionMemberUserId`
- `SessionUserId`

再套用 EF Core query filter：

- `admin` 可跨組織看資料。
- `staff` 只能看自己組織。
- `viewer` 只看和自己成員身分相關的資料，例如自己的訂單、月結、金流。

`SaveChanges` / `SaveChangesAsync` 會做兩件事：

1. 新增 `IOrganizationScoped` entity 時自動補 `OrganizationId`。
2. 新增 `AuditLog` / `MoneyLog` 時自動補 `LoginUserId`，AuditLog 也會補 IP。

這讓資料隔離和操作者 stamp 集中在 DbContext。

## 8. 權限模型

目前角色：

- `admin`
- `staff`
- `viewer`

權限碼集中在 `Common/PermissionCodes.cs`：

```text
Member.View / Create / Edit / Delete
Gift.View / Create / Edit / Delete
Order.View / Create / Edit / Cancel
Settlement.View / Close / Export
Account.Manage
Organization.Manage
Audit.View
```

後端靠 `[RequirePermission("...")]` 控制 API。

前端靠 `applyNavigationPermissions()` 和 `applyActionPermissions()` 隱藏：

- 側欄頁面
- 表單
- 編輯/刪除/標記已發薪等操作按鈕
- 紀錄頁內的操作紀錄/金流紀錄切換列

注意：前端隱藏只是 UX，真正權限以後端 middleware 為準。

## 9. 操作紀錄與金流紀錄

目前已拆成兩條資料流。

### 操作紀錄 Audit Log

資料表：`audit_logs`

用途：

- 登入
- 登出
- 新增/修改/刪除成員
- 帳號管理
- 訂單管理
- 權限修改
- 部門/組織管理
- 月結產生、發薪標記

主要欄位：

- `created_at`
- `login_user_id`
- `action`
- `target_type`
- `target_id`
- `before_json`
- `after_json`
- `ip_address`

前端顯示：

```text
時間 / 操作者 / 功能 / 動作 / 內容
```

### 金流紀錄 Money Log

資料表：`money_logs`

用途：

- 儲值
- 扣款
- 退款
- 禮物收入
- 月結
- 手動調帳

主要欄位：

- `created_at`
- `user_id`
- `type`
- `amount`
- `balance_after`
- `source_type`
- `source_id`
- `note`

`MoneyLogService` 會用同一會員上一筆 `balance_after` 計算新餘額。

前端顯示：

```text
時間 / 會員 / 類型 / 金額 / 餘額 / 來源 / 備註
```

## 10. 主要業務流程

### 登入

```text
POST /api/auth/login
  -> AuthService.LoginAsync
  -> SignIn 寫 Session
  -> AuthService.RecordAuthEventAsync(login)
  -> audit_logs
```

Discord 登入走 `/api/auth/discord/login` 與 `/auth/discord/callback`。

### 新增訂單

```text
ordersView form
  -> POST /api/orders
  -> OrderService.CreateAsync
  -> orders + order_members
  -> audit_logs(create, orders)
```

服務頁可用「點單」帶入訂單表單。

### 送禮紀錄

```text
giftRecordsView form
  -> POST /api/giftrecords
  -> GiftRecordService
  -> gift_records
  -> money_logs(type=gift_income)
  -> audit_logs
```

送禮金額會成為收禮成員的金流收入。

### 月結

```text
paymentsView
  -> POST /api/payments/generate-monthly
  -> PaymentService.GenerateMonthlyAsync
  -> payments
  -> audit_logs(generate_monthly)

mark-paid
  -> POST /api/payments/{id}/mark-paid
  -> PaymentService.MarkPaidAsync
  -> money_logs(type=monthly_settlement, amount negative)
```

## 11. 部署與設定

主要設定檔：

- `appsettings.json`
- `appsettings.example.json`
- `CONFIGURATION.md`
- `IIS_DEPLOYMENT.md`
- `installer/EAPlaymateGroup.iss`

資料庫：

- EF Core mapping 在 `EAPlaymateGroupDbContext.cs`。
- SQL script 在 `database/`。
- 啟動時 `DatabaseSchemaInitializer` 會補驗證 auth 欄位與 organization filter。

API 契約：

- `API_CONTRACT.md` 定義錯誤格式與 Swagger 啟用方式。
- 所有 API 錯誤統一為 `application/problem+json`。

## 12. 架構觀察與注意事項

目前架構清楚，適合中小型後台。主要優點：

- API 權限 metadata 強制檢查，降低漏標權限風險。
- 多組織資料隔離集中在 DbContext query filter。
- Audit Log 與 Money Log 已拆分，資料語意比較乾淨。
- Service 層承接多數商業邏輯，Controller 沒有過度肥大。
- 前端無框架，部署簡單，靜態檔即可。

目前主要風險：

- `wwwroot/app.js` 已經很大，所有 view、render、form submit、modal、picker 都在同一檔，後續維護成本會升高。
- 前端 state 沒有模組化，跨頁資料刷新靠 `refreshAll()` 判斷，頁面增加後容易互相影響。
- 前端權限與後端權限各自維護一份 mapping，需要小心同步。
- DbContext query filter 依賴 Session 狀態，背景任務或非 HTTP context 使用 DbContext 時要特別處理。
- Money Log 的餘額是用會員最後一筆流水推算，若未來有併發調帳，需要交易鎖或資料庫層級保護。
- `AuditLog` 的 `before_json` / `after_json` 可追資料，但目前前端只顯示簡短 note，若要做稽核比對，可能需要專門的明細檢視。

## 13. 建議後續整理方向

短期：

- 將 `app.js` 依功能拆成 `auth.js`、`navigation.js`、`orders.js`、`gifts.js`、`logs.js`、`shared-ui.js`。
- 將前端 permission/view mapping 抽成單一物件，避免散落。
- 補 `MoneyLog` 手動儲值/扣款/退款 UI，如果金流紀錄要由後台操作。
- 操作紀錄可增加目標摘要，例如「會員：00001」或「訂單：xxx」。

中期：

- 為金流新增交易邊界，避免同會員同時寫入時餘額錯算。
- 為 Audit Log 建立更完整的 change detail renderer。
- 將 API contract 補成 endpoint 層級文件。
- 補服務層測試，尤其是訂單分潤、月結、金流餘額。

長期：

- 若前端繼續變大，可考慮導入 Vite + TypeScript，至少先做模組化與型別保護。
- 若要多客戶/多組織正式化，需要補 organization admin、跨組織操作稽核、資料匯出權限。
