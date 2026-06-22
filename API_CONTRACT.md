# API 契約

## Swagger

開發環境啟動後開啟：

```text
http://localhost:5177/swagger
```

正式環境預設停用。需要啟用時，在未納入 Git 的
`appsettings.Production.json` 設定：

```json
{
  "Swagger": {
    "Enabled": true
  }
}
```

目前網頁登入使用 Session Cookie。可先呼叫 `POST /api/auth/login`，後續請求由
瀏覽器攜帶 `.EAPlaymateGroup.Session`。JWT Bearer 將另行加入。

## 錯誤格式

所有 API 錯誤使用 `application/problem+json`，基本格式：

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Bad Request",
  "status": 400,
  "detail": "輸入資料驗證失敗。",
  "instance": "/api/example",
  "code": "validation_error",
  "traceId": "00-...",
  "errors": {
    "field": ["錯誤原因"]
  }
}
```

- `code`：供程式判斷的穩定錯誤碼。
- `detail`：可顯示給使用者的訊息。
- `traceId`：供伺服器 Log 追查。
- `errors`：僅在欄位驗證失敗時出現。
