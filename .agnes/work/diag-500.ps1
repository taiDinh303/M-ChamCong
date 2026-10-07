$ErrorActionPreference = 'Stop'
$connStr = 'Data Source=DESKTOP-DVT;Initial Catalog=Monica_001;Integrated Security=True;Encrypt=False;TrustServerCertificate=True'
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

# Cols thực tế của Attendances
$cols = $conn.CreateCommand()
$cols.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Attendances' ORDER BY ORDINAL_POSITION"
$r = $cols.ExecuteReader(); $dbCols = @(); while ($r.Read()) { $dbCols += $r[0] }; $r.Close()
Write-Host ("CDS LOCAL cols Attendances: " + ($dbCols -join ', '))

# Cols EF model yeu cau (tu snapshot)
$efCols = @('Id','ActualHours','ApprovalStatus','ApprovedAt','ApprovedBy','AttendanceDate','CheckInPhoto','CheckOutPhoto','CreatedBy','CreatedTime','DeletedBy','DeletedTime','EmployeeId','LastUpdatedBy','LastUpdatedTime','Note','PlannedHours','PlannedShiftId','Status')
$missing = $efCols | Where-Object { $dbCols -notcontains $_ }
Write-Host ("COLS THIEU trong DB (EF co, DB khong co): [" + ($missing -join ', ') + "]")

# Thuc hien chinh xac query cua by-employee
$emp = 'c0000000-0000-0000-0000-000000000001'
$q = $conn.CreateCommand()
$q.CommandText = @"
SELECT TOP 50 [a].[Id],[a].[EmployeeId],[a].[AttendanceDate],[a].[Status],
 [a].[PlannedShiftId],[a].[PlannedHours],[a].[ActualHours],
 [a].[ApprovalStatus],[a].[ApprovedBy],[a].[CheckInPhoto],[a].[CheckOutPhoto],
 [a].[ApprovedAt],[a].[Note],[a].[CreatedBy],[a].[CreatedTime],[a].[LastUpdatedBy],[a].[LastUpdatedTime],
 [a].[EmployeeCode],[a].[DepartmentCode],[a].[PositionCode],
 [l].[Id] AS logid,[l].[LogTime],[l].[Type]
FROM [Attendances] AS [a]
LEFT JOIN [AttendanceLogs] [l] ON [a].[Id]=[l].[AttendanceId]
WHERE [a].[EmployeeId]='$emp' AND ([a].[DeletedTime] IS NULL)
ORDER BY [a].[AttendanceDate] DESC
"@
try {
    $dr = $q.ExecuteReader()
    $n = 0
    while ($dr.Read()) { $n++ }
    $dr.Close()
    Write-Host ("QUERY OK - " + $n + " rows cho employee " + $emp)
} catch {
    Write-Host ("QUERY FAIL: " + $_.Exception.Message)
}

# Xem 5 attendances co cua ai
$q2 = $conn.CreateCommand()
$q2.CommandText = "SELECT TOP 10 Id, EmployeeId, AttendanceDate FROM dbo.Attendances ORDER BY AttendanceDate DESC"
$dr2 = $q2.ExecuteReader(); $n2=0
Write-Host "--- 5 attendances hien co ---"
while ($dr2.Read()) { $n2++; Write-Host ($dr2[0] + "  emp=" + $dr2[1] + "  date=" + $dr2[2]) }
$dr2.Close()
$conn.Close()
