using M.Contract.Repositories.IUOW;
using M.Contract.Services.Interface;
using M.Contract.Services.Interface.Account;
using M.Repositories.UOW;
using M.Services.Service;
using M.Services.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace M.Services
{
    public static class DependencyInjection
    {
        // Registers the repository / unit-of-work layer.
        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRepositories();
        }

        public static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }

        // Registers every business service class. Moved from M.API so that
        // M.Services is self-contained (its own extension method owns its
        // own dependency graph). M.API only calls this, it no longer knows
        // the individual service names.
        public static void AddBusinessServices(this IServiceCollection services)
        {
            services
                .AddScoped<JwtGenerator>()
                .AddScoped<IAuthService, AuthService>()
                .AddScoped<IUserService, UserService>()
                .AddScoped<IRoleService, RoleService>()
                .AddScoped<IUserRoleService, UserRoleService>()
                .AddScoped<IUserLoginService, UserLoginService>()
                .AddScoped<IAttendanceLogService, AttendanceLogService>()
                .AddScoped<IAttendanceService, AttendanceService>()
                .AddScoped<IBankService, BankService>()
                .AddScoped<IDepartmentService, DepartmentService>()
                .AddScoped<IEmployeeBankAccountService, EmployeeBankAccountService>()
                .AddScoped<IEmployeeContractService, EmployeeContractService>()
                .AddScoped<IEmployeeDependentService, EmployeeDependentService>()
                .AddScoped<IEmployeeInsuranceService, EmployeeInsuranceService>()
                .AddScoped<IEmployeeSalaryService, EmployeeSalaryService>()
                .AddScoped<IEmployeeService, EmployeeService>()
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