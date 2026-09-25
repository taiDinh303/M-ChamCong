-- Sample data for M-ChamCong project (SQL Server)
-- Ch?y trên database ?ã áp d?ng migration (b?ng ?ã t?n t?i)

SET NOCOUNT ON;

-- Th?i gian t?o chung
DECLARE @Now datetimeoffset = '2026-09-25 08:00:00 +07:00';

-- IDs m?u (GUID)
DECLARE @DeptHR uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @DeptIT uniqueidentifier = '22222222-2222-2222-2222-222222222222';
DECLARE @PosDev uniqueidentifier = '33333333-3333-3333-3333-333333333333';
DECLARE @PosMgr uniqueidentifier = '44444444-4444-4444-4444-444444444444';
DECLARE @SalaryGroup1 uniqueidentifier = '55555555-5555-5555-5555-555555555555';
DECLARE @BankVCB uniqueidentifier = '66666666-6666-6666-6666-666666666666';
DECLARE @EmpAlice uniqueidentifier = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
DECLARE @EmpBob uniqueidentifier = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
DECLARE @ContractAlice uniqueidentifier = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
DECLARE @SalaryAlice uniqueidentifier = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
DECLARE @BankAccAlice uniqueidentifier = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
DECLARE @LeaveTypeAnnual uniqueidentifier = '77777777-7777-7777-7777-777777777777';
DECLARE @LeaveReqAlice uniqueidentifier = '88888888-8888-8888-8888-888888888888';
DECLARE @AttendanceAlice uniqueidentifier = '99999999-9999-9999-9999-999999999999';
DECLARE @AttendanceLog1 uniqueidentifier = 'aaaabbbb-cccc-dddd-eeee-ffffffff0001';
DECLARE @PayrollAlice uniqueidentifier = 'ffffffff-ffff-ffff-ffff-ffffffffffff';

-- 1. SalaryGroups
IF NOT EXISTS (SELECT 1 FROM SalaryGroups WHERE Id = @SalaryGroup1)
BEGIN
    INSERT INTO SalaryGroups (Id, Code, Name, Description, IsActive, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@SalaryGroup1, 'SG-01', 'Nhóm l??ng 01', 'Nhóm l??ng m?u', 1, 'system', 'system', @Now, @Now);
END

-- 2. Banks
IF NOT EXISTS (SELECT 1 FROM Banks WHERE Id = @BankVCB)
BEGIN
    INSERT INTO Banks (Id, Code, Name, ShortName, IsActive, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@BankVCB, 'VCB', 'Vietcombank', 'VCB', 1, 'system', 'system', @Now, @Now);
END

-- 3. LeaveTypes
IF NOT EXISTS (SELECT 1 FROM LeaveTypes WHERE Id = @LeaveTypeAnnual)
BEGIN
    INSERT INTO LeaveTypes (Id, Code, Name, MaxDays, IsPaid, IsActive, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@LeaveTypeAnnual, 'ANNUAL', 'Ngh? phép n?m', 12.00, 1, 1, 'system', 'system', @Now, @Now);
END

-- 4. Positions
IF NOT EXISTS (SELECT 1 FROM Positions WHERE Id = @PosDev)
BEGIN
    INSERT INTO Positions (Id, Code, Name, Description, IsActive, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@PosDev, 'DEV', 'Developer', 'L?p trình viên', 1, 'system', 'system', @Now, @Now);
END

IF NOT EXISTS (SELECT 1 FROM Positions WHERE Id = @PosMgr)
BEGIN
    INSERT INTO Positions (Id, Code, Name, Description, IsActive, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@PosMgr, 'MGR', 'Manager', 'Qu?n lý', 1, 'system', 'system', @Now, @Now);
END

-- 5. Departments
IF NOT EXISTS (SELECT 1 FROM Departments WHERE Id = @DeptHR)
BEGIN
    INSERT INTO Departments (Id, Code, Name, Description, ManagerId, IsActive, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@DeptHR, 'HR', 'Phòng Nhân S?', 'Phòng nhân s?', NULL, 1, 'system', 'system', @Now, @Now);
END

IF NOT EXISTS (SELECT 1 FROM Departments WHERE Id = @DeptIT)
BEGIN
    INSERT INTO Departments (Id, Code, Name, Description, ManagerId, IsActive, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@DeptIT, 'IT', 'Phòng Công Ngh?', 'Phòng phát tri?n ph?n m?m', NULL, 1, 'system', 'system', @Now, @Now);
END

-- 6. Employees
IF NOT EXISTS (SELECT 1 FROM Employees WHERE Id = @EmpAlice)
BEGIN
    INSERT INTO Employees (
        Id, EmployeeCode, GivenName, FamilyName, BirthDate, Gender, CitizenId, PhoneNumber, Email,
        PermanentAddress, CurrentAddress, UserId, DepartmentId, PositionId, ManagerId,
        StartDate, ProbationEndDate, LaborType, Status, UsePhoneAttendance, Note,
        CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (
        @EmpAlice, 'E001', 'Alice', 'Nguyen', '1990-05-10', 2, '012345678', '0123456789', 'alice@example.com',
        'Hanoi', 'Hanoi', NULL, @DeptIT, @PosDev, NULL,
        '2024-01-01', '2024-04-01', 1, 2, 0, 'Nhân viên m?u',
        'system', 'system', @Now, @Now);
END

IF NOT EXISTS (SELECT 1 FROM Employees WHERE Id = @EmpBob)
BEGIN
    INSERT INTO Employees (
        Id, EmployeeCode, GivenName, FamilyName, BirthDate, Gender, CitizenId, PhoneNumber, Email,
        PermanentAddress, CurrentAddress, UserId, DepartmentId, PositionId, ManagerId,
        StartDate, ProbationEndDate, LaborType, Status, UsePhoneAttendance, Note,
        CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (
        @EmpBob, 'E002', 'Bob', 'Tran', '1985-11-20', 1, '987654321', '0987654321', 'bob@example.com',
        'Hanoi', 'Hanoi', NULL, @DeptHR, @PosMgr, NULL,
        '2020-06-01', '2020-09-01', 1, 2, 0, 'Qu?n lý m?u',
        'system', 'system', @Now, @Now);
END

-- Update department manager to Bob
UPDATE Departments SET ManagerId = @EmpBob, LastUpdatedBy = 'system', LastUpdatedTime = @Now WHERE Id = @DeptHR;

-- 7. EmployeeContract for Alice
IF NOT EXISTS (SELECT 1 FROM EmployeeContracts WHERE Id = @ContractAlice)
BEGIN
    INSERT INTO EmployeeContracts (Id, EmployeeId, ContractNumber, ContractType, StartDate, EndDate, Note, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@ContractAlice, @EmpAlice, 'CT-2024-001', 3, '2024-01-01', NULL, 'H?p ??ng chính th?c', 'system', 'system', @Now, @Now);
END

-- 8. EmployeeSalary for Alice
IF NOT EXISTS (SELECT 1 FROM EmployeeSalaries WHERE Id = @SalaryAlice)
BEGIN
    INSERT INTO EmployeeSalaries (Id, EmployeeId, SalaryGroupId, PaymentType, BasicSalary, DailyRate, PositionAllowance, OtherAllowance, Bonus, SocialInsuranceSalary, EffectiveFrom, EffectiveTo, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@SalaryAlice, @EmpAlice, @SalaryGroup1, 1, 15000000.00, 0.00, 1000000.00, 0.00, 0.00, 15000000.00, '2024-01-01', NULL, 'system', 'system', @Now, @Now);
END

-- 9. EmployeeBankAccount for Alice
IF NOT EXISTS (SELECT 1 FROM EmployeeBankAccounts WHERE Id = @BankAccAlice)
BEGIN
    INSERT INTO EmployeeBankAccounts (Id, EmployeeId, BankId, AccountNumber, AccountHolderName, IsPrimary, Status, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@BankAccAlice, @EmpAlice, @BankVCB, '012345678901', 'Alice Nguyen', 1, 1, 'system', 'system', @Now, @Now);
END

-- 10. LeaveRequest for Alice
IF NOT EXISTS (SELECT 1 FROM LeaveRequests WHERE Id = @LeaveReqAlice)
BEGIN
    INSERT INTO LeaveRequests (Id, EmployeeId, LeaveTypeId, FromDate, ToDate, TotalDays, Reason, Status, ApprovedBy, ApprovedAt, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@LeaveReqAlice, @EmpAlice, @LeaveTypeAnnual, '2026-09-20', '2026-09-22', 3.00, 'Ngh? phép gia ?ình', 1, NULL, NULL, 'system', 'system', @Now, @Now);
END

-- 11. Attendance + AttendanceLog for Alice (m?t ngày m?u)
IF NOT EXISTS (SELECT 1 FROM Attendances WHERE Id = @AttendanceAlice)
BEGIN
    INSERT INTO Attendances (Id, EmployeeId, AttendanceDate, Status, Note, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@AttendanceAlice, @EmpAlice, '2026-09-24', 1, 'Có m?t', 'system', 'system', @Now, @Now);

    INSERT INTO AttendanceLogs (Id, AttendanceId, LogTime, Type, Method, Latitude, Longitude, DeviceId, Note, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@AttendanceLog1, @AttendanceAlice, '2026-09-24 08:05:00 +07:00', 1, 4, 21.0285110, 105.8048170, 'PHONE-001', 'Check-in b?ng ?i?n tho?i', 'system', 'system', @Now, @Now);
END

-- 12. Payroll for Alice
IF NOT EXISTS (SELECT 1 FROM Payrolls WHERE Id = @PayrollAlice)
BEGIN
    INSERT INTO Payrolls (Id, EmployeeId, PayrollMonth, BasicSalary, Allowance, Bonus, Overtime, Insurance, Tax, Deduction, NetSalary, Status, CreatedBy, LastUpdatedBy, CreatedTime, LastUpdatedTime)
    VALUES (@PayrollAlice, @EmpAlice, '2026-09-01', 15000000.00, 1000000.00, 0.00, 0.00, 1500000.00, 1000000.00, 0.00, 13500000.00, 1, 'system', 'system', @Now, @Now);
END

PRINT 'Sample data inserted (if not existed).';
