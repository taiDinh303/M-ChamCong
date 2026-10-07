# =====================================================================
#  publish-api-test.ps1 - Publish CHANCE API (khong co React) de test
#  Somee: chi up M.API\publish-api-test\ vao goc site, test:
#    https://marixa001.somee.com/api/diagnostics
#    https://marixa001.somee.com/swagger
#  MU DICH: phan loai loi 500 co do tong hop React static hay khong.
# =====================================================================
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$out = Join-Path $root "M.API\publish-api-test"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

Write-Host "==> Publishing M.API (API only, no React)..." -ForegroundColor Cyan
dotnet publish "$root\M.API\M.API.csproj" -c Release -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Ép web.config chuan Somee (dotnet publish tu sinh lai bang SDK)
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

New-Item -ItemType Directory -Force -Path (Join-Path $out "logs") | Out-Null

Write-Host ""
Write-Host "DONE! Upload NOI DUNG (khong phai thay vao) M.API\publish-api-test\ len Somee goc site." -ForegroundColor Green
Write-Host "Test: https://marixa001.somee.com/api/diagnostics" -ForegroundColor Yellow
