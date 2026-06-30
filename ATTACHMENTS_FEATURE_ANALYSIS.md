# 檔案附件功能需求分析

## 1. 設計原則

- 附件採共用資料表，不在各模組主表重複新增 `xxx_file`、`xxx_image` 欄位。
- 關聯方式採 `TargetType + TargetId`，並保留 `TargetUuid` 方便稽核、匯出與跨環境追蹤。
- 「必填」應只套用在需要作為憑證、證據、對帳依據的情境；一般補充資料維持選填，避免日常作業成本過高。
- 檔案格式分層控管：圖片與 PDF 可直接預覽，Office/CSV 供文件與對帳使用，壓縮檔只給少數內部文件情境使用。
- 上傳、下載、刪除都必須寫入操作紀錄，並受組織 `organization_id` 隔離。

## 2. 模組附件需求矩陣

| 功能/情境 | 是否需要附件 | 必填/選填 | 附件用途 | 建議格式 | 多檔 |
|---|---:|---|---|---|---:|
| 訂單：建立草稿/一般完成訂單 | 需要 | 選填 | 客戶需求截圖、聊天室紀錄、報價依據、遊戲進度截圖 | jpg, jpeg, png, webp, pdf | 是 |
| 訂單：完成代打/代肝類服務 | 需要 | 選填，若公司規範要求驗收則可改必填 | 完成前後截圖、交付證明、驗收紀錄 | jpg, jpeg, png, webp, mp4, pdf | 是 |
| 訂單：取消訂單 | 需要 | 選填 | 取消原因截圖、客戶同意紀錄、退款溝通紀錄 | jpg, jpeg, png, webp, pdf | 是 |
| 訂單：爭議/客訴狀態 | 需要 | 必填 | 爭議證據、違規證據、雙方對話、處理依據 | jpg, jpeg, png, webp, pdf, mp4, txt | 是 |
| 訂單：客戶付款狀態改為已付款/部分付款 | 需要 | 必填 | 收款證明、轉帳截圖、第三方支付截圖、對帳紀錄 | jpg, jpeg, png, webp, pdf | 是 |
| 禮物/送禮紀錄：新增紀錄 | 需要 | 選填 | 禮物畫面、聊天室紀錄、金額來源截圖 | jpg, jpeg, png, webp, pdf | 是 |
| 禮物/送禮紀錄：客戶付款狀態改為已付款/部分付款 | 需要 | 必填 | 收款證明、禮物付款證明、平台交易截圖 | jpg, jpeg, png, webp, pdf | 是 |
| 禮物/送禮紀錄：取消紀錄 | 需要 | 選填 | 取消原因、補償或退款溝通紀錄 | jpg, jpeg, png, webp, pdf | 是 |
| 金流紀錄：儲值 | 需要 | 必填 | 入金證明、銀行/第三方支付收款截圖、人工補登依據 | jpg, jpeg, png, webp, pdf, csv | 是 |
| 金流紀錄：扣款 | 需要 | 選填 | 扣款依據、服務交付證明、人工調整原因 | jpg, jpeg, png, webp, pdf | 是 |
| 金流紀錄：退款 | 需要 | 必填 | 退款證明、匯款截圖、退款申請單 | jpg, jpeg, png, webp, pdf | 是 |
| 金流紀錄：手動調帳 | 需要 | 必填 | 調帳核准紀錄、錯帳證明、主管簽核截圖 | jpg, jpeg, png, webp, pdf, xlsx, csv | 是 |
| 金流紀錄：禮物收入/月結自動產生流水 | 需要 | 選填 | 追溯來源憑證；通常由來源單據附件承擔，流水本身可補附件 | jpg, jpeg, png, webp, pdf | 是 |
| 月結：產生月結 | 需要 | 選填 | 月結試算表、結算明細、匯出報表快照 | pdf, xlsx, csv | 是 |
| 月結：標記已發薪/已付款 | 需要 | 必填 | 發薪證明、匯款截圖、批次付款檔、銀行回條 | jpg, jpeg, png, webp, pdf, xlsx, csv | 是 |
| 月結：修改結算紀錄 | 需要 | 選填，若調整金額則建議必填 | 調整原因、主管核准、差額計算表 | jpg, jpeg, png, webp, pdf, xlsx, csv | 是 |
| 團員/老闆資料：新增/編輯 | 需要 | 選填 | 身分或聯絡資料佐證、銀行帳號截圖、合約/同意書 | jpg, jpeg, png, webp, pdf | 是 |
| 團員：停用/離團 | 需要 | 選填，若違規離團則必填 | 離團申請、違規證據、交接紀錄 | jpg, jpeg, png, webp, pdf, txt | 是 |
| 帳號管理：新增/停用後台帳號 | 需要 | 選填 | 帳號申請單、權限核准、停用依據 | jpg, jpeg, png, webp, pdf | 是 |
| 權限管理：調整角色權限 | 需要 | 選填，正式簽核流程可改必填 | 權限申請單、主管核准紀錄 | jpg, jpeg, png, webp, pdf | 是 |
| 問題回報：Bug/錯誤 | 需要 | 必填 | 問題截圖、錯誤訊息、重現影片、瀏覽器 console 截圖 | jpg, jpeg, png, webp, mp4, txt, log | 是 |
| 問題回報：需求建議/一般諮詢 | 需要 | 選填 | 參考圖、需求草稿、補充文件 | jpg, jpeg, png, webp, pdf, docx | 是 |
| 操作紀錄：稽核事件 | 需要 | 選填 | 補充稽核證據、處理說明附件；系統自動紀錄本身不應要求附件 | jpg, jpeg, png, webp, pdf, txt | 是 |
| 登入紀錄：登入異常/資安事件 | 需要 | 選填，若建立資安事件單則必填 | 異常登入截圖、IP 查詢、處理紀錄 | jpg, jpeg, png, webp, pdf, txt, log | 是 |
| 公告：一般文字公告 | 需要 | 選填 | 公告圖片、活動 Banner、說明文件 | jpg, jpeg, png, webp, gif, pdf | 是 |
| 公告：制度/SOP/合約公告 | 需要 | 必填 | SOP、制度文件、合約範本、教育訓練文件 | pdf, docx, xlsx, pptx | 是 |
| 部門：部門資料 | 需要 | 選填 | 部門職責說明、SOP、組織圖、交接文件 | jpg, jpeg, png, webp, pdf, docx, xlsx, pptx | 是 |
| 部門成員：任命/主管異動 | 需要 | 選填，若正式任命流程可改必填 | 任命單、職務說明、核准紀錄 | jpg, jpeg, png, webp, pdf | 是 |
| 組織：組織資料 | 需要 | 選填 | 公司/團隊文件、品牌圖、規章 | jpg, jpeg, png, webp, pdf, docx | 是 |
| 服務項目/價目表 | 需要 | 選填 | 價目表圖片、服務說明圖、方案文件 | jpg, jpeg, png, webp, pdf, xlsx | 是 |
| 個人化設定 | 不建議 | 不適用 | 主題、版面偏好屬個人設定，不需附件 | 不適用 | 否 |

## 3. 建議 TargetType

現有後端已支援部分 `TargetType`：`users`、`login_users`、`orders`、`gift_records`、`payments`、`service_items`、`departments`。

建議擴充為：

| TargetType | 對應資料 | 用途 |
|---|---|---|
| `orders` | `orders.id` | 訂單證明、爭議、收款證明 |
| `gift_records` | `gift_records.id` | 禮物紀錄、送禮付款證明 |
| `money_logs` | `money_logs.id` | 儲值、退款、扣款、手動調帳憑證 |
| `payments` | `payments.id` | 月結、發薪、結算調整憑證 |
| `users` | `users.id` | 團員/老闆資料佐證 |
| `login_users` | `login_users.id` | 後台帳號與權限申請附件 |
| `problem_reports` | `problem_reports.id` | 問題截圖、錯誤 log、重現影片 |
| `audit_logs` | `audit_logs.id` | 稽核補充證據 |
| `login_histories` | `login_histories.id` | 登入異常與資安事件附件 |
| `announcements` | `announcements.id` | 公告圖片、SOP、制度文件 |
| `departments` | `departments.id` | 部門 SOP、組織圖 |
| `department_members` | `department_members.id` | 任命/職務異動證明 |
| `organizations` | `organizations.id` | 組織文件與品牌素材 |
| `service_items` | `service_items.id` | 價目表、服務說明素材 |

目前專案尚未看到 `problem_reports` 與 `announcements` 主表；若要做完整附件功能，應先補這兩個主功能的資料表與 API，再把它們加入附件允許清單。

## 4. 必填規則建議

附件是否必填不應只看模組，而要看「動作」與「狀態」。

| 規則代碼 | 套用情境 | 條件 | 最少附件數 |
|---|---|---|---:|
| `order_payment_paid` | 訂單付款狀態改為 `paid` 或 `partial` | `target_type = orders` 且付款狀態轉為已付款/部分付款 | 1 |
| `order_disputed` | 訂單狀態改為 `disputed` | `target_type = orders` 且狀態轉為爭議 | 1 |
| `gift_payment_paid` | 禮物付款狀態改為 `paid` 或 `partial` | `target_type = gift_records` | 1 |
| `money_deposit` | 金流儲值 | `target_type = money_logs` 且 `type = deposit` | 1 |
| `money_refund` | 金流退款 | `target_type = money_logs` 且 `type = refund` | 1 |
| `money_adjustment` | 手動調帳 | `target_type = money_logs` 且 `type = adjustment` 或人工建立 | 1 |
| `payment_mark_paid` | 月結標記已發薪/已付款 | `target_type = payments` 且狀態轉為 `paid` | 1 |
| `problem_bug` | 問題回報為 Bug/錯誤 | `target_type = problem_reports` 且分類為 bug/error | 1 |
| `announcement_sop` | 公告類型為制度/SOP/合約 | `target_type = announcements` 且類型為 sop/policy/contract | 1 |
| `disciplinary_member_leave` | 團員因違規停用/離團 | `target_type = users` 且原因分類為 violation | 1 |

實作上建議在各 Service 的狀態轉換流程驗證，而不是只在附件 API 驗證。原因是「附件必填」通常取決於業務動作，例如標記付款、調帳、建立 Bug 回報，不是單純上傳檔案本身可以判斷。

## 5. 共用 Attachments 資料表設計

現有 `file_attachments` 已接近需求，建議保留命名或建立新版 `attachments`。若要新設一套通用表，欄位如下：

```sql
CREATE TABLE dbo.attachments (
    id BIGINT IDENTITY(1,1) NOT NULL,
    organization_id INT NOT NULL,

    target_type NVARCHAR(50) NOT NULL,
    target_id INT NOT NULL,
    target_uuid UNIQUEIDENTIFIER NULL,

    attachment_kind NVARCHAR(30) NULL,
    original_file_name NVARCHAR(255) NOT NULL,
    stored_file_name NVARCHAR(120) NOT NULL,
    storage_path NVARCHAR(500) NOT NULL,
    content_type NVARCHAR(120) NOT NULL,
    file_extension NVARCHAR(20) NULL,
    file_size BIGINT NOT NULL,
    sha256_hash CHAR(64) NULL,

    uploaded_by_login_user_id INT NULL,
    note NVARCHAR(500) NULL,
    is_deleted BIT NOT NULL CONSTRAINT DF_attachments_is_deleted DEFAULT 0,
    deleted_at DATETIME2 NULL,
    deleted_by_login_user_id INT NULL,

    created_at DATETIME2 NOT NULL CONSTRAINT DF_attachments_created_at DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_attachments PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_attachments_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
    CONSTRAINT FK_attachments_uploaded_by FOREIGN KEY (uploaded_by_login_user_id) REFERENCES dbo.login_users(id),
    CONSTRAINT FK_attachments_deleted_by FOREIGN KEY (deleted_by_login_user_id) REFERENCES dbo.login_users(id),
    CONSTRAINT CK_attachments_target_type CHECK (LEN(target_type) > 0),
    CONSTRAINT CK_attachments_target_id CHECK (target_id > 0),
    CONSTRAINT CK_attachments_file_size CHECK (file_size > 0)
);

CREATE INDEX IX_attachments_target
ON dbo.attachments (organization_id, target_type, target_id, is_deleted, created_at DESC);

CREATE INDEX IX_attachments_target_uuid
ON dbo.attachments (organization_id, target_type, target_uuid)
WHERE target_uuid IS NOT NULL;

CREATE INDEX IX_attachments_uploaded_by
ON dbo.attachments (organization_id, uploaded_by_login_user_id, created_at DESC);
```

### 欄位說明

| 欄位 | 說明 |
|---|---|
| `organization_id` | 多組織資料隔離，必填 |
| `target_type` | 模組/資料表類型，例如 `orders`、`payments`、`money_logs` |
| `target_id` | 目標資料主鍵 |
| `target_uuid` | 目標資料 UUID；不是所有表都有 UUID，但有就寫入 |
| `attachment_kind` | 附件用途分類，例如 `payment_proof`、`issue_screenshot`、`sop`、`evidence` |
| `original_file_name` | 使用者上傳時的原始檔名 |
| `stored_file_name` | 系統儲存檔名，避免重名與路徑攻擊 |
| `storage_path` | 實體或物件儲存路徑 |
| `content_type` | MIME type |
| `file_extension` | 副檔名，供白名單與搜尋使用 |
| `file_size` | 檔案大小，建議預設單檔 25 MB，上限依情境調整 |
| `sha256_hash` | 防重、稽核與檔案完整性驗證 |
| `uploaded_by_login_user_id` | 上傳者 |
| `note` | 附件備註 |
| `is_deleted/deleted_at/deleted_by_login_user_id` | 軟刪除，保留稽核線索 |

## 6. 建議檔案白名單

| 類型 | 副檔名 | 用途 |
|---|---|---|
| 圖片 | jpg, jpeg, png, webp, gif | 收款證明、問題截圖、公告圖片、服務圖 |
| 文件 | pdf, docx, xlsx, pptx, csv | SOP、月結、對帳、簽核文件 |
| 文字/log | txt, log | 錯誤紀錄、重現資料、稽核補充 |
| 影片 | mp4, mov | 問題重現、交付證明、違規證據 |

建議預設單檔 25 MB；影片可針對 `problem_reports`、`orders`、`audit_logs` 放寬到 100 MB，但需要更嚴格權限與儲存容量控管。

## 7. 與現有程式的差距

- 現有 `file_attachments` 已是共用表模式，具備 `TargetType + TargetId + TargetUuid`。
- 現有可掛載目標缺少 `money_logs`、`audit_logs`、`login_histories`、`organizations`、`department_members`、`problem_reports`、`announcements`。
- 現有附件 API 是單檔上傳；可用多次上傳達成多檔，但前端可以提供多選檔案後逐一呼叫 API。
- 現有程式只限制大小，尚未看到副檔名/MIME 白名單、雜湊、軟刪除、附件用途分類。
- 現有程式沒有「業務動作必填附件」驗證，應補在 Order/Payment/MoneyLog/Gift/ProblemReport/Announcement 對應 Service。

## 8. 實作優先順序

1. 先沿用 `file_attachments`，補 `attachment_kind`、`file_extension`、`sha256_hash`、軟刪除欄位。
2. 擴充 TargetType 白名單與目標存在檢查，至少加入 `money_logs`、`audit_logs`、`login_histories`、`organizations`、`department_members`。
3. 前端在訂單、禮物、金流、月結、團員、部門、服務項目詳細/編輯畫面加入附件列表與多檔上傳。
4. 在狀態轉換 Service 補必填附件驗證：付款、退款、調帳、月結已付款、爭議、Bug 回報、SOP 公告。
5. 補 `problem_reports` 與 `announcements` 主功能後，再接入附件共用元件。

## 9. 附件入口實作位置

### 主要出入口調整

附件的主要操作入口調整為下列四個業務流程：

| 主要入口 | TargetType | 附件提交時機 | 後端處理 |
|---|---|---|---|
| 新增送禮紀錄 | `gift_records` | 建立送禮紀錄時可直接選 1 到多個附件；若收款狀態為 `paid` 或 `partial`，必須附檔。 | `GiftRecordsController.CreateGiftRecordMultipart(...)` 建立成功後呼叫 `GiftRecordService.CreateWithAttachmentsAsync(...)` 綁定附件。 |
| 訂單 | `orders` | 新增訂單時可直接選 1 到多個附件；若訂單狀態為 `disputed`，或收款狀態為 `paid` / `partial`，必須附檔。 | `OrdersController.CreateOrderMultipart(...)` 建立成功後呼叫 `OrderService.CreateOrderWithAttachmentsAsync(...)` 綁定附件。 |
| 月結 | `payments` | 月結標記已付款時選擇付款證明附件後送出。 | `PaymentsController.MarkPaidMultipart(...)` 先儲存附件，再呼叫 `PaymentService.MarkPaidWithAttachmentsAsync(...)` 完成標記付款。 |
| 金流紀錄 | `money_logs` | 不作為主要前端新增入口；金流附件應優先掛在來源業務，例如訂單、送禮、月結。若金流紀錄需要補依據，從金流明細補附件。 | 保留後端 `MoneyLogsController.CreateMultipart(...)` 能力，但前端不在操作紀錄頁提供新增金流表單。 |

其他模組例如團員、部門、服務項目仍保留共用附件面板，但不是目前主要上傳出入口；主要必填流程應以以上四個業務動作為準。

### 前端入口

| 模組 | 入口位置 | TargetType | 說明 |
|---|---|---|---|
| 金流明細 | `wwwroot/app.js`：`moneyLogAttachmentPanel` | `money_logs` | 僅用於既有金流紀錄的補充依據與追溯附件，不在操作紀錄頁新增金流。 |
| 金流明細 | `wwwroot/app.js`：`moneyLogAttachmentPanel` | `money_logs` | 已建立金流後，可查看、上傳、下載、刪除附件。 |
| 訂單明細/編輯 | `wwwroot/index.html`：`orderAttachmentPanel`；`wwwroot/app.js`：`renderAttachmentPanel(..., "orders", ...)` | `orders` | 用於付款證明、爭議證據、需求截圖等。 |
| 禮物紀錄明細/編輯 | `wwwroot/index.html`：`giftRecordAttachmentPanel`；`wwwroot/app.js`：`renderAttachmentPanel(..., "gift_records", ...)` | `gift_records` | 用於禮物付款證明、送禮截圖、交易紀錄等。 |
| 月結列表 | `wwwroot/app.js`：`data-payment-attachments` | `payments` | 月結列上提供附件按鈕，開啟共用附件 Modal。 |
| 團員明細/編輯 | `wwwroot/index.html`：`userAttachmentPanel`；`wwwroot/app.js`：`renderAttachmentPanel(..., "users", ...)` | `users` | 用於身分資料、違規證據、停用/離團依據等。 |
| 部門明細/編輯 | `wwwroot/index.html`：`departmentAttachmentPanel`；`wwwroot/app.js`：`renderAttachmentPanel(..., "departments", ...)` | `departments` | 用於部門 SOP、職責說明、交接文件等。 |
| 服務項目列表 | `wwwroot/app.js`：`data-service-attachments` | `service_items` | 服務項目列上提供附件按鈕，開啟共用附件 Modal。 |

### 共用前端元件

| 檔案 | 位置 | 說明 |
|---|---|---|
| `wwwroot/app.js` | `renderAttachmentPanel(...)` | 共用附件列表、單檔上傳、下載連結、軟刪除按鈕。 |
| `wwwroot/app.js` | `openAttachmentModal(...)` | 給列表型模組使用的附件彈窗，例如月結、服務項目。 |

### 後端 API 與服務

| 功能 | 檔案/方法 | 說明 |
|---|---|---|
| 金流 JSON 建立 | `Controllers/MoneyLogsController.cs`：`Create(...)` | 保留原本 JSON API；deposit/refund/manual_adjustment/adjustment 仍會因必填附件被擋下。 |
| 金流 multipart 建立 | `Controllers/MoneyLogsController.cs`：`CreateMultipart(...)` | 新增 `multipart/form-data` API，可同時提交金流欄位與 `attachments` 多檔。 |
| 金流建立後綁附件 | `Services/MoneyLogService.cs`：`AddManualWithAttachmentsAsync(...)` | 建立 MoneyLog 成功後，立即將附件存成 `target_type = money_logs`、`target_id = moneyLog.Id`。 |
| 共用附件儲存 | `Services/FileAttachmentService.cs`：`UploadManyAsync(...)` | 多檔附件儲存共用流程，包含副檔名、大小、hash、target、AuditLog。 |
| 共用附件 API | `Controllers/FileAttachmentsController.cs` | 提供附件上傳、列表、下載、預覽、軟刪除。 |

### 金流附件處理原則

1. 金流紀錄不放在操作紀錄頁做新增入口。
2. 付款或依據附件優先掛在來源業務：訂單、送禮紀錄、月結。
3. 系統自動產生的金流，例如送禮收入、月結扣款，追溯來源時應回到來源單據看附件。
4. 既有金流紀錄若需要補依據，只在金流明細使用共用附件面板補充。
5. 後端保留 `MoneyLogsController.CreateMultipart(...)` 給特殊管理流程使用，但不是目前主要前端出入口。
