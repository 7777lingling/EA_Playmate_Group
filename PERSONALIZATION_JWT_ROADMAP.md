# 個人化與 JWT 分頁準備路線圖

這份文件整理後台個人化與未來拆分頁面、改 JWT 的建議順序。重點是先做最划算、風險最低的後台體驗改善，再逐步準備分頁與 Token 驗證。

## 1. 個人化優先策略

不要一開始做太複雜。第一階段先做「每個登入帳號自己的後台體驗」，不要先拆整個前端架構。

### 第一階段功能

#### 個人化配色

每個登入帳號可選主題色，例如：

- 紫色科技
- 藍色金屬
- 淺色簡潔

建議存放位置：

- 優先：`user_preferences`
- 不建議：全部塞進 `users`
- 可接受但不推薦：塞進 `login_users`，未來欄位會膨脹

#### Dashboard 個人化

不同角色看到不同卡片。

管理員 / 團長：

- 營收
- 月結
- 未付款
- 操作紀錄

財務：

- 金流紀錄
- 月結
- 沖正紀錄

營運：

- 訂單
- 會員
- 評論
- 排班

一般查看者：

- 只看被允許的資料

#### 表格欄位與偏好記憶

範例：

- 操作紀錄預設顯示 100 筆
- 金流紀錄預設排序 newest first
- 登入紀錄只看自己的登入紀錄

#### 常用篩選記憶

範例：

- 訂單狀態
- 金流月份
- 操作人
- 登入成功 / 失敗

## 2. 建議資料表

先加一張乾淨的偏好設定表。

### `user_preferences`

欄位建議：

```sql
id
login_user_id
theme_name
accent_color
dashboard_layout
table_page_size
default_order_status_filter
default_money_log_filter
created_at
updated_at
```

### 設計原則

- `login_user_id` 一對一對應登入帳號。
- 主題、Dashboard、表格偏好先集中在這張表。
- JSON 類欄位可以用在布局與複合設定，例如 `dashboard_layout`。
- 常用的查詢條件才獨立欄位，例如 `table_page_size`。

## 3. 未來分頁面方向

目前是單頁式：

```text
index.html
app.js
styles.css
```

未來可拆成：

```text
/login.html
/dashboard.html
/orders.html
/payments.html
/audit-logs.html
/money-logs.html
/login-history.html
/settings.html
```

## 4. JWT 前端規則

登入成功後儲存 token：

```js
localStorage.setItem("token", result.token);
```

每次呼叫 API 都帶：

```http
Authorization: Bearer <token>
```

前端路由規則：

- 沒 token：跳回 `/login.html`
- API 回 401：清掉 token，跳回 `/login.html`
- API 回 403：顯示權限不足，不跳登入

## 5. JWT 後端準備

需要新增或調整：

- `AuthController`
- `LoginRequest`
- `LoginResponse`
- `JwtTokenService`
- `PasswordHashService`

登入 API：

```http
POST /api/auth/login
```

回傳：

```json
{
  "token": "...",
  "user": {
    "id": 1,
    "username": "00001",
    "role": "admin"
  }
}
```

Controller 驗證：

```csharp
[Authorize]
```

角色限制：

```csharp
[Authorize(Roles = "admin")]
```

現有 `[RequirePermission]` 之後可以改成讀 JWT claims，而不是讀 Session。

## 6. 建議實作順序

最佳順序：

1. 先做 `login.html`
2. 做 `/api/auth/login`
3. JWT 登入成功回 token
4. 前端 `fetch` 統一帶 `Authorization`
5. API 全部加 `[Authorize]`
6. 再做 `user_preferences`
7. 最後拆 `dashboard/orders/payments/audit` 等頁面

## 7. 為什麼不要先拆頁面

如果先拆頁面但還沒 JWT：

- 每個 HTML 都可能直接被開啟。
- 權限判斷會分散在各頁。
- Session Cookie 與多頁面跳轉會比較難清楚控制。
- 之後再換 JWT 時，每頁都要重補 auth guard。

所以建議先把登入驗證與 Token 規則補好，再拆頁面。

## 8. 現階段建議

短期最划算：

1. 新增 `user_preferences`
2. 做主題設定 UI
3. 讓 Dashboard 根據角色與偏好顯示不同卡片
4. 讓表格 page size / sort / filters 能記憶

中期：

1. 新增 JWT login flow
2. 讓現有 SPA 可同時支援 Bearer token
3. 將 Session middleware 漸進替換成 JWT / claims

長期：

1. 拆成多 HTML 頁
2. 拆 `app.js` 成各頁模組
3. 將共用 API client、auth guard、theme loader 抽出

## 9. 與 CSS 個人化文件的關係

配色與 UI token 請參考：

- `CSS_CUSTOMIZATION_GUIDE.md`

建議做法：

- `user_preferences.theme_name` 對應一組預設主題。
- `user_preferences.accent_color` 可覆寫 `--accent`。
- 前端登入後讀偏好，將 CSS variables 寫到 `document.documentElement.style`。
