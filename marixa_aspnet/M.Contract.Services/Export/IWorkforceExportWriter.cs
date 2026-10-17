using M.ModelViews.AttendancePeriodModelView;

namespace M.Contract.Services.Export;

public interface IWorkforceExportWriter
{
    byte[] AttendanceXlsx(PeriodSnapshot snapshot, string sha256);
    byte[] LeavePdf(LeavePdfData data);
}
