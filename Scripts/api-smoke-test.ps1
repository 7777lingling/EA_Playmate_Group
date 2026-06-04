param(
    [string]$BaseUrl = "http://localhost:5177",
    [string]$ProjectPath = (Resolve-Path "$PSScriptRoot\..").Path
)

$ErrorActionPreference = "Stop"

function Invoke-Json {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Method,
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [object]$Body = $null
    )

    $uri = "$BaseUrl$Path"
    if ($null -eq $Body) {
        return Invoke-RestMethod -Method $Method -Uri $uri -TimeoutSec 20
    }

    $json = $Body | ConvertTo-Json -Depth 20
    return Invoke-RestMethod -Method $Method -Uri $uri -ContentType "application/json" -Body $json -TimeoutSec 20
}

$runId = Get-Date -Format "yyyyMMddHHmmss"
$payMonth = Get-Date -Format "yyyy-MM"
$orderDate = Get-Date -Format "yyyy-MM-dd"
$playerName = "E2E_Player_$runId"
$bossName = "E2E_Boss_$runId"
$orderNo = "E2E-$runId"

$process = Start-Process dotnet `
    -ArgumentList "run --no-build --urls $BaseUrl" `
    -WorkingDirectory $ProjectPath `
    -WindowStyle Hidden `
    -PassThru

try {
    Start-Sleep -Seconds 4

    $health = Invoke-Json -Method Get -Path "/api/health"
    Write-Host "Health: $($health.status)"

    $player = Invoke-Json -Method Post -Path "/api/users" -Body @{
        nickname = $playerName
        systemRole = "staff"
        isPlayer = $true
        isBoss = $false
    }
    Write-Host "Created player: id=$($player.id), nickname=$($player.nickname)"

    $boss = Invoke-Json -Method Post -Path "/api/users" -Body @{
        nickname = $bossName
        systemRole = "staff"
        isPlayer = $false
        isBoss = $true
    }
    Write-Host "Created boss: id=$($boss.id), nickname=$($boss.nickname)"

    $order = Invoke-Json -Method Post -Path "/api/orders" -Body @{
        orderNo = $orderNo
        orderDate = $orderDate
        ownerUserId = $boss.id
        amount = 100.00
        commissionRate = 0.1000
        commissionAmount = 10.00
        status = "completed"
        customerPaymentStatus = "unpaid"
        remark = "E2E smoke test order"
        members = @(
            @{
                userId = $player.id
                role = "player"
                shareAmount = 90.00
            }
        )
    }
    Write-Host "Created order: id=$($order.id), orderNo=$($order.orderNo), shareTotal=$($order.shareTotalAmount)"

    Invoke-Json -Method Post -Path "/api/orders/$($order.id)/customer-payment-status" -Body @{
        customerPaymentStatus = "paid"
    } | Out-Null
    Write-Host "Marked customer payment paid for order id=$($order.id)"

    $payments = Invoke-Json -Method Post -Path "/api/payments/generate-monthly" -Body @{
        payMonth = $payMonth
        overwriteExisting = $true
    }
    Write-Host "Generated payments: count=$($payments.Count)"

    $ranking = Invoke-Json -Method Get -Path "/api/dashboard/ranking"
    $playerRanking = $ranking | Where-Object { $_.userId -eq $player.id } | Select-Object -First 1
    if ($null -eq $playerRanking) {
        throw "Created player was not found in ranking."
    }
    Write-Host "Ranking verified: nickname=$($playerRanking.nickname), totalShare=$($playerRanking.totalShareAmount)"

    $auditLogs = Invoke-Json -Method Get -Path "/api/auditlogs?take=20"
    Write-Host "Audit logs returned: count=$($auditLogs.Count)"

    Write-Host "Smoke test completed."
}
finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }
}
