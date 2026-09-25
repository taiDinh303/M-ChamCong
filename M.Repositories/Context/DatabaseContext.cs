using M.Contract.Repositories.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace M.Repositories.Context
{
    public class DatabaseContext : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        Guid,
        ApplicationUserClaim,
        ApplicationUserRole,
        ApplicationUserLogin,
        ApplicationRoleClaim,
        ApplicationUserToken>
    {
        public DatabaseContext(
            DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        // =====================================================
        // IDENTITY
        // =====================================================

        public virtual DbSet<ApplicationUser> ApplicationUser
            => Set<ApplicationUser>();

        public virtual DbSet<ApplicationRole> ApplicationRole
            => Set<ApplicationRole>();

        public virtual DbSet<ApplicationUserClaim> ApplicationUserClaim
            => Set<ApplicationUserClaim>();

        public virtual DbSet<ApplicationUserRole> ApplicationUserRole
            => Set<ApplicationUserRole>();

        public virtual DbSet<ApplicationUserLogin> ApplicationUserLogin
            => Set<ApplicationUserLogin>();

        public virtual DbSet<ApplicationRoleClaim> ApplicationRoleClaim
            => Set<ApplicationRoleClaim>();

        public virtual DbSet<ApplicationUserToken> ApplicationUserToken
            => Set<ApplicationUserToken>();


        // =====================================================
        // HRMS
        // =====================================================

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Department> Departments { get; set; }

        public DbSet<Position> Positions { get; set; }

        public DbSet<EmployeeContract> EmployeeContracts { get; set; }

        public DbSet<SalaryGroup> SalaryGroups { get; set; }

        public DbSet<EmployeeSalary> EmployeeSalaries { get; set; }

        public DbSet<EmployeeInsurance> EmployeeInsurances { get; set; }

        public DbSet<Bank> Banks { get; set; }

        public DbSet<EmployeeBankAccount> EmployeeBankAccounts { get; set; }

        public DbSet<EmployeeDependent> EmployeeDependents { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<AttendanceLog> AttendanceLogs { get; set; }

        public DbSet<LeaveType> LeaveTypes { get; set; }

        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        public DbSet<Payroll> Payrolls { get; set; }


        // =====================================================
        // RELATIONSHIPS
        // =====================================================

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =========================================================
            // EMPLOYEE - DEPARTMENT
            // =========================================================

            builder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);


            // =========================================================
            // DEPARTMENT - MANAGER
            // =========================================================

            builder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithMany()
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);


            // =========================================================
            // EMPLOYEE - MANAGER
            // =========================================================

            builder.Entity<Employee>()
                .HasOne(e => e.Manager)
                .WithMany(e => e.Subordinates)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.NoAction);


            // =========================================================
            // EMPLOYEE - APPLICATION USER
            // =========================================================

            builder.Entity<Employee>()
                .HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<Employee>(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);


            // =========================================================
            // EMPLOYEE - LEAVE REQUEST
            // =========================================================

            builder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // LEAVE REQUEST - APPROVER
            // =========================================================

            builder.Entity<LeaveRequest>()
                .HasOne(l => l.Approver)
                .WithMany()
                .HasForeignKey(l => l.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);


            // =========================================================
            // ATTENDANCE - EMPLOYEE
            // =========================================================

            builder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // ATTENDANCE LOG - ATTENDANCE
            // =========================================================

            builder.Entity<AttendanceLog>()
                .HasOne(l => l.Attendance)
                .WithMany(a => a.AttendanceLogs)
                .HasForeignKey(l => l.AttendanceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}