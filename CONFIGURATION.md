# 設定說明

請勿將資料庫帳號、密碼提交至此儲存庫。

## 必須執行的處理

先前的 SQL Server 密碼已經提交至 Git，因此必須直接在 SQL Server
上更換密碼。僅從目前的檔案移除密碼，無法撤銷已經出現在 Git
歷史紀錄中的憑證。

登入帳號建立腳本使用 SQLCMD 變數 `EA_PLAYMATE_DB_PASSWORD`。
執行腳本時請提供新的密碼：

```powershell
sqlcmd -S YOUR_SQL_SERVER -E -v EA_PLAYMATE_DB_PASSWORD="YOUR_NEW_PASSWORD" -i database/01_create_app_login.sql
```

## 環境變數

ASP.NET Core 會將 `ConnectionStrings__DefaultConnection` 對應至
`ConnectionStrings:DefaultConnection`。

以下 PowerShell 範例只會設定目前程序使用的環境變數：

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=YOUR_SQL_SERVER;Database=EAPlaymateGroup;User Id=YOUR_SQL_USER;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;Encrypt=True;"
dotnet run
```

正式環境請將此變數設定於部署平台的密碼或機密管理服務中。
請勿將真實連線字串寫入會提交至 Git 的腳本。

## 本機設定檔

本機開發使用的 `appsettings.Development.json` 已由 Git 忽略，
因此也可以將連線字串放在該檔案中，但仍建議優先使用環境變數或
密碼管理服務。

`appsettings.example.json` 僅供範例使用。提交至 Git 的範例檔案
必須保留 placeholder，不可填入真實帳號或密碼。
