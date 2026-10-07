$b = [IO.File]::ReadAllBytes('D:\My Project\M-BE\Marixa-ChamCong\M.API\Program.cs')
$text = [Text.Encoding]::UTF8.GetString($b)
$lines = $text -split "`n"
for ($i = 20; $i -lt 45; $i++) {
    if ($i -lt $lines.Count) {
        Write-Host ("{0,3}: {1}" -f ($i+1), $lines[$i])
    }
}
Write-Host "---BOM check---"
$b = [IO.File]::ReadAllBytes('D:\My Project\M-BE\Marixa-ChamCong\M.API\Program.cs')
Write-Host ("bytes 1-3: {0} {1} {2}" -f $b[0], $b[1], $b[2])
$line = 25
foreach ($l in ($text -split "`n")) {
    if ($line -eq 25) {
        $bytes = [Text.Encoding]::UTF8.GetBytes($l)
        Write-Host ("line 25 hex: " + [BitConverter]::ToString($bytes))
    }
    $line++
}
