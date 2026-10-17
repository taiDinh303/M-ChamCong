using M.Contract.Services.Storage;
using M.ModelViews.AttendancePeriodModelView;

namespace M.Contract.Services.Interface;

public interface IAttendancePeriodService
{
    Task<PeriodView> GetAsync(int year, int month);
    Task<IReadOnlyList<PeriodEventView>> HistoryAsync(int year, int month);
    Task<PeriodView> ReviewAsync(int year, int month, string reason);
    Task<PeriodView> LockAsync(int year, int month, string reason);
    Task<PeriodView> ReopenAsync(int year, int month, string reason);
    Task<SnapshotView> SnapshotAsync(int year, int month, int? version = null);
    Task<DocumentDownload> ExportAsync(int year, int month, int? version = null);
}
