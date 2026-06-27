# CSS 個人化整理

## Theme CSS Files

Current personalization is color-theme only. The base layout and components stay in `wwwroot/styles.css`; each selectable color version is split into its own file:

- `wwwroot/themes/purple-tech.css`: Cyber Violet / 霓紫科技
- `wwwroot/themes/blue-metal.css`: Aurora Blue / 極光金屬
- `wwwroot/themes/dopamine-candy.css`: Dopamine Candy / 甜感多巴胺
- `wwwroot/themes/mint-energy.css`: Mint Energy / 薄荷能量
- `wwwroot/themes/sunset-neon.css`: Sunset Neon / 落日霓虹
- `wwwroot/themes/light-clean.css`: Light Clean / 清透白

The page loads the active theme through `<link id="themeStylesheet">` in `wwwroot/index.html`. `wwwroot/app.js` switches this link when the user changes 個人化配色.

Theme background gradients are controlled by:

- `--app-background`: main authenticated admin background.
- `--login-background`: login page background.

To add a new theme:

1. Add a new file under `wwwroot/themes/`.
2. Define the same `:root` tokens as the existing theme files.
3. Add the option in `setupPersonalizationUI()` in `wwwroot/app.js`.
4. Add the theme file path in `themePreset()` in `wwwroot/app.js`.

此文件整理目前 `wwwroot/styles.css` 使用到的主要 CSS 架構，之後做個人化主題、配色、字體、密度調整時可先看這份。

## 主要檔案

- `wwwroot/styles.css`：全站樣式、元件、RWD、紀錄頁樣式。
- `wwwroot/index.html`：主要 DOM 結構與 class 使用位置。
- `wwwroot/app.js`：部分 UI 會動態插入 class，例如紀錄頁的 KPI、篩選列、右側 Drawer。

## 注意事項

`styles.css` 目前有兩段較大的樣式區：

- 前段：基礎 token、登入頁、側欄、表單、表格、Modal、紀錄頁新增樣式。
- 後段：新版視覺覆寫，包含按鈕、卡片、表格、Modal、RWD 等覆蓋規則。

若同一個 class 在前後段都出現，瀏覽器會以後面規則優先。個人化時建議：

1. 先改 `:root` token。
2. 若 token 不夠，再在檔案最後新增「Personal Theme」區塊覆寫。
3. 不建議直接分散修改多個同名 class，後續會難維護。

## Design Tokens

位於 `:root`。

### 背景與面板

- `--bg`：主背景色。
- `--bg-soft`：柔和背景色。
- `--panel`：主要卡片/面板背景。
- `--panel-subtle`：次要面板背景。
- `--glass`：玻璃感背景。
- `--glass-strong`：較實的玻璃背景。

### 文字

- `--ink`：主要文字。
- `--muted`：輔助文字、提示文字。

### 邊線

- `--line`：一般邊線。
- `--line-strong`：較明顯邊線。

### 品牌與重點色

- `--accent`：主要紫色。
- `--accent-dark`：主要色 hover/加深。
- `--accent-2`：第二重點色。
- `--accent-soft`：主要色淡背景。
- `--gold`：目前實際偏粉色，可改成金色或品牌輔色。
- `--gold-soft`：`--gold` 的淡背景。

### 狀態色

- `--warn` / `--warn-soft`：警告。
- `--danger` / `--danger-soft`：錯誤、刪除、支出。
- `--success` / `--success-soft`：成功、收入。

### 陰影與尺寸

- `--shadow`：主要陰影。
- `--shadow-soft`：輕陰影。
- `--sidebar-width`：側欄寬度。
- `--sidebar-collapsed-width`：側欄收合寬度。
- `--primary-gradient`：主按鈕/品牌漸層。

## 全域基礎

- `*`：`box-sizing: border-box`。
- `[hidden]`：強制隱藏。
- `body`：全站字體、背景、文字色。
- `button, input, select`：繼承字體。
- `.muted`：輔助文字。
- `.eyebrow`：小標/英文標籤。

## 按鈕

- `.primary`：主要操作按鈕。
- `.ghost`：次要按鈕。
- `.ghost.danger-action`：危險操作，例如刪除。
- `.ghost.danger`：金流沖正等危險操作。
- `.small`：小型按鈕。
- `.icon-btn`：圖示/小操作按鈕。

個人化建議：

- 主色改 `--accent`、`--accent-dark`、`--primary-gradient`。
- 危險色改 `--danger`。
- 若要整體更扁平，可降低 `box-shadow` 或調整 `--shadow-soft`。

## 主要版面

- `.sidebar`：左側主選單。
- `.brand` / `.brand-mark` / `.brand-copy`：品牌區。
- `.sidebar-toggle`：側欄收合。
- `.nav-tabs` / `.nav-tabs button`：主導航。
- `.main`：主內容區。
- `.topbar`：上方工具列。
- `.top-actions`：右上操作。
- `.account-actions`：帳號相關操作。
- `.mobile-nav-toggle` / `.mobile-more-toggle` / `.mobile-nav-backdrop`：手機版導航。

## 通用區塊

- `.view` / `.view.active`：SPA 頁面切換。
- `.stats-grid`：Dashboard KPI 格線。
- `.stat`：Dashboard 統計卡。
- `.grid`：通用 grid。
- `.two`：兩欄 grid。
- `.panel`：主要卡片/面板。
- `.panel-head`：面板標題列。
- `.form`：表單區塊。
- `.form-actions`：表單按鈕列。
- `.field-hint`：欄位提示。
- `.split`：左右分割布局。

## 表單與輸入

- `input, select`：基本欄位樣式。
- `input:focus, select:focus`：聚焦狀態。
- `.check-grid`：checkbox 群組。
- `.inline-check`：單行 checkbox。
- `.member-row`：訂單成員列。
- `.member-field`：成員欄位。
- `.member-user` / `.member-share`：成員與分潤欄位。
- `.member-remove`：移除成員。
- `.calc-line`：計算列。

## 表格

- `.table-wrap`：表格外層捲動容器。
- `table` / `th` / `td`：全域表格樣式。
- `.table-actions`：表格內操作按鈕排列。
- `.truncate-cell`：長文字省略，現在用於登入紀錄裝置資訊。
- `.recent-orders-table`：Dashboard 近期訂單表。
- `.login-user-table`：帳號管理表。

## 狀態標籤

- `.pill`：狀態膠囊。
- `.pill.good`：成功/正常。
- `.pill.warn`：警告/待處理。
- `.pill.bad`：錯誤/停用。
- `.plain-status`：文字狀態。
- `.plain-status.good`
- `.plain-status.warn`
- `.plain-status.bad`
- `.plain-status.muted`

## 紀錄頁樣式

目前三個紀錄頁由 `app.js` 動態加上共用結構。

### 共用 class

- `.log-page`：紀錄頁根樣式與共用 CSS 變數。
- `.log-switcher`：紀錄頁內切換列。
- `.log-hero`：紀錄頁 Header。
- `.log-kpis`：KPI 卡片容器。
- `.log-kpi-card`：KPI 卡片。
- `.log-filters`：篩選列。
- `.log-table-panel`：紀錄表格 panel。
- `.log-table`：紀錄表格。
- `.log-row`：可點擊表格列。
- `.log-drawer-backdrop`：右側 Drawer 背景。
- `.log-drawer`：右側 Drawer 主體。
- `.log-drawer-head`：Drawer 標題列。
- `.log-drawer-section`：Drawer 內容區。

### 三種主題色

- `.log-page-purple`：操作紀錄，紫色。
- `.log-page-gold`：金流紀錄，金黃色。
- `.log-page-blue`：登入紀錄，藍色。

對應變數：

- `--log-accent`
- `--log-accent-soft`

個人化時可以只改這三個 class 的色值。

### 金額顏色

- `.amount-positive`：收入，綠色。
- `.amount-negative`：支出，紅色。

## Modal / Drawer / Picker

- `.modal-backdrop`：Modal 背景。
- `.modal-panel`：Modal 面板。
- `.modal-head`：Modal 標題列。
- `.password-modal`：改密碼。
- `.member-picker-modal`：成員選擇。
- `.gift-picker-modal`：禮物選擇。
- `.record-modal`：舊資料詳情 Modal。
- `.record-modal-content`：舊詳情內容。
- `.record-detail-grid`：詳情欄位 grid。
- `.record-detail`：詳情欄位。
- `.record-json`：JSON 顯示區。

目前紀錄頁詳情已改用 `.log-drawer`，不再使用 `record-modal`。

## 組織與部門

- `.org-layout`
- `.org-workspace`
- `.org-summary`
- `.org-list-panel`
- `.org-editor`
- `.org-editor-head`
- `.segmented`
- `.org-tab-panel`
- `.department-list`
- `.department-card`
- `.department-head`
- `.department-title`
- `.department-title-en`
- `.department-description`
- `.department-meta`
- `.department-actions`
- `.department-members`
- `.department-modal`
- `.department-modal-summary`
- `.department-member-list`
- `.department-member-row`

## 價目表 / 圖片展示

- `.price-board`
- `.price-gallery`
- `.price-shot`
- `.price-shot.active`
- `.price-display`
- `.price-display img`
- `.price-nav`
- `.price-nav-prev`
- `.price-nav-next`
- `.price-edge-hint`
- `.category-tabs`
- `.category-tabs button.active`

## 權限管理

- `.permission-panel`
- `.permission-note`
- `.permission-table`
- `.permission-code`

## RWD 區塊

目前有多個 `@media`：

- `@media (max-width: 980px)`：紀錄頁 KPI / filter 改為兩欄。
- `@media (max-width: 640px)`：紀錄頁 KPI / filter 改為單欄，Drawer 滿版。
- `@media (min-width: 1280px)`：大螢幕布局。
- `@media (max-width: 720px)`：平板/手機布局。
- `@media (max-width: 560px)`：手機細節修正。
- `@media (max-width: 400px)`：小手機修正。
- `@media (prefers-reduced-motion: reduce)`：降低動畫。

## 個人化建議

### 1. 只換主題色

優先改：

```css
:root {
  --accent: #你的主色;
  --accent-dark: #你的深主色;
  --accent-2: #你的第二主色;
  --accent-soft: rgb(... / 12%);
  --primary-gradient: linear-gradient(...);
}
```

### 2. 改紀錄頁三色

```css
.log-page-purple {
  --log-accent: #7c3aed;
  --log-accent-soft: rgba(124, 58, 237, 0.12);
}

.log-page-gold {
  --log-accent: #d99a00;
  --log-accent-soft: rgba(217, 154, 0, 0.14);
}

.log-page-blue {
  --log-accent: #2563eb;
  --log-accent-soft: rgba(37, 99, 235, 0.12);
}
```

### 3. 改資訊密度

可調整：

- `.panel` padding。
- `.table-wrap` max-height / overflow。
- `th, td` padding。
- `.log-kpi-card` padding。
- `.log-filters` grid 欄寬與 padding。

### 4. 改字體

```css
body {
  font-family: "Microsoft JhengHei", "Noto Sans TC", Arial, sans-serif;
}
```

### 5. 建議新增個人化區塊

建議在 `styles.css` 最後放：

```css
/* Personal Theme */
:root {
  --accent: ...;
}
```

這樣可以覆寫前面所有設定，也方便之後還原。

## 待整理項目

- `styles.css` 後段有大量覆寫規則，未來可拆成：
  - `tokens.css`
  - `layout.css`
  - `components.css`
  - `logs.css`
  - `responsive.css`
- 目前部分 HTML/JS 顯示文字有編碼異常，個人化 UI 文字時建議一併清理。
