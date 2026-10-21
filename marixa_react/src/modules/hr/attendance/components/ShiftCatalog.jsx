import { useState } from "react";
import employeeAttendanceApi from "../api/employeeAttendanceApi";
import { apiErrorMessage } from "../../../../services/api/contracts";

const blankShift = () => ({ code: "", name: "", startTime: "08:00", endTime: "17:00", standardHours: 8, breakMinutes: 60, workDays: 63 });
const weekdays = ["Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy", "Chủ nhật"];

export default function ShiftCatalog({ shifts, canManage, onUpdated }) {
    const [draft, setDraft] = useState(blankShift);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [formError, setFormError] = useState("");
    const [notice, setNotice] = useState("");
    const set = (key, value) => setDraft((old) => ({ ...old, [key]: value }));
    const save = async (event) => {
        event.preventDefault();
        if (busy) return;
        setNotice("");
        setError(""); setFormError("");
        if (draft.startTime === draft.endTime) { setFormError("Giờ vào và giờ ra ca không được trùng nhau."); return; }
        setBusy(true); setError(""); setNotice("");
        try {
            await employeeAttendanceApi.createShift({ ...draft, code: draft.code.trim(), name: draft.name.trim(),
                startTime: `${draft.startTime}:00`, endTime: `${draft.endTime}:00`,
                isNight: draft.endTime < draft.startTime, isActive: true });
            setDraft(blankShift()); setNotice("Đã tạo ca làm.");
            await onUpdated();
        } catch (err) { setFormError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    const toggle = async (shift) => {
        if (busy) return;
        setBusy(true); setError(""); setNotice("");
        try {
            await employeeAttendanceApi.updateShift({ ...shift, isActive: !shift.isActive });
            setNotice(shift.isActive ? "Đã ngừng nhận lịch mới cho ca; giữ các lịch đã gán." : "Đã cho phép gán lịch mới cho ca.");
            await onUpdated();
        } catch (err) { setError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    return <details className="att-card shift-catalog">
        <summary>Danh mục ca làm ({shifts.length})</summary>
        <p className="att-muted">Ca dùng để xếp lịch kế hoạch. Quy tắc tính công chung vẫn được quản lý tại mục Quy định.</p>
        {error && <div role="alert" className="att-error">{error}</div>}
        {notice && <p role="status">{notice}</p>}
        <ul className="shift-catalog-list">
            {shifts.map((shift) => <li key={shift.id}>
                <div><strong>{shift.code} · {shift.name}</strong><p>{String(shift.startTime).slice(0, 5)}–{String(shift.endTime).slice(0, 5)} · Nghỉ {shift.breakMinutes} phút · {shift.isActive ? "Đang nhận lịch mới" : "Ngừng nhận lịch mới"}</p></div>
                {canManage && <button type="button" className="admin-link-btn" disabled={busy} onClick={() => toggle(shift)}>{shift.isActive ? "Ngừng áp dụng" : "Áp dụng lại"}</button>}
            </li>)}
        </ul>
        {shifts.length === 0 && <p>Chưa có ca làm. Admin hoặc HR có thể tạo ca bên dưới.</p>}
        {canManage && <form onSubmit={save}>
            <h2>Tạo ca làm</h2>
            <div className="work-schedule-assignment-grid">
                <label>Mã ca<input required maxLength={50} value={draft.code} onChange={(e) => set("code", e.target.value)} /></label>
                <label>Tên ca<input required maxLength={150} value={draft.name} onChange={(e) => set("name", e.target.value)} /></label>
                <label>Giờ vào<input required type="time" value={draft.startTime} onChange={(e) => set("startTime", e.target.value)} /></label>
                <label>Giờ ra<input required type="time" value={draft.endTime} onChange={(e) => set("endTime", e.target.value)} /></label>
                <label>Giờ chuẩn<input required type="number" min="1" max="24" value={draft.standardHours} onChange={(e) => set("standardHours", Number(e.target.value))} /></label>
                <label>Phút nghỉ<input required type="number" min="0" max="1439" value={draft.breakMinutes} onChange={(e) => set("breakMinutes", Number(e.target.value))} /></label>
            </div>
            <fieldset className="shift-catalog-weekdays"><legend>Ngày làm việc</legend>{weekdays.map((label, index) => <label key={label}><input type="checkbox" checked={Boolean(draft.workDays & (1 << index))} onChange={() => set("workDays", draft.workDays ^ (1 << index))} />{label}</label>)}</fieldset>
            <p className="att-muted">Giờ ra trước giờ vào là ca qua đêm. Khoảng ngày gán vẫn theo ngày bắt đầu ca.</p>
            {formError && <div role="alert" className="att-error">{formError}</div>}
            <button className="work-schedule-add-btn" type="submit" disabled={busy || !draft.workDays}>{busy ? "Đang lưu…" : "Tạo ca"}</button>
        </form>}
    </details>;
}
