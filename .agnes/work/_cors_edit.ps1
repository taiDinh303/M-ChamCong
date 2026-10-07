$ErrorActionPreference = 'Stop'
$path = 'M.API\DependencyInjection.cs'
$bytes = [IO.File]::ReadAllBytes($path)
$crlf = ($bytes -contains 13) -and ($bytes -contains 10)
Write-Host "CRLF present: $crlf"

$nlChar = "`r`n"
if (-not $crlf) { $nlChar = "`n" }
function Str2Bytes($s) { [System.Text.Encoding]::ASCII.GetBytes($s) }

function Patch($b, $anchor, $replacement) {
    $a = Str2Bytes $anchor
    $r = Str2Bytes $replacement
    $found = 0
    for ($i=0; $i -le $b.Length - $a.Length; $i++) {
        $ok = $true
        for ($j=0; $j -lt $a.Length; $j++) { if ($b[$i+$j] -ne $a[$j]) { $ok=$false; break } }
        if ($ok) { $found++; if ($found -eq 1) { $idx = $i } }
        if ($found -gt 1) { break }
    }
    if ($found -ne 1) { throw "anchor not unique (found=$found): $anchor" }
    $out = New-Object byte[] ($b.Length + $r.Length - $a.Length)
    [Array]::Copy($b, 0, $out, 0, $idx)
    [Array]::Copy($r, 0, $out, $idx, $r.Length)
    [Array]::Copy($b, $idx + $a.Length, $out, $idx + $r.Length, $b.Length - ($idx + $a.Length))
    return $out
}

# 1) Before AddCors: read allowed origins from config
$anchor1 = 'services.AddCors(options =>'
$rep1 = "var corsAllowedOrigins = configuration.GetSection(`"CorsPolicy:AllowedOrigins`").Get<string[]>() ?? Array.Empty<string>();" + $nlChar + '            services.AddCors(options =>'
$bytes = Patch $bytes $anchor1 $rep1

# 2) Extend the origin predicate with configured origins
$anchor2 = '|| origin.StartsWith("http://[::1]", StringComparison.OrdinalIgnoreCase))'
$rep2 = '|| origin.StartsWith("http://[::1]", StringComparison.OrdinalIgnoreCase)' + $nlChar + '                            || corsAllowedOrigins.Any(o => !string.IsNullOrWhiteSpace(o) && string.Equals(origin, o, StringComparison.OrdinalIgnoreCase)))'
$bytes = Patch $bytes $anchor2 $rep2

# 3) Allow credentials
$anchor3 = '.AllowAnyMethod();'
$rep3 = '.AllowAnyMethod()' + $nlChar + '                        .AllowCredentials();'
$bytes = Patch $bytes $anchor3 $rep3

[IO.File]::WriteAllBytes($path, $bytes)

# verify high bytes unchanged (was 10)
$hi = 0
foreach ($x in $bytes) { if ($x -ge 128) { $hi++ } }
Write-Host "High bytes after patch: $hi (was 10)"
Write-Host "OK"
