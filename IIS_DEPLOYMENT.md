# IIS 與 GitHub Actions 部署

此專案使用兩個 workflow：

- `CI`：PR 與推送到 `main` 時執行 restore、build、publish。
- `Deploy IIS`：推送到 `main` 或手動觸發時，透過 IIS 主機上的
  GitHub self-hosted runner 部署。

## 1. IIS 主機準備

1. 安裝 IIS。
2. 安裝與專案 Target Framework 相符的 ASP.NET Core Hosting Bundle。
3. 建立 Application Pool，例如 `EAPlaymateGroupPool`：
   - `.NET CLR Version`：`No Managed Code`
   - `Enable 32-Bit Applications`：`False`
4. 建立網站，例如：
   - Site name：`EAPlaymateGroup`
   - Physical path：`C:\Sites\EAPlaymateGroup`
   - Binding：使用實際網域與 HTTPS 憑證
5. 建立持久化目錄：

```powershell
New-Item C:\Sites\EAPlaymateGroup\DataProtectionKeys -ItemType Directory -Force
New-Item C:\Sites\EAPlaymateGroup\logs -ItemType Directory -Force
```

6. 授權 App Pool：

```powershell
icacls C:\Sites\EAPlaymateGroup /grant "IIS AppPool\EAPlaymateGroupPool:(OI)(CI)RX"
icacls C:\Sites\EAPlaymateGroup\DataProtectionKeys /grant "IIS AppPool\EAPlaymateGroupPool:(OI)(CI)M"
icacls C:\Sites\EAPlaymateGroup\logs /grant "IIS AppPool\EAPlaymateGroupPool:(OI)(CI)M"
```

## 2. 正式環境設定

在 IIS 實體目錄建立未納入 Git 的 `appsettings.Production.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQL_SERVER;Database=EAPlaymateGroup;User Id=ea_playmate_app;Password=CHANGE_ME;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

將檔案 ACL 限制為 Administrators、SYSTEM 與 App Pool Identity。部署腳本會保留：

- `appsettings.Production.json`
- `DataProtectionKeys`
- `logs`

資料庫初次建立依序執行：

```text
database/01_create_app_login.sql
database/02_create_tables.sql
database/03_seed_sample_data.sql
```

正式環境通常不要執行 sample data；請先檢查 `03_seed_sample_data.sql`。

## 3. 安裝 Self-hosted Runner

在 GitHub repository：

```text
Settings > Actions > Runners > New self-hosted runner
```

選擇 Windows x64，依 GitHub 顯示的指令安裝，並將 runner 設為 Windows
Service。新增自訂 label：

```text
iis-production
```

Runner Service 帳號必須具有：

- 讀寫 IIS 實體目錄
- 管理指定 Application Pool
- 執行 `WebAdministration` PowerShell module

建議使用專用部署帳號，不要使用一般使用者帳號。

## 4. GitHub Environment

建立：

```text
Settings > Environments > New environment > production
```

建議啟用 Required reviewers。新增 Environment variables：

| Variable | Example |
|---|---|
| `IIS_SITE_NAME` | `EAPlaymateGroup` |
| `IIS_HEALTH_URL` | `https://example.com/api/health` |

本流程不會把 SQL 連線字串放進 GitHub artifact。連線字串保留在 IIS
主機的 `appsettings.Production.json`。

## 5. 第一次部署

1. 確認 IIS 網站、Application Pool 與 production 設定檔已建立。
2. 確認 runner 顯示為 Online。
3. 在 GitHub 開啟：

```text
Actions > Deploy IIS > Run workflow
```

部署腳本會：

1. 備份目前版本。
2. 建立 `app_offline.htm` 並停止 App Pool。
3. 同步新 publish 檔案。
4. 啟動 App Pool。
5. 呼叫 `/api/health`。
6. 健康檢查失敗時自動回復上一版。

目前 Session 使用 `AddDistributedMemoryCache()`，因此 App Pool recycle 或部署會讓
已登入使用者的 Session 失效。若未來需要多台 IIS 或不中斷 Session，應改用
SQL Server 或 Redis distributed cache。

## 6. 日常流程

```text
feature branch -> Pull Request -> CI -> merge main -> production approval -> IIS deploy
```

不要直接在 IIS 目錄修改程式檔案。正式設定與 Data Protection keys 不由
Git 管理。

## 7. .NET 版本

專案目前為 `net9.0`。正式長期運行前應升級至受支援的 LTS 版本，並在
IIS 主機安裝相同版本的 Hosting Bundle，再同步更新 workflow 中的
`dotnet-version`。
