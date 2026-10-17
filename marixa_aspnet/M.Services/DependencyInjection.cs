using M.Contract.Services.Security;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;

using M.Core.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;
using M.Services.Services;
using M.Services.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace M.Services
{
    public static class DependencyInjection
    {
        // Registers every business service class. Moved from M.API so that
        // M.Services is self-contained (its own extension method owns its
        // own dependency graph). M.API only calls this, it no longer knows
        // the individual service names.
        public static void AddBusinessServices(this IServiceCollection services)
        {
            services.TryAddSingleton<TimeProvider>(TimeProvider.System);
            services.AddScoped<ICurrentUser, HttpCurrentUser>();
            services.AddScoped<IAdminAccountGuard, AdminAccountGuard>();
            services
                .AddScoped<JwtGenerator>()
                .AddScoped<IAuthService, AuthService>()
                .AddScoped<IUserService, UserService>()
                .AddScoped<IRoleService, RoleService>()
                .AddScoped<IUserRoleService, UserRoleService>()
                .AddScoped<IUserLoginService, UserLoginService>()
                .AddScoped<IAttendanceLogService, AttendanceLogService>()
                .AddScoped<IAttendanceService, AttendanceService>()
                .AddScoped<IAttendancePeriodService, AttendancePeriodService>()
                .AddScoped<ILeaveDocumentService, LeaveDocumentService>()
                .AddScoped<IBankService, BankService>()
                .AddScoped<IDepartmentService, DepartmentService>()
                .AddScoped<IEmployeeBankAccountService, EmployeeBankAccountService>()
                .AddScoped<IEmployeeContractService, EmployeeContractService>()
                .AddScoped<IEmployeeDependentService, EmployeeDependentService>()
                .AddScoped<IEmployeeInsuranceService, EmployeeInsuranceService>()
                .AddScoped<IEmployeeSalaryService, EmployeeSalaryService>()
                .AddScoped<IEmployeeService, EmployeeService>()
                .AddScoped<IEmployeeReportService, EmployeeReportService>()
                .AddScoped<IEmployeePromotionService, EmployeePromotionService>()
                .AddScoped<IEmploymentChangeProcessor, EmploymentChangeProcessor>()
                .AddScoped<IEmployeeHandoverService, EmployeeHandoverService>()
                .AddScoped<ILeaveRequestService, LeaveRequestService>()
                .AddScoped<ILeaveTypeService, LeaveTypeService>()
                .AddScoped<IPayrollService, PayrollService>()
                .AddScoped<IPositionService, PositionService>()
                .AddScoped<ISalaryGroupService, SalaryGroupService>()
                .AddScoped<IShiftService, ShiftService>()
                .AddScoped<IEmployeeShiftService, EmployeeShiftService>()
                .AddScoped<IHolidayCalendarService, HolidayCalendarService>()
                .AddScoped<IAttendanceRuleService, AttendanceRuleService>()
                .AddScoped<IActivationCodeService, ActivationCodeService>()
                .AddScoped<IOvertimeRequestService, OvertimeRequestService>()
                .AddScoped<ILeaveLedgerService, LeaveLedgerService>()
                .AddScoped<IAttendanceCorrectionService, AttendanceCorrectionService>()
                .AddScoped<IOfficeLocationService, OfficeLocationService>()
                .AddScoped<IWorkTaskService, WorkTaskService>()
                .AddScoped<IWorkProjectService, WorkProjectService>()
                .AddScoped<IWorkPerformanceService, WorkPerformanceService>()
                .AddScoped<IAuditLogService, AuditLogService>()
                .AddHttpContextAccessor();
        }
    }
}
