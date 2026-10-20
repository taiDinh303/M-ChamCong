import ResponsiveTable from "../../../components/common/ResponsiveTable.jsx";
import { useCallback, useEffect, useId, useState } from "react";
import { getAuth } from "../../../services/auth/auth";
import { apiErrorMessage, itemsOf } from "../../../services/api/contracts";
import { formatVnDate, formatVnTime, vnTodayKey } from "../../../utils/vnTime";
import { workforceApi } from "../api/workforceApi";
import ReasonDialog from "../../../components/common/ReasonDialog";
import useModalDialog from "../../../components/common/useModalDialog";
import "../workforce.css";

const STATUS = { 1: "Chờ duyệt", 2: "Đã duyệt", 3: "Từ chối", 4: "Đã hủy" };
const ATTENDANCE = { 1: "Có mặt", 2: "Đi trễ", 3: "Về sớm", 4: "Vắng", 5: "Nghỉ phép", 6: "Ngày lễ", 7: "Cuối tuần" };
const inputTime = (iso) => iso ? `${vnTodayKey(new Date(iso))}T${formatVnTime(iso)}` : "";
function CorrectionForm({ record, employeeId, onClose, onSaved }) {
    const ref = useModalDialog();
    const titleId = useId();
    const [form, setForm] = useState({ workDate: record?.attendanceDate?.slice(0, 10) || vnTodayKey(),
        proposedCheckIn: inputTime(record?.checkInTime), proposedCheckOut: inputTime(record?.checkOutTime), proposedStatus: "", reason: "" });
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const change = (key) => (e) => setForm((old) => ({ ...old, [key]: e.target.value }));
    const submit = async (event) => {
        event.preventDefault(); setError("");
        if (!form.proposedCheckIn && !form.proposedCheckOut && !form.proposedStatus) {
            setError("Hãy đề nghị ít nhất một giờ vào, giờ ra hoặc trạng thái."); return;
        }
        setBusy(true);
        try {
            await workforceApi.createCorrection({ employeeId, attendanceId: record?.id || null, workDate: form.workDate,
                proposedCheckIn: form.proposedCheckIn ? new Date(`${form.proposedCheckIn}:00+07:00`).toISOString() : null,
                proposedCheckOut: form.proposedCheckOut ? new Date(`${form.proposedCheckOut}:00+07:00`).toISOString() : null,
                proposedStatus: form.proposedStatus ? Number(form.proposedStatus) : null, reason: form.reason.trim() });
            onSaved();
        } catch (err) { setError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    return <dialog ref={ref} className="wf-dialog" aria-labelledby={titleId} onCancel={(e) => { e.preventDefault(); if (!busy) onClose(); }}>
        <form onSubmit={submit}><h2 id={titleId}>Yêu cầu sửa công</h2>
            <p className="wf-note">Giá trị đề nghị chỉ áp dụng sau khi được duyệt. Nhập giờ Việt Nam (UTC+7); để trống giờ nếu muốn giữ nguyên.</p>
            <label>Ngày công<input autoFocus type="date" required disabled={Boolean(record?.id)} value={form.workDate} onChange={change("workDate")} /></label>
            <div className="wf-grid"><label>Giờ vào đề nghị<input type="datetime-local" value={form.proposedCheckIn} onChange={change("proposedCheckIn")} /></label>
                <label>Giờ ra đề nghị<input type="datetime-local" value={form.proposedCheckOut} onChange={change("proposedCheckOut")} /></label></div>
            <label>Trạng thái đề nghị<select value={form.proposedStatus} onChange={change("proposedStatus")}><option value="">Giữ nguyên / theo giờ chấm công</option>
                {Object.entries(ATTENDANCE).map(([key, label]) => <option value={key} key={key}>{label}</option>)}</select></label>
            <label>Lý do<textarea rows={3} maxLength={500} value={form.reason} onChange={change("reason")} /></label>
            {error && <p className="att-error" role="alert">{error}</p>}
            <div className="wf-actions"><button type="button" className="wf-btn" disabled={busy} onClick={onClose}>Đóng</button>
                <button className="wf-btn wf-btn-primary" disabled={busy}>{busy ? "Đang gửi..." : "Gửi yêu cầu"}</button></div>
        </form>
    </dialog>;
}

export default function CorrectionPanel({ staff = false, draft = null, onDraftClosed, onChanged }) {
    const auth = getAuth();
    const employeeId = auth?.employeeId;
    const privileged = auth?.roles?.some((role) => ["Admin", "HR"].includes(role));
    const reviewer = auth?.roles?.some((role) => ["Admin", "HR", "Manager"].includes(role));
    const [rows, setRows] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [record, setRecord] = useState(draft);
    const [decision, setDecision] = useState(null);
    const [busy, setBusy] = useState(false);
    const [decisionError, setDecisionError] = useState("");
    const [notice, setNotice] = useState("");
    const load = useCallback(async () => {
        if (!staff && !employeeId) { setLoading(false); return; }
        setLoading(true); setError("");
        try { setRows(itemsOf(await workforceApi.corrections(employeeId, staff))); }
        catch (err) { setError(apiErrorMessage(err)); }
        finally { setLoading(false); }
    }, [employeeId, staff]);
    useEffect(() => { load(); }, [load]);
    const close = () => { setRecord(null); onDraftClosed?.(); };
    const review = async (note) => {
        setBusy(true); setDecisionError("");
        try {
            await workforceApi.reviewCorrection(decision.row.id, decision.status, employeeId, note);
            setDecision(null); setNotice("Đã xử lý yêu cầu sửa công."); await load(); onChanged?.();
        } catch (err) { setDecisionError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    return <section className="att-card wf-panel" aria-label="Yêu cầu sửa công">
        <div className="wf-heading"><h2>{staff ? "Duyệt yêu cầu sửa công" : "Yêu cầu sửa công của tôi"}</h2>
            {employeeId && <button type="button" className="wf-btn wf-btn-primary" onClick={() => setRecord({})}>Tạo yêu cầu sửa công</button>}</div>
        {notice && <p role="status">{notice}</p>}
        {error && <p className="att-error" role="alert">{error} <button type="button" className="wf-btn" onClick={load}>Thử lại</button></p>}
        {loading ? <p>Đang tải yêu cầu...</p> : <div className="att-table-wrap"><ResponsiveTable className="att-table"><thead><tr>
            {staff && <th>Nhân viên</th>}<th>Ngày công</th><th>Giờ / trạng thái đề nghị</th><th>Lý do / ý kiến</th><th>Kết quả</th><th>Thao tác</th>
        </tr></thead><tbody>{rows.map((row) => <tr key={row.id}>
            {staff && <td>{row.employeeName || row.employeeCode}</td>}<td>{formatVnDate(row.workDate)}</td>
            <td>Vào: {row.proposedCheckIn ? `${formatVnDate(row.proposedCheckIn)} ${formatVnTime(row.proposedCheckIn)}` : "Giữ nguyên"}<br />
                Ra: {row.proposedCheckOut ? `${formatVnDate(row.proposedCheckOut)} ${formatVnTime(row.proposedCheckOut)}` : "Giữ nguyên"}<br />
                {ATTENDANCE[row.proposedStatus] || "Theo giờ chấm công"}</td>
            <td>{row.reason || "—"}{row.reviewNote && <p>{row.reviewNote}</p>}</td><td><span className="att-badge info">{STATUS[row.status] || "Không rõ"}</span><br />{row.reviewerName}</td>
            <td>{staff && reviewer && employeeId && row.status === 1 && (privileged || row.employeeId !== employeeId) && <div className="wf-actions">
                <button type="button" className="wf-btn" onClick={() => { setDecisionError(""); setDecision({ row, status: 2 }); }}>Duyệt</button>
                <button type="button" className="wf-btn" onClick={() => { setDecisionError(""); setDecision({ row, status: 3 }); }}>Từ chối</button>
            </div>}</td>
        </tr>)}{rows.length === 0 && <tr><td colSpan={staff ? 6 : 5}>Chưa có yêu cầu sửa công.</td></tr>}</tbody></ResponsiveTable></div>}
        {record && <CorrectionForm record={record} employeeId={employeeId} onClose={close} onSaved={() => { close(); setNotice("Đã gửi yêu cầu sửa công."); load(); }} />}
        {decision && <ReasonDialog title={decision.status === 2 ? "Duyệt sửa công" : "Từ chối sửa công"} minLength={0} maxLength={500}
            busy={busy} error={decisionError} onClose={() => setDecision(null)} onSubmit={review} />}
    </section>;
}
