import { useCallback, useEffect, useId, useState } from "react";
import { getAuth } from "../../../services/auth/auth";
import { apiErrorMessage, itemsOf } from "../../../services/api/contracts";
import { formatVnDate, formatVnTime, vnTodayKey } from "../../../utils/vnTime";
import { workforceApi } from "../api/workforceApi";
import ReasonDialog from "../../../components/common/ReasonDialog";
import useModalDialog from "../../../components/common/useModalDialog";
import "../workforce.css";

const STATUS = { 1: "Chờ duyệt", 2: "Đã duyệt", 3: "Từ chối", 4: "Đã hủy" };
const inputTime = (iso) => iso ? `${vnTodayKey(new Date(iso))}T${formatVnTime(iso)}` : "";
function OvertimeForm({ row, employeeId, onClose, onSaved }) {
    const ref = useModalDialog();
    const titleId = useId();
    const today = vnTodayKey();
    const [form, setForm] = useState({ start: inputTime(row?.startAt) || `${today}T17:00`, end: inputTime(row?.endAt) || `${today}T19:00`, reason: row?.reason || "" });
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const save = async (event) => {
        event.preventDefault(); setError("");
        const start = new Date(`${form.start}:00+07:00`);
        const end = new Date(`${form.end}:00+07:00`);
        const duration = end - start;
        if (!Number.isFinite(duration) || duration <= 0 || duration > 86400000) {
            setError("Giờ kết thúc phải sau giờ bắt đầu, thời lượng tối đa 24 giờ."); return;
        }
        setBusy(true);
        const payload = { employeeId: row?.employeeId || employeeId, attendanceId: row?.attendanceId || null,
            workDate: form.start.slice(0, 10), startAt: start.toISOString(), endAt: end.toISOString(), reason: form.reason.trim() };
        try {
            if (row) await workforceApi.updateOvertime({ ...payload, id: row.id });
            else await workforceApi.createOvertime(payload);
            onSaved();
        } catch (err) { setError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    return <dialog ref={ref} className="wf-dialog" aria-labelledby={titleId} onCancel={(e) => { e.preventDefault(); if (!busy) onClose(); }}>
        <form onSubmit={save}><h2 id={titleId}>{row ? "Sửa đơn tăng ca" : "Đăng ký tăng ca"}</h2>
            <p className="wf-note">Chọn thời gian theo giờ Việt Nam (UTC+7). Giờ OT được công nhận dựa trên thời gian làm thực tế trong khung tăng ca đã duyệt.</p>
            <div className="wf-grid"><label>Bắt đầu<input autoFocus type="datetime-local" required value={form.start} onChange={(e) => setForm({ ...form, start: e.target.value })} /></label>
                <label>Kết thúc<input type="datetime-local" required value={form.end} onChange={(e) => setForm({ ...form, end: e.target.value })} /></label></div>
            <label>Lý do<textarea rows={3} maxLength={500} value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} /></label>
            {error && <p className="att-error" role="alert">{error}</p>}
            <div className="wf-actions"><button type="button" className="wf-btn" disabled={busy} onClick={onClose}>Đóng</button>
                <button className="wf-btn wf-btn-primary" disabled={busy}>{busy ? "Đang gửi..." : "Gửi đơn"}</button></div>
        </form>
    </dialog>;
}

export default function OvertimePanel({ reviewMode = false, onChanged }) {
    const auth = getAuth();
    const employeeId = auth?.employeeId;
    const roles = auth?.roles || [];
    const privileged = roles.some((role) => ["Admin", "HR"].includes(role));
    const canReview = roles.some((role) => ["Admin", "HR", "Manager"].includes(role));
    const [rows, setRows] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [filter, setFilter] = useState("");
    const [form, setForm] = useState(null);
    const [decision, setDecision] = useState(null);
    const [busy, setBusy] = useState(false);
    const [decisionError, setDecisionError] = useState("");
    const [notice, setNotice] = useState("");
    const load = useCallback(async () => {
        if (!reviewMode && !employeeId) { setLoading(false); return; }
        setLoading(true); setError("");
        try { setRows(itemsOf(await workforceApi.overtime(employeeId, reviewMode))); }
        catch (err) { setError(apiErrorMessage(err)); }
        finally { setLoading(false); }
    }, [employeeId, reviewMode]);
    useEffect(() => { load(); }, [load]);
    const review = async (note) => {
        setBusy(true); setDecisionError("");
        try {
            await workforceApi.reviewOvertime(decision.row.id, decision.status, employeeId, note);
            setDecision(null); setNotice("Đã xử lý đơn tăng ca."); await load(); onChanged?.();
        } catch (err) { setDecisionError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    return <section className="att-card wf-panel" aria-label="Đơn tăng ca">
        <div className="wf-heading"><h2>{reviewMode ? "Quản lý đơn tăng ca" : "Đơn tăng ca của tôi"}</h2>
            {employeeId && <button type="button" className="wf-btn wf-btn-primary" onClick={() => setForm({})}>Đăng ký tăng ca</button>}</div>
        <p className="wf-note">Khung giờ đăng ký và giờ OT được công nhận là hai số liệu riêng. Xem giờ OT thực tế trong lịch sử/thống kê công.</p>
        {!employeeId && <p>Tài khoản chưa liên kết hồ sơ nhân viên.</p>}
        {notice && <p role="status">{notice}</p>}
        {error && <p className="att-error" role="alert">{error} <button type="button" className="wf-btn" onClick={load}>Thử lại</button></p>}
        <label>Lọc trạng thái<select value={filter} onChange={(e) => setFilter(e.target.value)}><option value="">Tất cả</option>
            {Object.entries(STATUS).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
        {loading ? <p>Đang tải đơn tăng ca...</p> : <div className="att-table-wrap"><table className="att-table"><thead><tr>
            {reviewMode && <th>Nhân viên</th>}<th>Ngày làm</th><th>Khung giờ đăng ký (VN)</th><th>Trạng thái</th><th>Lý do / ý kiến</th><th>Thao tác</th>
        </tr></thead><tbody>{rows.filter((row) => !filter || String(row.status) === filter).map((row) => <tr key={row.id}>
            {reviewMode && <td>{row.employeeName || row.employeeCode}</td>}<td>{formatVnDate(row.workDate)}</td>
            <td>{formatVnDate(row.startAt)} {formatVnTime(row.startAt)}<br />→ {formatVnDate(row.endAt)} {formatVnTime(row.endAt)}</td>
            <td><span className="att-badge info">{STATUS[row.status] || "Không rõ"}</span></td><td>{row.reason || "—"}{row.note && <p>{row.note}</p>}</td>
            <td><div className="wf-actions">
                {row.status === 1 && row.employeeId === employeeId && <button type="button" className="wf-btn" onClick={() => setForm(row)}>Sửa</button>}
                {reviewMode && canReview && employeeId && row.status === 1 && (privileged || row.employeeId !== employeeId) && <>
                    <button type="button" className="wf-btn" onClick={() => { setDecisionError(""); setDecision({ row, status: 2 }); }}>Duyệt</button>
                    <button type="button" className="wf-btn" onClick={() => { setDecisionError(""); setDecision({ row, status: 3 }); }}>Từ chối</button>
                </>}
            </div></td>
        </tr>)}{!rows.some((row) => !filter || String(row.status) === filter) && <tr><td colSpan={reviewMode ? 6 : 5}>Không có đơn tăng ca phù hợp.</td></tr>}</tbody></table></div>}
        {form && <OvertimeForm row={form.id ? form : null} employeeId={employeeId} onClose={() => setForm(null)} onSaved={() => { setForm(null); setNotice("Đã gửi đơn tăng ca."); load(); }} />}
        {decision && <ReasonDialog title={decision.status === 2 ? "Duyệt tăng ca" : "Từ chối tăng ca"} minLength={0} maxLength={500}
            busy={busy} error={decisionError} onClose={() => setDecision(null)} onSubmit={review} />}
    </section>;
}
