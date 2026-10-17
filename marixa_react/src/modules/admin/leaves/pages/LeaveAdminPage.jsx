import AdminListPage from "../../AdminListPage";
import adminApi from "../../api/adminApi";
import { useState } from "react";
import { getAuth } from "../../../../services/auth/auth";
import employeeApi from "../../../hr/api/employeeApi";
import relatedApi from "../../../me/api/relatedApi";
import ReasonDialog from "../../../../components/common/ReasonDialog";
import { downloadResponse } from "../../../../utils/download";
import { apiErrorMessage } from "../../../../services/api/contracts";
import LeaveLedgerDialog from "../../../me/components/LeaveLedgerDialog";

// ===== Trang Nghỉ phép (quản trị) =====
const LeaveAdminPage = ({ hrMode = false }) => {
    const auth = getAuth();
    const [cancellation, setCancellation] = useState(null);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [ledgerOpen, setLedgerOpen] = useState(false);
    const approve = (row) => employeeApi.approveLeave(row.id, "");
    const reject = async (row) => {
        const note = window.prompt("Ý kiến từ chối (tùy chọn):");
        if (note !== null) await employeeApi.rejectLeave(row.id, note);
    };
    const pdf = async (row) => {
        try { downloadResponse(await relatedApi.leavePdf(row.id), `Don-nghi-${row.id}.pdf`); }
        catch (err) { window.alert(apiErrorMessage(err)); }
    };
    const cancel = async (reason) => {
        setBusy(true); setError("");
        try { await employeeApi.cancelLeave(cancellation.row.id, reason); await cancellation.reload(); setCancellation(null); }
        catch (err) { setError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };

    const LABELS = { 1: "Chờ duyệt", 2: "Đã duyệt", 3: "Từ chối", 4: "Đã hủy" };
    const TONES = { 1: "warn", 2: "ok", 3: "bad", 4: "muted" };

    return (
        <>
        <AdminListPage
            toolbarActions={auth?.roles?.some((role) => ["Admin", "HR"].includes(role)) && <button type="button" className="wf-btn" onClick={() => setLedgerOpen(true)}>Sổ phép · Cấp / điều chỉnh</button>}
            title="Nghỉ phép"
            subtitle="Duyệt / từ chối các đơn xin nghỉ phép"
            fetcher={adminApi.leaveRequests}
            columns={[
                { key: "employeeName", label: "Nhân viên" },
                { key: "leaveTypeName", label: "Loại phép" },
                { key: "fromDate", label: "Từ", date: true },
                { key: "toDate", label: "Đến", date: true },
                { key: "totalDays", label: "Số ngày" },
                { key: "reason", label: "Lý do" },
                {
                    key: "status",
                    label: "Trạng thái",
                    badge: (v) => TONES[v] || "muted",
                    labelOf: (v) => LABELS[v] || v,
                },
            ]}
            searchKeys={["employeeName", "leaveTypeName", "reason"]}
            searchPlaceholder="Tìm nhân viên, loại phép, lý do..."
            filter={{
                key: "status",
                options: [
                    ["1", "Chờ duyệt"],
                    ["2", "Đã duyệt"],
                    ["3", "Từ chối"],
                    ["4", "Đã hủy"],
                ],
            }}
            approve={approve}
            reject={reject}
            pendingOf={(r) => r.status === 1 && r.employeeId !== auth?.employeeId}
            rowActions={(row, reload) => <div className="wf-actions">
                <button type="button" className="wf-btn" onClick={() => pdf(row)}>PDF</button>
                {row.status === 2 && row.employeeId !== auth?.employeeId && auth?.roles?.some((role) => ["Admin", "HR"].includes(role)) &&
                    <button type="button" className="wf-btn" onClick={() => { setError(""); setCancellation({ row, reload }); }}>Hủy đơn đã duyệt</button>}
            </div>}
            emptyText="Không có đơn nghỉ phép nào."
            hrMode={hrMode}
        />
        {cancellation && <ReasonDialog title="Hủy đơn nghỉ đã duyệt" maxLength={500} busy={busy} error={error} onClose={() => setCancellation(null)} onSubmit={cancel} />}
        {ledgerOpen && <LeaveLedgerDialog employeeId={auth?.employeeId} manage onClose={() => setLedgerOpen(false)} />}
        </>
    );
};

export default LeaveAdminPage;
