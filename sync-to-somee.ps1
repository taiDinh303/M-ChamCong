# =====================================================================
#  sync-to-somee.ps1 - Ép web.config chuan + thu log stdout
#
#  Sau dotnet publish, chay script nay de:
#    1. Ghi web.config chuan (OutOfProcess + Production + %LAUNCHER_PATH%)
#       vao publish\ (dotnet publish luon ghi de bang SDK)
#    2. Tao publish\logs\ (cho stdout.log cua IIS)
#
#  Sau do re-upload NOI DUNG publish\ len Somee
# =====================================================================
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root "publish"
if (-not (Test-Path $out)) { throw "Khong tim thay folder: $out" }

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
New-Item -ItemType Directory -Force -Path (Join-Path $out "wwwroot\uploads") | Out-Null

Write-Host "DONE. web.config chuan + logs/ da tao trong: $out" -ForegroundColor Green
Write-Host "Upload NOI DUNG folder publish\ len Somee (goc site), roi RECYCLE POOL" -ForegroundColor Yellow
