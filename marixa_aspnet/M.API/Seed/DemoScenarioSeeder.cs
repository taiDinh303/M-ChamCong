using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using M.Contract.Repositories.AuthEntities;
using M.Contract.Repositories.Entities;
using M.Core.Base;
using M.ModelViews.AttendancePeriodModelView;
using M.Repositories.Context;
using M.Services.Services.Timekeeping;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace M.API.Seed;

/// <summary>Explicit, additive demo dataset. Never called by normal API startup.</summary>
public static class DemoScenarioSeeder
{
    public const string Marker = "MARIXA-DEMO-202610-V1";
    public static Guid Id(string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes(Marker + ":" + key))[..16]);
    public static readonly string[] AccountNames = ["demo.an", "demo.binh", "demo.chi", "demo.dung", "demo.ha", "demo.linh"];

    public static async Task<bool> SeedAsync(DatabaseContext db, UserManager<ApplicationUser> users, string password)
    {
        if (await db.AuditLogs.AnyAsync(x => x.Id == Id("completed"))) return false;
        if (await db.Employees.AnyAsync(x => x.EmployeeCode.StartsWith("DEMO-")))
            throw new InvalidOperationException("Existing DEMO employees without this batch marker; inspect before continuing.");
        if (await db.AttendancePeriods.AnyAsync(p => (p.YearMonth == 202610 || p.YearMonth == 202609) && p.State != AttendancePeriodState.Open))
            throw new InvalidOperationException("A demo target month is reviewed/locked. No source data was changed.");
        // A closed period affects everyone. Only demonstrate locking in an empty historical month.
        if (await db.Attendances.AnyAsync(a => a.AttendanceDate >= Day(9, 1) && a.AttendanceDate < Day(10, 1))
            || await db.AttendancePeriods.AnyAsync(p => p.YearMonth == 202609))
            throw new InvalidOperationException("September 2026 already has source data or a period. Choose another demo history before seeding.");
        var rule = await db.AttendanceRules.SingleAsync(r => r.IsActive && r.DeletedTime == null);
        var reviewer = await db.Users.Join(db.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
            .Join(db.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r.Name })
            .Where(x => x.Name == "Admin" && x.u.DeletedTime == null && x.u.EmployeeId.HasValue
                && db.Employees.Any(e => e.Id == x.u.EmployeeId && e.DeletedTime == null)).Select(x => x.u).FirstAsync();
        var reviewerEmployeeId = reviewer.EmployeeId ?? throw new InvalidOperationException("The reviewer must have an employee profile.");
        var employeeRole = await db.Roles.SingleAsync(r => r.Name == "Employee");
        _ = employeeRole;
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var departments = new[] { "Vận hành", "Kinh doanh", "Kỹ thuật" }.Select((name, i) => Stamp(new Department
        { Id = Id($"department-{i}"), Code = $"DEMO-PB-{i + 1}", Name = $"[DEMO] {name}", Description = "Phòng ban giả lập để kiểm thử; không phải cơ cấu thật." })).ToArray();
        var positions = new[] { "Trưởng nhóm", "Chuyên viên", "Nhân viên", "Thực tập sinh" }.Select((name, i) => Stamp(new Position
        { Id = Id($"position-{i}"), Code = $"DEMO-CV-{i + 1}", Name = $"[DEMO] {name}" })).ToArray();
        var salaryGroup = Stamp(new SalaryGroup { Id = Id("salary-group"), Code = "DEMO-SG", Name = "[DEMO] Lương khối văn phòng" });
        var bank = Stamp(new Bank { Id = Id("bank"), Code = "DEMO-BANK", Name = "[DEMO] Ngân hàng kiểm thử", ShortName = "DEMO" });
        var shift = Stamp(new Shift { Id = Id("shift"), Code = "DEMO-HC", Name = "[DEMO] Hành chính 08–17", StartTime = rule.CheckInTime,
            EndTime = rule.CheckOutTime, StandardHours = rule.StandardHours, BreakMinutes = rule.BreakMinutes, WorkDays = 31 });
        var annual = Stamp(new LeaveType { Id = Id("annual-leave"), Code = "DEMO-ANNUAL", Name = "[DEMO] Phép năm", MaxDays = 12, IsPaid = true });
        var unpaid = Stamp(new LeaveType { Id = Id("unpaid-leave"), Code = "DEMO-UNPAID", Name = "[DEMO] Nghỉ không lương", IsPaid = false });
        db.AddRange(departments); db.AddRange(positions); db.AddRange(salaryGroup, bank, shift, annual, unpaid);
        db.OfficeLocations.Add(Stamp(new OfficeLocation { Id = Id("office"), Name = "[DEMO] Văn phòng mô phỏng", IsPrimary = false,
            Description = "Địa điểm giả lập, không dùng để kiểm vị trí nhân viên thật.", Latitude = 10.7769m, Longitude = 106.7009m, RadiusMeters = 200 }));
        db.HolidayCalendars.Add(Stamp(new HolidayCalendar { Id = Id("holiday"), Date = Day(9, 2), Year = 2026,
            Type = HolidayType.PublicHoliday, Name = "[DEMO] Quốc khánh 02/09" }));
        var names = new[] { ("An", "Nguyễn Minh"), ("Bình", "Trần Quốc"), ("Chi", "Lê Ngọc"), ("Dũng", "Phạm Anh"), ("Hà", "Võ Thanh"), ("Linh", "Đỗ Thùy") };
        var employees = names.Select((n, i) => Stamp(new Employee
        {
            Id = Id($"employee-{i}"), EmployeeCode = $"DEMO-{i + 1:000}", GivenName = n.Item1 + " [DEMO]", FamilyName = n.Item2,
            Email = $"{AccountNames[i]}@example.invalid", BirthDate = new DateTime(1992 + i, 3 + i, 12, 0, 0, 0, DateTimeKind.Utc),
            Gender = i is 2 or 4 or 5 ? GenderType.Female : GenderType.Male,
            DepartmentId = departments[i % 3].Id, PositionId = positions[i == 0 ? 0 : i == 5 ? 3 : 2].Id,
            ManagerId = i == 0 ? null : Id("employee-0"), StartDate = Day(i == 5 ? 10 : 1, 1),
            ProbationEndDate = i == 5 ? Day(11, 30) : null, LaborType = i == 5 ? LaborType.Intern : LaborType.FullTime,
            Status = i == 5 ? EmployeeStatus.Probation : EmployeeStatus.Working, UsePhoneAttendance = true,
            CurrentAddress = "Địa chỉ giả lập — không dùng gửi thư", Note = Marker + ": Hồ sơ giả, không có số CCCD/điện thoại thật."
        })).ToArray();
        db.AddRange(employees);
        await db.SaveChangesAsync();
        for (var i = 0; i < employees.Length; i++)
        {
            var employee = employees[i];
            var user = new ApplicationUser { Id = Id($"user-{i}"), UserName = AccountNames[i], Email = employee.Email,
                EmailConfirmed = true, EmployeeId = employee.Id, CreatedBy = Marker };
            Check(await users.CreateAsync(user, password));
            Check(await users.AddToRoleAsync(user, "Employee"));
            employee.UserId = user.Id;
            db.EmployeeContracts.Add(Stamp(new EmployeeContract { Id = Id($"contract-{i}"), EmployeeId = employee.Id,
                ContractNumber = $"DEMO-HD-2026-{i + 1:000}", ContractType = i == 5 ? ContractType.Probation : ContractType.FixedTerm,
                StartDate = employee.StartDate!.Value, EndDate = i == 5 ? Day(11, 30) : i == 4 ? Day(11, 5) : Day(12, 31), Note = "[DEMO] Hợp đồng mô phỏng, không có giá trị pháp lý." }));
            var salary = 11000000m + i * 1500000m;
            db.EmployeeSalaries.Add(Stamp(new EmployeeSalary { Id = Id($"salary-{i}"), EmployeeId = employee.Id, SalaryGroupId = salaryGroup.Id,
                PaymentType = SalaryPaymentType.Monthly, BasicSalary = salary, PositionAllowance = i == 0 ? 2000000 : 0,
                OtherAllowance = 730000, SocialInsuranceSalary = salary, EffectiveFrom = employee.StartDate.Value }));
            db.EmployeeInsurances.Add(Stamp(new EmployeeInsurance { Id = Id($"insurance-{i}"), EmployeeId = employee.Id,
                IsSocialInsuranceParticipant = i != 5, SocialInsuranceSalary = i == 5 ? 0 : salary,
                ParticipationStartDate = i == 5 ? null : employee.StartDate, Status = i == 5 ? InsuranceStatus.Suspended : InsuranceStatus.Active }));
            db.EmployeeBankAccounts.Add(Stamp(new EmployeeBankAccount { Id = Id($"bank-account-{i}"), EmployeeId = employee.Id, BankId = bank.Id,
                AccountNumber = $"DEMO-ACCOUNT-{i + 1:000}", AccountHolderName = employee.FullName, IsPrimary = true }));
            db.EmployeeShifts.Add(Stamp(new EmployeeShift { Id = Id($"shift-assignment-{i}"), EmployeeId = employee.Id, ShiftId = shift.Id,
                EffectiveFrom = employee.StartDate.Value, Note = "[DEMO] Gán ca hành chính" }));
            db.EmployeeDependents.Add(Stamp(new EmployeeDependent { Id = Id($"dependent-{i}"), EmployeeId = employee.Id,
                GivenName = "Bé " + (i + 1) + " [DEMO]", FamilyName = "Nguyễn", Relationship = "Con", BirthDate = new DateTime(2017 + i, 5, 10, 0, 0, 0, DateTimeKind.Utc), EffectiveFrom = Day(1, 1) }));
            db.LeaveLedger.Add(Stamp(new LeaveLedger { Id = Id($"grant-{i}"), EmployeeId = employee.Id, LeaveTypeId = annual.Id, Year = 2026,
                EntryType = LeaveLedgerEntryType.Grant, Days = 12, Reason = "[DEMO] Cấp phép thủ công cho kịch bản kiểm thử, không phải chính sách tự cấp." }));
            db.Payrolls.Add(Stamp(new Payroll { Id = Id($"payroll-{i}"), EmployeeId = employee.Id, PayrollMonth = Day(10, 1),
                BasicSalary = salary, Allowance = 730000, Bonus = i == 1 ? 500000 : 0, Insurance = i == 5 ? 0 : salary * .105m,
                Tax = 0, Deduction = 0, NetSalary = salary + 730000 + (i == 1 ? 500000 : 0) - (i == 5 ? 0 : salary * .105m),
                Status = i % 2 == 0 ? PayrollStatus.Draft : PayrollStatus.Calculated, PaymentMethod = PaymentMethod.BankTransfer }));
        }
        db.LeaveLedger.Add(Stamp(new LeaveLedger { Id = Id("adjust"), EmployeeId = employees[0].Id, LeaveTypeId = annual.Id, Year = 2026,
            EntryType = LeaveLedgerEntryType.Adjust, Days = .5m, Reason = "[DEMO] Điều chỉnh cộng nửa ngày để kiểm tra số lẻ." }));
        await db.SaveChangesAsync();

        var allAttendance = new List<Attendance>();
        for (var i = 0; i < employees.Length; i++)
        {
            for (var day = 1; day <= 16; day++)
            {
                var date = Day(10, day);
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                var late = (day + i) % 7 == 0; var early = (day + i) % 11 == 0;
                var missingCheckout = day == 16 && i == 2;
                var attendance = Stamp(new Attendance { Id = Id($"attendance-{i}-{day}"), EmployeeId = employees[i].Id,
                    AttendanceDate = date, CheckInTime = At(date, 8, late ? 17 : 0), CheckOutTime = missingCheckout ? null : At(date, early ? 16 : 17, early ? 30 : 0),
                    Status = late ? AttendanceStatus.Late : early ? AttendanceStatus.EarlyLeave : AttendanceStatus.Present,
                    ApprovalStatus = i == 0 || day < 8 ? AttendanceApprovalStatus.Approved : AttendanceApprovalStatus.Pending,
                    PlannedShiftId = shift.Id, PlannedHours = rule.StandardHours, Note = "[DEMO] Công mô phỏng; không có bằng chứng ảnh/GPS thật." });
                if (attendance.ApprovalStatus == AttendanceApprovalStatus.Approved) { attendance.ApprovedBy = reviewerEmployeeId; attendance.ApprovedAt = date.AddDays(1); }
                var overtime = new List<OvertimeRequest>();
                if (day == 9 || day == 15)
                {
                    attendance.CheckOutTime = At(date, 19, 0);
                    overtime.Add(Stamp(new OvertimeRequest { Id = Id($"overtime-{i}-{day}"), EmployeeId = employees[i].Id, AttendanceId = attendance.Id,
                        WorkDate = date, StartAt = At(date, 17, 0), EndAt = At(date, 19, 0), OvertimeHours = 2,
                        Status = day == 9 ? OvertimeStatus.Approved : i % 2 == 0 ? OvertimeStatus.Pending : OvertimeStatus.Rejected,
                        ApprovedBy = day == 9 ? reviewerEmployeeId : null, ApprovedAt = day == 9 ? date.AddDays(1) : null,
                        Reason = "[DEMO] Hoàn tất đối soát đơn hàng cuối ngày", Note = "OT chỉ công nhận khi Approved và giao với giờ làm thực tế." }));
                    db.AddRange(overtime);
                }
                var hours = AttendanceTimeCalculator.Compute(attendance.CheckInTime, attendance.CheckOutTime, rule, overtime);
                attendance.ActualHours = hours.ActualHours; attendance.OvertimeHours = hours.OvertimeHours;
                db.Attendances.Add(attendance); allAttendance.Add(attendance);
                db.AttendanceLogs.Add(Stamp(new AttendanceLog { Id = Id($"checkin-{i}-{day}"), AttendanceId = attendance.Id,
                    LogTime = attendance.CheckInTime!.Value, Type = AttendanceLogType.CheckIn, Method = AttendanceMethod.Import, DeviceId = "DEMO-IMPORT", Note = "[DEMO] Sự kiện giả lập" }));
                if (attendance.CheckOutTime.HasValue) db.AttendanceLogs.Add(Stamp(new AttendanceLog { Id = Id($"checkout-{i}-{day}"), AttendanceId = attendance.Id,
                    LogTime = attendance.CheckOutTime.Value, Type = AttendanceLogType.CheckOut, Method = AttendanceMethod.Import, DeviceId = "DEMO-IMPORT", Note = "[DEMO] Sự kiện giả lập" }));
            }
        }
        // No overlap with attendance rows: these requests are for later dates in October.
        for (var i = 0; i < 4; i++)
        {
            var status = (LeaveRequestStatus)(i + 1);
            var request = Stamp(new LeaveRequest { Id = Id($"leave-{i}"), EmployeeId = employees[i].Id, LeaveTypeId = annual.Id,
                FromDate = Day(10, 20 + i), ToDate = Day(10, 20 + i), TotalDays = 1, Status = status, Reason = "[DEMO] Sắp xếp công việc gia đình",
                ApprovedBy = status is LeaveRequestStatus.Approved or LeaveRequestStatus.Cancelled ? reviewerEmployeeId : null,
                ApprovedAt = status is LeaveRequestStatus.Approved or LeaveRequestStatus.Cancelled ? Day(10, 17) : null,
                CancelledBy = status == LeaveRequestStatus.Cancelled ? reviewerEmployeeId : null,
                CancelledAt = status == LeaveRequestStatus.Cancelled ? Day(10, 18) : null,
                CancellationReason = status == LeaveRequestStatus.Cancelled ? "[DEMO] Nhân viên thay đổi kế hoạch, hoàn phép đã trừ." : null });
            db.LeaveRequests.Add(request);
            if (status is LeaveRequestStatus.Approved or LeaveRequestStatus.Cancelled)
                db.LeaveLedger.Add(Stamp(new LeaveLedger { Id = Id($"deduct-{i}"), EmployeeId = request.EmployeeId, LeaveTypeId = annual.Id, Year = 2026,
                    LeaveRequestId = request.Id, EntryType = LeaveLedgerEntryType.Deduct, Days = 1, Reason = "[DEMO] Trừ phép theo đơn đã duyệt" }));
            if (status == LeaveRequestStatus.Cancelled)
                db.LeaveLedger.Add(Stamp(new LeaveLedger { Id = Id($"refund-{i}"), EmployeeId = request.EmployeeId, LeaveTypeId = annual.Id, Year = 2026,
                    LeaveRequestId = request.Id, EntryType = LeaveLedgerEntryType.Refund, Days = 1, Reason = "[DEMO] Hoàn một lần sau hủy đơn đã duyệt" }));
        }
        var correctionAttendance = allAttendance.Single(a => a.EmployeeId == employees[2].Id && a.AttendanceDate.Day == 16);
        db.AttendanceCorrections.Add(Stamp(new AttendanceCorrection { Id = Id("correction"), EmployeeId = employees[2].Id, AttendanceId = correctionAttendance.Id,
            WorkDate = correctionAttendance.AttendanceDate, ProposedCheckIn = correctionAttendance.CheckInTime, ProposedCheckOut = At(Day(10, 16), 17, 0),
            ProposedStatus = AttendanceStatus.Present, Status = AttendanceCorrectionStatus.Pending, Reason = "[DEMO] Quên chấm ra, đề nghị đối soát." }));
        for (var i = 1; i <= 3; i++)
        {
            db.EmployeePromotions.Add(Stamp(new EmployeePromotion { Id = Id($"promotion-{i}"), EmployeeId = employees[i].Id,
                CurrentEmployeeCode = employees[i].EmployeeCode, CurrentPositionName = positions[2].Name, NewPositionId = positions[1].Id,
                NewPositionName = positions[1].Name, NewPositionCode = positions[1].Code, TargetRoleName = "Employee", ProposedByUserId = reviewer.Id,
                ProposedByEmployeeId = reviewer.EmployeeId, ProposedByName = "[DEMO] Người đề xuất", ProposalType = 2,
                Reason = "[DEMO] Hoàn thành tốt mục tiêu quý, đề xuất chuyên viên.", EffectiveDate = Day(11, 1), Status = i - 1,
                ReviewedByUserId = i == 2 ? reviewer.Id : null, ReviewedAt = i == 2 ? Day(10, 17) : null }));
            db.EmployeeHandovers.Add(Stamp(new EmployeeHandover { Id = Id($"handover-{i}"), EmployeeId = employees[i].Id, ManagerId = employees[0].Id,
                LastWorkingDate = Day(11, 15 + i), Reason = "[DEMO] Kế hoạch chuyển công việc sau thời gian báo trước", WorkProgress = "Đã hoàn thành đối soát; còn bàn giao tài liệu quy trình.",
                CompanyAssets = "Laptop DEMO-LAP-00" + i, AssetsJson = JsonSerializer.Serialize(new[] { new { assetType = "Laptop", assetCode = $"DEMO-LAP-{i:000}", condition = "Hoạt động tốt", note = "Tài sản mô phỏng" } }),
                ProjectsJson = "[]", AccountIssued = true, Status = i - 1, ReviewedByEmployeeId = i == 2 ? reviewer.EmployeeId : null,
                ReviewedByName = i == 2 ? "[DEMO] Người duyệt" : null, ReviewedAt = i == 2 ? Day(10, 17) : null }));
        }
        var project = Stamp(new WorkProject { Id = Id("project"), Name = "[DEMO] Chuẩn hóa quy trình vận hành Q4", Description = "Dự án giả lập để thử giao việc, tiến độ và đánh giá.",
            StartDate = Day(10, 1), EndDate = Day(11, 30), OwnerEmployeeId = employees[0].Id, OwnerEmployeeCode = employees[0].EmployeeCode, OwnerName = employees[0].FullName,
            Color = "#1647C8", Status = 1, MemberCount = 4, TaskTotal = 3, TaskDone = 1 });
        db.WorkProjects.Add(project);
        for (var i = 0; i < 4; i++) db.WorkProjectMembers.Add(Stamp(new WorkProjectMember { Id = Id($"member-{i}"), ProjectId = project.Id,
            EmployeeId = employees[i].Id, EmployeeCode = employees[i].EmployeeCode, EmployeeName = employees[i].FullName }));
        for (var i = 1; i <= 3; i++)
        {
            var task = Stamp(new WorkTask { Id = Id($"task-{i}"), AssigneeId = employees[i].Id, AssignedById = employees[0].Id,
                AssigneeEmployeeCode = employees[i].EmployeeCode, AssigneeName = employees[i].FullName, AssignedByName = employees[0].FullName,
                Title = "[DEMO] " + new[] { "", "Đối soát dữ liệu chấm công", "Rà soát hồ sơ nhân sự", "Cập nhật hướng dẫn bàn giao" }[i], Description = "Dữ liệu kiểm thử liên kết nhân viên DEMO.",
                Priority = i, Status = i == 1 ? WorkTaskStatus.InProgress : i == 2 ? WorkTaskStatus.Todo : WorkTaskStatus.Done,
                CurrentStatusLabel = WorkTaskStatus.Label(i == 1 ? WorkTaskStatus.InProgress : i == 2 ? WorkTaskStatus.Todo : WorkTaskStatus.Done), DueDate = Day(10, 18 + i),
                ProgressPercent = i == 3 ? 100 : i == 1 ? 60 : 0, CompletedAt = i == 3 ? Day(10, 16) : null });
            db.WorkTasks.Add(task);
            db.Set<WorkTaskComment>().Add(Stamp(new WorkTaskComment { Id = Id($"comment-{i}"), TaskId = task.Id, AuthorEmployeeId = employees[0].Id,
                AuthorName = employees[0].FullName, Content = "[DEMO] Đã thống nhất đầu ra; cập nhật tiến độ trước hạn." }));
            db.WorkPerformances.Add(Stamp(new WorkPerformance { Id = Id($"performance-{i}"), EmployeeId = employees[i].Id, RatedById = employees[0].Id,
                EmployeeEmployeeCode = employees[i].EmployeeCode, EmployeeName = employees[i].FullName, RatedByName = employees[0].FullName,
                Period = "2026-10", PeriodLabel = "Tháng 10/2026 [DEMO]", Quality = 4, Timeliness = i == 2 ? 3 : 4, Collaboration = 4, Initiation = 3,
                Strengths = "Chủ động phối hợp và phản hồi", Improvements = "Cập nhật tiến độ đều đặn hơn", OverallComment = "[DEMO] Đánh giá giả lập", Status = 1 }));
        }
        // Historic demo data is deliberately separate from the open October period.
        var historic = Stamp(new Attendance { Id = Id("historic-attendance"), EmployeeId = employees[0].Id, AttendanceDate = Day(9, 30),
            CheckInTime = At(Day(9, 30), 8, 0), CheckOutTime = At(Day(9, 30), 17, 0), Status = AttendanceStatus.Present,
            ActualHours = 8, OvertimeHours = 0, ApprovalStatus = AttendanceApprovalStatus.Approved, ApprovedBy = reviewerEmployeeId, ApprovedAt = Day(10, 1), Note = "[DEMO] Dữ liệu tháng lịch sử để thử snapshot/XLSX." });
        db.Attendances.Add(historic);
        await db.SaveChangesAsync();
        var period = Stamp(AttendancePeriod.Open(2026, 9)); period.Id = Id("period");
        period.Review(reviewer.Id, At(Day(10, 1), 9, 0), "[DEMO] Kiểm tra kỳ chỉ chứa công giả lập.");
        var version = period.Lock(At(Day(10, 1), 9, 5));
        var snapshot = new PeriodSnapshot(2026, 9, version, At(Day(10, 1), 9, 5),
            new SnapshotRule(rule.Code, rule.CheckInTime, rule.CheckOutTime, rule.BreakMinutes, rule.StandardHours) { BreakStartTime = rule.BreakStartTime },
            [new SnapshotRow(historic.Id, employees[0].Id, employees[0].EmployeeCode, employees[0].FullName, historic.AttendanceDate, historic.Status,
                historic.ApprovalStatus, historic.CheckInTime, historic.CheckOutTime, historic.ActualHours, historic.OvertimeHours)]);
        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        db.AttendancePeriods.Add(period);
        db.AttendancePeriodSnapshots.Add(Stamp(AttendancePeriodSnapshot.Capture(period.Id, version, json,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))), Marker, At(Day(10, 1), 9, 5))));
        db.AttendancePeriodEvents.Add(Stamp(AttendancePeriodEvent.Record(period.Id, "Reviewed", reviewer.Id, "[DEMO] Người duyệt", "[DEMO] Kiểm tra kỳ lịch sử", At(Day(10, 1), 9, 0))));
        db.AttendancePeriodEvents.Add(Stamp(AttendancePeriodEvent.Record(period.Id, "Locked", reviewer.Id, "[DEMO] Người duyệt", "[DEMO] Khóa kỳ lịch sử", At(Day(10, 1), 9, 5))));
        await PrepareReportDraftsAsync(db);
        db.AuditLogs.Add(Stamp(new AuditLog { Id = Id("completed"), ActorUserName = Marker, Action = AuditAction.Create,
            EntityType = "DemoDataset", EntityId = Id("completed"), Reason = "[DEMO] Tạo bộ dữ liệu kiểm thử được chủ dự án yêu cầu. Không sửa hồ sơ cũ." }));
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public static async Task PrepareReportDraftsAsync(DatabaseContext db)
    {
        var reviewer = await db.Users.Join(db.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
            .Join(db.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r.Name })
            .Where(x => x.Name == "Admin" && x.u.DeletedTime == null && x.u.EmployeeId.HasValue
                && db.Employees.Any(e => e.Id == x.u.EmployeeId && e.DeletedTime == null)).Select(x => x.u).FirstAsync();
        if (await db.Employees.CountAsync(e => e.CreatedBy == Marker) != 6)
            throw new InvalidOperationException("DEMO employee dataset is incomplete.");
        for (var i = 0; i < 3; i++)
        {
            if (await db.EmployeeReports.AnyAsync(r => r.Id == Id($"report-{i}"))) continue;
            var report = Stamp(new EmployeeReport { Id = Id($"report-{i}"), EmployeeId = Id($"employee-{i}"),
                ReportCode = $"DEMO-BC-202610-{i + 1:000}", Title = $"[DEMO] Báo cáo vận hành tháng 10 — {AccountNames[i]}", ReportType = "Vận hành [DEMO]",
                Period = Day(10, 1), Deadline = Day(10, 25), SubmittedAt = Day(10, 18), Status = EmployeeReportStatus.ChangesRequested,
                ManagerComment = "[DEMO] Hồ sơ khởi tạo; cần nộp PDF để hoàn tất tình huống kiểm thử." });
            db.EmployeeReports.Add(report);
            db.EmployeeReportEvents.Add(Stamp(new EmployeeReportEvent { ReportId = report.Id, ActionName = "Created", ActorId = reviewer.Id,
                ActorName = Marker, OccurredAt = Day(10, 18), Comment = "[DEMO] Khởi tạo hồ sơ để người dùng DEMO nộp tài liệu qua API riêng tư." }));
        }
        await db.SaveChangesAsync();
    }

    private static T Stamp<T>(T entity) where T : BaseEntity { entity.CreatedBy = Marker; entity.LastUpdatedBy = Marker; return entity; }
    private static DateTime Day(int month, int day) => new(2026, month, day, 0, 0, 0, DateTimeKind.Utc);
    private static DateTimeOffset At(DateTime day, int hour, int minute) => new DateTimeOffset(DateTime.SpecifyKind(day.Date.AddHours(hour).AddMinutes(minute), DateTimeKind.Unspecified), TimeSpan.FromHours(7)).ToUniversalTime();
    private static void Check(IdentityResult result) { if (!result.Succeeded) throw new InvalidOperationException("DEMO identity validation failed: " + string.Join(", ", result.Errors.Select(e => e.Code))); }
}
