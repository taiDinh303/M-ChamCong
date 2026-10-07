/*
  Sample data for SQL Server. Run after the EF Core schema migrations have been applied.
  Running this script again resets only the deterministic demo users/employees
  (C000...001-006 and B000...001-006) and their related rows.

  6 tai khoan demo (login name = so dien thoai, chung mot mat khau theo hash duoi day):
    0900000001  Dinh Van Tai       (Nhan vien)
    0900000002  Phung Vinh Luon    (Nhan vien)
    0900000003  Le Anh Khoa        (Nhan su)
    0900000004  Tran Phung Tuyen   (Nhan su)
    0900000005  Huynh Hoang Dang   (Quan ly)
    0900000006  Josept Duc Tuan    (Admin)

  Mat khau tung tuyen dung: Hovaten123@  (hash PBKDF2-SHA256 Identity V3 o duoi).
  Duh lieu cac bang con duoc gioi han ~5 records de kem va chuan cho DB 30MB.
  Development/test data only; do not use these credentials in production.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @missingTables nvarchar(2048) = N'';
SELECT @missingTables = @missingTables
    + CASE WHEN @missingTables = N'' THEN N'' ELSE N', ' END
    + v.TableName
FROM (VALUES
    (N'ActivationCodes'),(N'AspNetRoles'),(N'AspNetUserClaims'),(N'AspNetUserLogins'),
    (N'AspNetUserRoles'),(N'AspNetUsers'),(N'AspNetUserTokens'),(N'AttendanceLogs'),
    (N'Attendances'),(N'Banks'),(N'Departments'),(N'EmployeeBankAccounts'),
    (N'EmployeeContracts'),(N'EmployeeDependents'),(N'EmployeeInsurances'),(N'Employees'),
    (N'EmployeeSalaries'),(N'EmployeeShifts'),(N'LeaveRequests'),(N'LeaveTypes'),
    (N'Payrolls'),(N'Positions'),(N'Shifts')
) v(TableName)
WHERE OBJECT_ID(N'dbo.' + v.TableName, N'U') IS NULL;

IF @missingTables <> N''
BEGIN
    DECLARE @schemaError nvarchar(2048) =
        N'Missing required dbo tables: ' + @missingTables
        + N'. Apply EF Core migrations to Monica_001 before running SampleData.sql.';
    THROW 51000, @schemaError, 1;
END;

BEGIN TRANSACTION;

DECLARE @now datetimeoffset = SYSDATETIMEOFFSET();
DECLARE @today date = CONVERT(date, GETDATE());
DECLARE @passwordHash nvarchar(max) = N'AQAAAAEAAYagAAAAEPRKUNrjqLaRkB4jKF2dOulTwaOc/7ksVVn++05ItTYObdf18SxZ17cGsNFXrDm20Q==';

DECLARE @staff TABLE
(
    Seq int PRIMARY KEY,
    EmployeeId uniqueidentifier NOT NULL,
    EmployeeCode nvarchar(50) NOT NULL,
    GivenName nvarchar(100) NOT NULL,
    FamilyName nvarchar(100) NOT NULL,
    Gender int NOT NULL,
    PhoneNumber nvarchar(20) NOT NULL,
    Email nvarchar(256) NOT NULL,
    UserId uniqueidentifier NULL,
    RoleName nvarchar(256) NULL,
    DeptCode nvarchar(50) NOT NULL,
    PositionCode nvarchar(50) NOT NULL
);

-- 6 tai khoan theo danh sach, so dien thoai = username.
INSERT INTO @staff VALUES
(1,'C0000000-0000-0000-0000-000000000001',N'NV-001',N'Đinh Văn',N'Tài',1,N'0900000001',N'sample.tai@marixa.local','B0000000-0000-0000-0000-000000000001',N'Employee',N'ENG',N'STAFF'),
(2,'C0000000-0000-0000-0000-000000000002',N'NV-002',N'Phùng Vĩnh',N'Luân',1,N'0900000002',N'sample.luan@marixa.local','B0000000-0000-0000-0000-000000000002',N'Employee',N'OPS',N'STAFF'),
(3,'C0000000-0000-0000-0000-000000000003',N'HR-001',N'Lê Anh',N'Khoa',1,N'0900000003',N'sample.khoa@marixa.local','B0000000-0000-0000-0000-000000000003',N'HR',N'HR',N'HR'),
(4,'C0000000-0000-0000-0000-000000000004',N'HR-002',N'Trần Phụng',N'Tuyền',2,N'0900000004',N'sample.tuyen@marixa.local','B0000000-0000-0000-0000-000000000004',N'HR',N'HR',N'HR'),
(5,'C0000000-0000-0000-0000-000000000005',N'QL-001',N'Huỳnh Hoàng',N'Đăng',1,N'0900000005',N'sample.dang@marixa.local','B0000000-0000-0000-0000-000000000005',N'Manager',N'OPS',N'MANAGER'),
(6,'C0000000-0000-0000-0000-000000000006',N'AD-001',N'Josept',N'Đức Tuấn',1,N'0900000006',N'sample.tuan@marixa.local','B0000000-0000-0000-0000-000000000006',N'Admin',N'OPS',N'ADMIN');

DECLARE @sampleEmployeeIds TABLE (Id uniqueidentifier PRIMARY KEY);
DECLARE @sampleUserIds TABLE (Id uniqueidentifier PRIMARY KEY);
INSERT INTO @sampleEmployeeIds SELECT EmployeeId FROM @staff;
INSERT INTO @sampleUserIds SELECT UserId FROM @staff WHERE UserId IS NOT NULL;

/* Remove only rows linked to the deterministic demo IDs, so re-running refreshes the sample. */
DELETE l
FROM dbo.AttendanceLogs l
JOIN dbo.Attendances a ON a.Id = l.AttendanceId
WHERE a.EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);

DELETE FROM dbo.Attendances
WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);

DELETE FROM dbo.LeaveRequests
WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);
UPDATE dbo.LeaveRequests SET ApprovedBy = NULL
WHERE ApprovedBy IN (SELECT Id FROM @sampleEmployeeIds)
  AND EmployeeId NOT IN (SELECT Id FROM @sampleEmployeeIds);

DELETE FROM dbo.ActivationCodes
WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds)
   OR UserId IN (SELECT Id FROM @sampleUserIds);
DELETE FROM dbo.EmployeeShifts WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);
DELETE FROM dbo.EmployeeContracts WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);
DELETE FROM dbo.EmployeeSalaries WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);
DELETE FROM dbo.EmployeeInsurances WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);
DELETE FROM dbo.EmployeeBankAccounts WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);
DELETE FROM dbo.EmployeeDependents WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);
DELETE FROM dbo.Payrolls WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);

UPDATE dbo.Attendances SET ApprovedBy = NULL
WHERE ApprovedBy IN (SELECT Id FROM @sampleEmployeeIds)
  AND EmployeeId NOT IN (SELECT Id FROM @sampleEmployeeIds);
UPDATE dbo.Departments SET ManagerId = NULL
WHERE ManagerId IN (SELECT Id FROM @sampleEmployeeIds);
UPDATE dbo.Employees SET ManagerId = NULL
WHERE ManagerId IN (SELECT Id FROM @sampleEmployeeIds);
UPDATE dbo.Employees SET UserId = NULL
WHERE UserId IN (SELECT Id FROM @sampleUserIds);
UPDATE dbo.AspNetUsers SET EmployeeId = NULL
WHERE EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);

DELETE FROM dbo.Employees WHERE Id IN (SELECT Id FROM @sampleEmployeeIds);
DELETE FROM dbo.AspNetUserRoles WHERE UserId IN (SELECT Id FROM @sampleUserIds);
DELETE FROM dbo.AspNetUserClaims WHERE UserId IN (SELECT Id FROM @sampleUserIds);
DELETE FROM dbo.AspNetUserLogins WHERE UserId IN (SELECT Id FROM @sampleUserIds);
DELETE FROM dbo.AspNetUserTokens WHERE UserId IN (SELECT Id FROM @sampleUserIds);
DELETE FROM dbo.AspNetUsers WHERE Id IN (SELECT Id FROM @sampleUserIds);

DECLARE @roles TABLE (RoleName nvarchar(256), Description nvarchar(max));
INSERT INTO @roles VALUES
(N'Admin',N'Quản trị hệ thống'),
(N'Manager',N'Quản lý'),
(N'HR',N'Nhân sự'),
(N'Accountant',N'Kế toán'),
(N'Employee',N'Nhân viên');

INSERT INTO dbo.AspNetRoles (Id,CreatedTime,LastUpdatedTime,Description,Name,NormalizedName,ConcurrencyStamp)
SELECT NEWID(),@now,@now,r.Description,r.RoleName,UPPER(r.RoleName),CONVERT(nvarchar(36),NEWID())
FROM @roles r
WHERE NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles x WHERE x.NormalizedName=UPPER(r.RoleName));

INSERT INTO dbo.Departments (Id,Code,Name,Description,ManagerId,IsActive,CreatedTime,LastUpdatedTime)
SELECT NEWID(),v.Code,v.Name,v.Description,NULL,1,@now,@now
FROM (VALUES
    (N'ENG',N'Kỹ thuật',N'Phát triển sản phẩm'),
    (N'OPS',N'Vận hành',N'Vận hành nội bộ'),
    (N'HR',N'Nhân sự',N'Quản lý nhân sự'),
    (N'FIN',N'Tài chính kế toán',N'Kế toán và tiền lương'),
    (N'MKT',N'Tiếp thị',N'Truyền thông và tiếp thị')
) v(Code,Name,Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Departments d WHERE d.Code=v.Code);

INSERT INTO dbo.Positions (Id,Code,Name,Description,IsActive,CreatedTime,LastUpdatedTime)
SELECT NEWID(),v.Code,v.Name,v.Description,1,@now,@now
FROM (VALUES
    (N'STAFF',N'Nhân viên',N'Nhân viên'),
    (N'MANAGER',N'Quản lý',N'Quản lý nhóm'),
    (N'HR',N'Chuyên viên nhân sự',N'Nhân sự'),
    (N'ACCOUNTANT',N'Kế toán',N'Kế toán'),
    (N'ADMIN',N'Quản trị viên',N'Quản trị hệ thống')
) v(Code,Name,Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Positions p WHERE p.Code=v.Code);

INSERT INTO dbo.Shifts (Id,Code,Name,Description,StartTime,EndTime,StandardHours,BreakMinutes,IsNight,WorkDays,IsActive,CreatedTime,LastUpdatedTime)
SELECT NEWID(),v.Code,v.Name,v.Description,v.StartTime,v.EndTime,v.StandardHours,v.BreakMinutes,v.IsNight,v.WorkDays,1,@now,@now
FROM (VALUES
    (N'HC',N'Ca hành chính',N'Thứ 2 đến thứ 6',CAST('08:00' AS time),CAST('17:00' AS time),8,60,0,31),
    (N'MORN',N'Ca sáng',N'Ca sáng',CAST('06:00' AS time),CAST('14:00' AS time),7,60,0,127),
    (N'EVE',N'Ca chiều',N'Ca chiều',CAST('14:00' AS time),CAST('22:00' AS time),7,60,0,127),
    (N'NIGHT',N'Ca đêm',N'Ca đêm',CAST('22:00' AS time),CAST('06:00' AS time),7,60,1,127),
    (N'PART',N'Ca bán thời gian',N'Ca linh hoạt',CAST('09:00' AS time),CAST('13:00' AS time),4,0,0,31)
) v(Code,Name,Description,StartTime,EndTime,StandardHours,BreakMinutes,IsNight,WorkDays)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Shifts s WHERE s.Code=v.Code);

INSERT INTO dbo.Banks (Id,Code,Name,ShortName,IsActive,CreatedTime,LastUpdatedTime)
SELECT NEWID(),v.Code,v.Name,v.ShortName,1,@now,@now
FROM (VALUES
    (N'VCB',N'Ngân hàng TMCP Ngoại thương Việt Nam',N'Vietcombank'),
    (N'TCB',N'Ngân hàng TMCP Kỹ thương Việt Nam',N'Techcombank'),
    (N'MB',N'Ngân hàng TMCP Quân đội',N'MB Bank'),
    (N'ACB',N'Ngân hàng TMCP Á Châu',N'ACB'),
    (N'BIDV',N'Ngân hàng TMCP Đầu tư và Phát triển Việt Nam',N'BIDV')
) v(Code,Name,ShortName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Banks b WHERE b.Code=v.Code);

INSERT INTO dbo.LeaveTypes (Id,Code,Name,MaxDays,IsPaid,IsActive,CreatedTime,LastUpdatedTime)
SELECT NEWID(),v.Code,v.Name,v.MaxDays,v.IsPaid,1,@now,@now
FROM (VALUES
    (N'ANNUAL',N'Nghỉ phép năm',12,1),
    (N'SICK',N'Nghỉ ốm',30,1),
    (N'UNPAID',N'Nghỉ không lương',CAST(NULL AS int),0),
    (N'MATERNITY',N'Nghỉ thai sản',180,1),
    (N'PERSONAL',N'Nghỉ việc riêng',3,1)
) v(Code,Name,MaxDays,IsPaid)
WHERE NOT EXISTS (SELECT 1 FROM dbo.LeaveTypes t WHERE t.Code=v.Code);

INSERT INTO dbo.AspNetUsers
    (Id,CreatedTime,LastUpdatedTime,UserName,NormalizedUserName,Email,NormalizedEmail,EmailConfirmed,
     PasswordHash,SecurityStamp,ConcurrencyStamp,PhoneNumber,PhoneNumberConfirmed,TwoFactorEnabled,LockoutEnabled,AccessFailedCount)
SELECT s.UserId,@now,@now,s.PhoneNumber,UPPER(s.PhoneNumber),s.Email,UPPER(s.Email),1,
       @passwordHash,CONVERT(nvarchar(36),NEWID()),CONVERT(nvarchar(36),NEWID()),s.PhoneNumber,1,0,0,0
FROM @staff s
WHERE s.UserId IS NOT NULL;

INSERT INTO dbo.AspNetUserRoles (UserId,RoleId,CreatedTime,LastUpdatedTime)
SELECT s.UserId,r.Id,@now,@now
FROM @staff s
JOIN dbo.AspNetRoles r ON r.NormalizedName=UPPER(s.RoleName)
WHERE s.UserId IS NOT NULL;

INSERT INTO dbo.Employees
    (Id,EmployeeCode,GivenName,FamilyName,Gender,PhoneNumber,Email,UserId,DepartmentId,PositionId,ManagerId,
     StartDate,LaborType,Status,UsePhoneAttendance,CreatedTime,LastUpdatedTime)
SELECT s.EmployeeId,s.EmployeeCode,s.GivenName,s.FamilyName,s.Gender,
       s.PhoneNumber,s.Email,s.UserId,d.Id,p.Id,
       CASE WHEN s.Seq IN (5,6) THEN NULL ELSE N'C0000000-0000-0000-0000-000000000005' END,
       DATEADD(day,-(s.Seq*30),@today),1,2,1,@now,@now
FROM @staff s
JOIN dbo.Departments d ON d.Code=s.DeptCode
JOIN dbo.Positions p ON p.Code=s.PositionCode;

UPDATE u SET EmployeeId=e.Id
FROM dbo.AspNetUsers u
JOIN dbo.Employees e ON e.UserId=u.Id
WHERE u.Id IN (SELECT Id FROM @sampleUserIds);

UPDATE d SET ManagerId=N'C0000000-0000-0000-0000-000000000005'
FROM dbo.Departments d WHERE d.Code IN (N'ENG',N'OPS',N'MKT');
UPDATE d SET ManagerId=N'C0000000-0000-0000-0000-000000000006'
FROM dbo.Departments d WHERE d.Code IN (N'HR',N'FIN');

-- ================= Dữ liệu con: ~5 records (Seq <= 5) =================

INSERT INTO dbo.EmployeeContracts (Id,EmployeeId,ContractNumber,ContractType,StartDate,EndDate,Note,CreatedTime,LastUpdatedTime)
SELECT NEWID(),s.EmployeeId,CONCAT(N'HĐ-',s.EmployeeCode),2,DATEADD(day,-180,@today),DATEADD(year,1,@today),N'Dữ liệu demo',@now,@now
FROM @staff s WHERE s.Seq<=5;

INSERT INTO dbo.EmployeeInsurances
    (Id,EmployeeId,SocialInsuranceNumber,HealthInsuranceNumber,PersonalTaxCode,IsSocialInsuranceParticipant,
     ParticipationStartDate,SocialInsuranceSalary,Status,CreatedTime,LastUpdatedTime)
SELECT NEWID(),s.EmployeeId,CONCAT(N'BHXH',s.EmployeeCode),CONCAT(N'BHYT',s.EmployeeCode),CONCAT(N'MST',s.EmployeeCode),1,
       DATEADD(day,-180,@today),12000000,1,@now,@now
FROM @staff s WHERE s.Seq<=5;

INSERT INTO dbo.EmployeeBankAccounts (Id,EmployeeId,BankId,AccountNumber,AccountHolderName,IsPrimary,Status,CreatedTime,LastUpdatedTime)
SELECT NEWID(),s.EmployeeId,b.Id,CONCAT(N'001234567',FORMAT(s.Seq,N'00')),CONCAT(s.GivenName,N' ',s.FamilyName),1,1,@now,@now
FROM @staff s
JOIN dbo.Banks b ON b.Code=CASE s.Seq%5 WHEN 1 THEN N'VCB' WHEN 2 THEN N'TCB' WHEN 3 THEN N'MB' WHEN 4 THEN N'ACB' ELSE N'BIDV' END
WHERE s.Seq<=5;

INSERT INTO dbo.EmployeeSalaries
    (Id,EmployeeId,PaymentType,BasicSalary,DailyRate,PositionAllowance,OtherAllowance,Bonus,SocialInsuranceSalary,EffectiveFrom,CreatedTime,LastUpdatedTime)
SELECT NEWID(),s.EmployeeId,1,15000000,0,500000,500000,0,12000000,DATEADD(day,-180,@today),@now,@now
FROM @staff s WHERE s.Seq<=5;

INSERT INTO dbo.EmployeeShifts (Id,EmployeeId,ShiftId,EffectiveFrom,Note,CreatedTime,LastUpdatedTime)
SELECT NEWID(),s.EmployeeId,sh.Id,DATEADD(day,-30,@today),N'Ca demo',@now,@now
FROM @staff s CROSS JOIN dbo.Shifts sh
WHERE s.Seq<=5 AND sh.Code=N'HC';

INSERT INTO dbo.Payrolls
    (Id,EmployeeId,PayrollMonth,BasicSalary,Allowance,Bonus,Overtime,Insurance,Tax,Deduction,NetSalary,Status,PayDate,PaymentMethod,CreatedTime,LastUpdatedTime)
SELECT NEWID(),s.EmployeeId,DATEFROMPARTS(YEAR(@today),MONTH(@today),1),15000000,1000000,500000,0,1200000,300000,0,15000000,
       CASE WHEN s.Seq<=2 THEN 2 ELSE 1 END,NULL,2,@now,@now
FROM @staff s WHERE s.Seq<=5;

INSERT INTO dbo.EmployeeDependents (Id,EmployeeId,GivenName,FamilyName,Relationship,BirthDate,Status,CreatedTime,LastUpdatedTime)
SELECT NEWID(),s.EmployeeId,N'Mẫu',CONCAT(N'Người phụ thuộc ',s.Seq),N'Con',DATEADD(year,-8,@today),1,@now,@now
FROM @staff s WHERE s.Seq<=5;

INSERT INTO dbo.LeaveRequests
    (Id,EmployeeId,LeaveTypeId,FromDate,ToDate,TotalDays,Reason,Status,ApprovedBy,ApprovedAt,CreatedTime,LastUpdatedTime)
SELECT NEWID(),s.EmployeeId,t.Id,DATEADD(day,10+s.Seq,@today),DATEADD(day,10+s.Seq,@today),1,
       N'Đơn nghỉ phép mẫu',CASE WHEN s.Seq<=2 THEN 2 ELSE 1 END,
       CASE WHEN s.Seq<=2 THEN N'C0000000-0000-0000-0000-000000000005' ELSE NULL END,
       CASE WHEN s.Seq<=2 THEN DATEADD(day,1,@today) ELSE NULL END,@now,@now
FROM @staff s
JOIN dbo.LeaveTypes t ON t.Code=N'ANNUAL'
WHERE s.Seq<=5;

/* 5 working-day attendance records: 2 approved and 3 awaiting approval. */
;WITH DateRange AS
(
    SELECT CAST(DATEADD(day,-1,@today) AS date) AS WorkDate,1 AS n
    UNION ALL
    SELECT DATEADD(day,-1,WorkDate),n+1 FROM DateRange WHERE n<30
), WorkDays AS
(
    SELECT WorkDate,ROW_NUMBER() OVER (ORDER BY WorkDate DESC) AS DaySeq
    FROM DateRange
    WHERE DATEDIFF(day,CONVERT(date,'19000101'),WorkDate)%7 BETWEEN 0 AND 4
)
INSERT INTO dbo.Attendances
    (Id,EmployeeId,AttendanceDate,Status,PlannedShiftId,PlannedHours,ActualHours,ApprovalStatus,ApprovedBy,ApprovedAt,CreatedTime,LastUpdatedTime)
SELECT NEWID(),s.EmployeeId,w.WorkDate,1,sh.Id,8,8,
       CASE WHEN w.DaySeq<=2 THEN 1 ELSE 0 END,
       CASE WHEN w.DaySeq<=2 THEN N'C0000000-0000-0000-0000-000000000005' ELSE NULL END,
       CASE WHEN w.DaySeq<=2 THEN DATEADD(hour,18,CAST(w.WorkDate AS datetime2)) ELSE NULL END,@now,@now
FROM WorkDays w
JOIN @staff s ON s.Seq=1+((w.DaySeq-1)%2)
CROSS JOIN dbo.Shifts sh
WHERE w.DaySeq<=5 AND sh.Code=N'HC'
OPTION (MAXRECURSION 40);

INSERT INTO dbo.AttendanceLogs
    (Id,AttendanceId,LogTime,[Type],[Method],DeviceId,IsAdjusted,CreatedTime,LastUpdatedTime)
SELECT NEWID(),a.Id,TODATETIMEOFFSET(DATEADD(hour,8,CAST(a.AttendanceDate AS datetime2)),'+07:00'),1,4,N'DEMO-PHONE',0,@now,@now
FROM dbo.Attendances a
WHERE a.EmployeeId IN (SELECT Id FROM @sampleEmployeeIds)
UNION ALL
SELECT NEWID(),a.Id,TODATETIMEOFFSET(DATEADD(hour,17,CAST(a.AttendanceDate AS datetime2)),'+07:00'),2,4,N'DEMO-PHONE',0,@now,@now
FROM dbo.Attendances a
WHERE a.EmployeeId IN (SELECT Id FROM @sampleEmployeeIds);

COMMIT TRANSACTION;

PRINT N'Đã tạo 6 tài khoản demo (0900000001..06), 6 nhân viên và dữ liệu con ~5 records.';
