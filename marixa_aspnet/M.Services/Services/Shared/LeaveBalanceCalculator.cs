using M.Contract.Repositories.Entities;

namespace M.Services.Services;

public sealed record LeaveBalance(decimal TotalGranted, decimal TotalUsed, decimal Remaining);

public static class LeaveBalanceCalculator
{
    public static LeaveBalance Calculate(IEnumerable<LeaveLedger> entries)
    {
        decimal granted = 0, used = 0;
        foreach (var entry in entries.Where(e => !e.DeletedTime.HasValue))
        {
            switch (entry.EntryType)
            {
                case LeaveLedgerEntryType.Grant:
                case LeaveLedgerEntryType.Adjust: granted += entry.Days; break;
                case LeaveLedgerEntryType.Deduct: used += entry.Days; break;
                case LeaveLedgerEntryType.Refund: used -= entry.Days; break;
            }
        }
        return new LeaveBalance(Math.Max(0, granted), Math.Max(0, used), granted - used);
    }
}
