using M.Contract.Repositories.Entities;
using M.Contract.Repositories.Persistence;
using M.Contract.Services.Export;
using M.Contract.Services.Interface;
using M.Contract.Services.Storage;
using M.Core.Security;
using M.ModelViews.AttendancePeriodModelView;
using Microsoft.EntityFrameworkCore;
using static M.Core.Base.BaseException;

namespace M.Services.Services.Leave;

public sealed class LeaveDocumentService(IUnitOfWork uow, ICurrentUser actor, TimeProvider clock,
    IWorkforceExportWriter exports) : BaseService(uow, actor, clock), ILeaveDocumentService
{
    public async Task<DocumentDownload> PdfAsync(Guid id)
    {
        var request = await Repository<LeaveRequest>().Entities.AsNoTracking().Include(r => r.Employee)
            .Include(r => r.LeaveType).Include(r => r.Approver)
            .SingleOrDefaultAsync(r => r.Id == id && !r.DeletedTime.HasValue)
            ?? throw new ErrorException(404, "NOT_FOUND", "Leave request not found.");
        if (!CurrentUser.IsAuthenticated || (CurrentUser.EmployeeId != request.EmployeeId
                && !CurrentUser.IsInRole(RoleNames.Admin) && !CurrentUser.IsInRole(RoleNames.HR)))
            throw new ErrorException(403, "FORBIDDEN", "Leave data is outside your access scope.");
        var data = new LeavePdfData(request.Id, request.Employee?.EmployeeCode ?? "", request.Employee?.FullName ?? "",
            request.LeaveType?.Name ?? "", request.FromDate, request.ToDate, request.TotalDays, request.Reason,
            request.Status, request.Approver?.FullName, request.ApprovedAt, request.LastUpdatedTime, UtcNow)
            { CancelledAt = request.CancelledAt, CancellationReason = request.CancellationReason };
        return new DocumentDownload(new MemoryStream(exports.LeavePdf(data)), "application/pdf", $"Marixa-leave-{request.Id:N}.pdf");
    }
}
