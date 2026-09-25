$ErrorActionPreference = 'Stop'
$f = 'D:\My Project\M-BE\Marixa-ChamCong\.agnes\artifacts\research\20260925_5\deep_research\20260925-5-authentity-entities-cham-cong-ke-hoach.md'
$appendixSrc = 'D:\My Project\M-BE\Marixa-ChamCong\.agnes\work\research\20260925_5\appendix.md'
$lines = Get-Content -LiteralPath $f
# Find the first line containing the marker "evidence ledger" (the corrupted appendix heading)
$cut = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -like '*evidence ledger*' -and $lines[$i].StartsWith('##')) { $cut = $i; break }
}
if ($cut -lt 0) { Write-Output 'MARKER NOT FOUND'; exit 1 }
$clean = $lines[0..($cut-1)]
# Drop trailing blank lines
while ($clean.Count -gt 0 -and $clean[-1].Trim().Length -eq 0) { $clean = $clean[0..($clean.Count-2)] }
$appendix = Get-Content -LiteralPath $appendixSrc -Encoding UTF8
$all = $clean + @('') + $appendix
Set-Content -LiteralPath $f -Value $all -Encoding UTF8
Write-Output ("Cleaned body lines: " + $clean.Count)
Get-Content -LiteralPath $f -Tail 3
