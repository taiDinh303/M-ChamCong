using M.Contract.Repositories.Entities;
using M.Contract.Repositories.IUOW;
using M.Contract.Services.Interface;
using M.Core.Base;
using M.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelViews.LeaveRequestModelView;
using M.Services.Mappings;
using static M.Core.Base.BaseException;

namespace M.Services.Service
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LeaveRequestService(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<BasePaginatedList<LeaveRequestResponseModelView>> GetAllAsync(int pageNumber, int pageSize)
        {
            IGenericRepository<LeaveRequest> repo = _unitOfWork.GetRepository<LeaveRequest>();

            IQueryable<LeaveRequest> query = repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Include(x => x.Approver)
                .Where(x => !x.DeletedTime.HasValue)
                .OrderBy(x => x.CreatedTime);

            int totalItems = await query.CountAsync();

            List<LeaveRequest> items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<LeaveRequestResponseModelView> result = items
                .Select(x => x.ToViewModel())
                .ToList();

            return new BasePaginatedList<LeaveRequestResponseModelView>(
                result.AsReadOnly(),
                totalItems,
                pageNumber,
                pageSize);
        }

        public async Task<LeaveRequestResponseModelView> GetByIdAsync(Guid id)
        {
            IGenericRepository<LeaveRequest> repo = _unitOfWork.GetRepository<LeaveRequest>();

            LeaveRequest entity = await repo.Entities
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Include(x => x.Approver)
                .Where(x => x.Id == id && !x.DeletedTime.HasValue)
                .FirstOrDefaultAsync()
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave request not found");

            return entity.ToViewModel();
        }

        public async Task CreateAsync(CreateLeaveRequestModelView model)
        {
            IGenericRepository<LeaveRequest> repo = _unitOfWork.GetRepository<LeaveRequest>();

            // Validate employee
            IGenericRepository<Employee> employeeRepo = _unitOfWork.GetRepository<Employee>();
            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue);

            if (!employeeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee not found");
            }

            // Validate leave type
            IGenericRepository<LeaveType> leaveTypeRepo = _unitOfWork.GetRepository<LeaveType>();
            bool leaveTypeExists = await leaveTypeRepo.Entities
                .AnyAsync(x => x.Id == model.LeaveTypeId && !x.DeletedTime.HasValue);

            if (!leaveTypeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave type not found");
            }

            // Validate dates
            if (model.ToDate.Date < model.FromDate.Date)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "ToDate must be greater than or equal to FromDate");
            }

            // Calculate total days if not provided
            decimal totalDays = model.TotalDays ?? (decimal)((model.ToDate.Date - model.FromDate.Date).TotalDays + 1);

            // Check overlapping leave requests for the same employee
            bool overlap = await repo.Entities
                .AnyAsync(x =>
                    x.EmployeeId == model.EmployeeId &&
                    !x.DeletedTime.HasValue &&
                    x.FromDate.Date <= model.ToDate.Date &&
                    x.ToDate.Date >= model.FromDate.Date);

            if (overlap)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Employee already has a leave request overlapping with the specified period");
            }

            string currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            LeaveRequest entity = model.ToEntity();
            entity.TotalDays = totalDays;
            entity.CreatedBy = currentUser;
            entity.CreatedTime = CoreHelper.SystemTimeNow;
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await repo.InsertAsync(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateAsync(UpdateLeaveRequestModelView model)
        {
            IGenericRepository<LeaveRequest> repo = _unitOfWork.GetRepository<LeaveRequest>();

            LeaveRequest entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == model.Id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave request not found");

            // Validate employee
            IGenericRepository<Employee> employeeRepo = _unitOfWork.GetRepository<Employee>();
            bool employeeExists = await employeeRepo.Entities
                .AnyAsync(x => x.Id == model.EmployeeId && !x.DeletedTime.HasValue);

            if (!employeeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Employee not found");
            }

            // Validate leave type
            IGenericRepository<LeaveType> leaveTypeRepo = _unitOfWork.GetRepository<LeaveType>();
            bool leaveTypeExists = await leaveTypeRepo.Entities
                .AnyAsync(x => x.Id == model.LeaveTypeId && !x.DeletedTime.HasValue);

            if (!leaveTypeExists)
            {
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave type not found");
            }

            // Validate dates
            if (model.ToDate.Date < model.FromDate.Date)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "INVALID_INPUT",
                    "ToDate must be greater than or equal to FromDate");
            }

            // Check overlapping (exclude current record)
            bool overlap = await repo.Entities
                .AnyAsync(x =>
                    x.Id != model.Id &&
                    x.EmployeeId == model.EmployeeId &&
                    !x.DeletedTime.HasValue &&
                    x.FromDate.Date <= model.ToDate.Date &&
                    x.ToDate.Date >= model.FromDate.Date);

            if (overlap)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    "DUPLICATE",
                    "Another leave request overlaps with the specified period");
            }

            decimal totalDays = model.TotalDays ?? (decimal)((model.ToDate.Date - model.FromDate.Date).TotalDays + 1);

            string currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            model.ToEntity(entity);
            entity.TotalDays = totalDays;
            entity.LastUpdatedBy = currentUser;
            entity.LastUpdatedTime = CoreHelper.SystemTimeNow;

            await repo.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task SoftDeleteAsync(Guid id)
        {
            IGenericRepository<LeaveRequest> repo = _unitOfWork.GetRepository<LeaveRequest>();

            LeaveRequest entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id && !x.DeletedTime.HasValue)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave request not found");

            string currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            entity.DeletedBy = currentUser;
            entity.DeletedTime = CoreHelper.SystemTimeNow;

            await repo.UpdateAsync(entity);
            await _unitOfWork.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            IGenericRepository<LeaveRequest> repo = _unitOfWork.GetRepository<LeaveRequest>();

            LeaveRequest entity = await repo.Entities
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    "NOT_FOUND",
                    "Leave request not found");

            await repo.DeleteAsync(entity);
            await _unitOfWork.SaveAsync();
        }
    }
}
