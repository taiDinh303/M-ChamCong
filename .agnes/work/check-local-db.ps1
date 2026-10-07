$ErrorActionPreference = 'Stop'
$connStr = 'Data Source=DESKTOP-DVT;Initial Catalog=Monica_001;Integrated Security=True;Encrypt=False;TrustServerCertificate=True'
try {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' ORDER BY TABLE_NAME"
    $reader = $cmd.ExecuteReader()
    $tables = @()
    while ($reader.Read()) { $tables += $reader[0] }
    $reader.Close()

    $cols = $conn.CreateCommand()
    $cols.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Attendances' AND COLUMN_NAME IN ('CheckInTime','CheckOutTime','ChangeSummary') ORDER BY COLUMN_NAME"
    $r2 = $cols.ExecuteReader()
    $colNames = @()
    while ($r2.Read()) { $colNames += $r2[0] }
    $r2.Close()

    $rows = $conn.CreateCommand()
    $rows.CommandText = "SELECT COUNT(*) FROM dbo.Attendances"
    $attCount = $rows.ExecuteScalar()

    $logRows = $conn.CreateCommand()
    $logRows.CommandText = "SELECT COUNT(*) FROM dbo.AttendanceLogs"
    $logCount = $logRows.ExecuteScalar()

    $conn.Close()

    Write-Host ('Total tables: ' + $tables.Count)
    Write-Host ('AttendanceLogs table exists: ' + ($tables -contains 'AttendanceLogs'))
    Write-Host ('Attendances table exists: ' + ($tables -contains 'Attendances'))
    Write-Host ('Attendances new columns: [' + ($colNames -join ', ') + ']')
    Write-Host ('Attendances rows: ' + $attCount)
    Write-Host ('AttendanceLogs rows: ' + $logCount)
} catch {
    Write-Host ('ERROR: ' + $_.Exception.Message)
}
