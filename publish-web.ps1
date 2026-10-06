# =====================================================================
#  publish-web.ps1 - Build & publish full-stack (React + .NET) cho Somee
#
#  Cách dùng (từ thư mục gốc repo):
#    1. Sửa host thật:  M.API\appsettings.Production.json
#                       ChamCong\.env.production
#    2. Chạy:            powershell -ExecutionPolicy Bypass -File .\publish-web.ps1
#    3. Upload nội dung  M.API\publish-web\  lên Somee (gốc website)
#
#  Script làm gì:
#    - Build ChamCong (React, env production) -> dist/
#    - Copy dist/ -> M.API/wwwroot/   (1 URL duy nhất: API + React + /uploads)
#    - dotnet publish M.API -> M.API/publish-web/ (kèm web.config + wwwroot)
# =====================================================================
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

# ---------- 1. Build React ----------
Write-Host "==> Building React (ChamCong)..." -ForegroundColor Cyan
Push-Location "$root\ChamCong"
npm run build
Pop-Location

# ---------- 2. Gộp dist vào wwwroot của API ----------
Write-Host "==> Copying dist/ -> M.API/wwwroot/ ..." -ForegroundColor Cyan
$dist = Join-Path $root "ChamCong\dist"
$www  = Join-Path $root "M.API\wwwroot"
$uploads = Join-Path $www "uploads"

# Giữ lại wwwroot/uploads (ảnh đang chạy dev), chỉ dọn phần build cũ
if (Test-Path $www) {
    Get-ChildItem $www -Force | Where-Object { $_.Name -ne 'uploads' } | Remove-Item -Recurse -Force
}
# Copy NỘI DUNG của dist thẳng vào wwwroot (không nest "dist/")
Copy-Item "$dist\*" $www -Recurse
if (-not (Test-Path $uploads)) {
    New-Item -ItemType Directory -Force -Path $uploads | Out-Null
}

# ---------- 3. dotnet publish ----------
Write-Host "==> Publishing M.API (Release, production config)..." -ForegroundColor Cyan
$out = Join-Path $root "M.API\publish-web"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
dotnet publish "$root\M.API\M.API.csproj" -c Release -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Đảm bảo wwwroot (React build) được copy theo publish
if (-not (Test-Path (Join-Path $out "wwwroot"))) {
    Copy-Item $www (Join-Path $out "wwwroot") -Recurse
}
New-Item -ItemType Directory -Force -Path (Join-Path $out "logs") | Out-Null

# ---------- 4. ÉP web.config chuẩn vào output publish ----------
# dotnet publish (Web SDK) tự sinh web.config (inprocess + %LAUNCHER_PATH%...)
# co the ghi de bang chinh xac. Somee Free: OutOfProcess an toan hon (IIS worker
# chiem nhat memory, 128-256MB). Buoc nay ghi de bang cua SDK:
$webConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceExists="false" />
      </handlers>
      <aspNetCore processPath="%LAUNCHER_PATH%" arguments="." hostingModel="OutOfProcess" stdoutLogEnabled="true" stdoutLogFile=".\logs">
        <environmentVariables>
          <add name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
'@
Set-Content -Path (Join-Path $out "web.config") -Value $webConfig -Encoding UTF8
Write-Host "==> web.config overwritten (OutOfProcess + Production env, no <rewrite>)" -ForegroundColor Cyan

Write-Host ""
Write-Host "DONE!" -ForegroundColor Green
Write-Host "Upload nội dung thư mục: M.API\publish-web\" -ForegroundColor Yellow
Write-Host "Sau đó kiểm tra: https://YOUR_SOMEE_HOST/swagger"
