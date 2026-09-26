$path = 'D:\My Project\M-BE\Marixa-ChamCong\M.Contract.Repositories\Entities\EmployeeBankAccount.cs'
$enc = New-Object System.Text.UTF8Encoding($false)
$c = [System.IO.File]::ReadAllText($path, $enc)
$needle = "        public int Status { get; set; } = 1;"
$replacement = "        // Trạng thái tài khoản: 1 = Đang dùng, 0 = Đã ngưng
        public int Status { get; set; } = 1;"
if ($c.Contains($needle)) {
    $c = $c.Replace($needle, $replacement)
    [System.IO.File]::WriteAllText($path, $c, $enc)
    Write-Host "updated"
} else {
    Write-Host "needle not found"
}
