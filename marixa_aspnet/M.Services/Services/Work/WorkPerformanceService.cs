using M.Core.Security;
using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using M.ModelViews.WorkPerformanceModelView;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Work
{
    public class WorkPerformanceService : BaseService, IWorkPerformanceService
    {

        public WorkPerformanceService(
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser, TimeProvider clock) : base(unitOfWork, currentUser, clock)
        {
        }

        private IGenericRepository<WorkPerformance> PerfRepo => UnitOfWork.GetRepository<WorkPerformance>();
        private IGenericRepository<Employee> EmpRepo => UnitOfWork.GetRepository<Employee>();

        private IQueryable<WorkPerformanceRow> Query()
        {
            return from p in PerfRepo.Entities.AsNoTracking()
                   select new WorkPerformanceRow
                   {
                       Id = p.Id,
                       EmployeeId = p.EmployeeId,
                       RatedById = p.RatedById,
                       EmployeeEmployeeCode = p.EmployeeEmployeeCode,
                       EmployeeName = p.EmployeeName,
                       RatedByName = p.RatedByName,
                       Period = p.Period,
                       PeriodLabel = p.PeriodLabel,
                       Quality = p.Quality,
                       Timeliness = p.Timeliness,
                       Collaboration = p.Collaboration,
                       Initiation = p.Initiation,
                       Average = WorkPerformanceStatus.Average(p),
                       Strengths = p.Strengths,
                       Improvements = p.Improvements,
                       OverallComment = p.OverallComment,
                       Status = p.Status,
                       EmployeeConfirmedByName = p.EmployeeConfirmedByName,
                       EmployeeConfirmedAt = p.EmployeeConfirmedAt,
                       CreatedTime = p.CreatedTime
                   };
        }

        public async Task<List<WorkPerformanceRow>> GetMineAsync(Guid employeeId)
        {
            return await Query().Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.CreatedTime).ToListAsync();
        }

        public async Task<List<WorkPerformanceRow>> GetAllAsync(Guid requesterId, bool isAdmin)
        {
            var q = Query();
            if (!isAdmin)
                q = q.Where(x => x.RatedById == requesterId || x.EmployeeId == requesterId);
            return await q.OrderByDescending(x => x.CreatedTime).ToListAsync();
        }

        public async Task<WorkPerformanceRow?> GetByIdAsync(Guid id)
        {
            return await Query().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid> CreateAsync(Guid raterId, CreateWorkPerformanceModelView model)
        {
            RequireEmployeeAccess(model.EmployeeId);

            string currentUser = CurrentUserName;

            var emp = await EmpRepo.Entities.FirstOrDefaultAsync(e => e.Id == model.EmployeeId)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Không tìm thấy nhân viên.");
            var rater = await EmpRepo.Entities.FirstOrDefaultAsync(e => e.Id == raterId);

            var period = string.IsNullOrWhiteSpace(model.Period)
                ? $"{DateTime.Now:yyyy-MM}"
                : model.Period;

            // Không trùng kỳ
            if (await PerfRepo.Entities.AnyAsync(x => x.EmployeeId == emp.Id && x.Period == period))
                throw new ErrorException(StatusCodes.Status409Conflict, "DUPLICATE", $"Nhân viên đã có đánh giá kỳ {model.PeriodLabel ?? period}.");

            var p = new WorkPerformance
            {
                EmployeeId = emp.Id,
                EmployeeEmployeeCode = emp.EmployeeCode,
                EmployeeName = $"{emp.GivenName} {emp.FamilyName}",
                RatedById = rater?.Id ?? Guid.Empty,
                RatedByName = rater is null ? currentUser : $"{rater.GivenName} {rater.FamilyName}",
                Period = period,
                PeriodLabel = string.IsNullOrWhiteSpace(model.PeriodLabel) ? $"Kỳ {period}" : model.PeriodLabel,
                Quality = Math.Clamp(model.Quality, 1, 5),
                Timeliness = Math.Clamp(model.Timeliness, 1, 5),
                Collaboration = Math.Clamp(model.Collaboration, 1, 5),
                Initiation = Math.Clamp(model.Initiation, 1, 5),
                Strengths = model.Strengths?.Trim(),
                Improvements = model.Improvements?.Trim(),
                OverallComment = model.OverallComment?.Trim(),
                Status = model.Submit ? WorkPerformanceStatus.Submitted : WorkPerformanceStatus.Draft,
                CreatedBy = currentUser
            };
            await PerfRepo.InsertAsync(p);
            await UnitOfWork.SaveAsync();
            return p.Id;
        }

        public async Task ConfirmAsync(Guid id, ConfirmWorkPerformanceModelView model, Guid employeeId)
        {
            string currentUser = CurrentUserName;

            var p = await PerfRepo.Entities.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(StatusCodes.Status404NotFound, "NOT_FOUND", "Đánh giá không tồn tại.");
            var employee = await EmpRepo.Entities.FirstOrDefaultAsync(e => e.Id == employeeId);

            if (p.EmployeeId != employee?.Id)
                throw new ErrorException(StatusCodes.Status403Forbidden, "FORBIDDEN", "Chỉ nhân viên được đánh giá mới xác nhận.");
            if (p.Status == WorkPerformanceStatus.Confirmed)
                throw new ErrorException(StatusCodes.Status409Conflict, "DUPLICATE", "Đã xác nhận trước đó.");

            p.Status = WorkPerformanceStatus.Confirmed;
            p.EmployeeConfirmedBy = employee?.Id;
            p.EmployeeConfirmedByName = employee is null ? currentUser : $"{employee.GivenName} {employee.FamilyName}";
            p.EmployeeConfirmedAt = UtcNow.UtcDateTime;
            p.OverallComment = string.IsNullOrWhiteSpace(model.Comment) ? p.OverallComment : model.Comment!.Trim();
            p.LastUpdatedBy = currentUser;
            p.LastUpdatedTime = UtcNow;
            await PerfRepo.UpdateAsync(p);
            await UnitOfWork.SaveAsync();
        }
    }
}
