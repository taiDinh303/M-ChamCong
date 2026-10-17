import { useId, useState } from "react";
import { workforceApi } from "../api/workforceApi";
import { apiErrorMessage } from "../../../services/api/contracts";
import useModalDialog from "../../../components/common/useModalDialog";
import "../workforce.css";

export default function RuleEditor({ rule, onClose, onSaved }) {
    const ref = useModalDialog();
    const titleId = useId();
    const [form, setForm] = useState({ ...rule, checkInTime: rule.checkInTime.slice(0, 5), checkOutTime: rule.checkOutTime.slice(0, 5), breakStartTime: (rule.breakStartTime || "12:00").slice(0, 5) });
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const update = (key, value) => setForm((old) => ({ ...old, [key]: value }));
    const save = async (event) => {
        event.preventDefault(); setBusy(true); setError("");
        try {
            await workforceApi.updateRule({ ...form, checkInTime: `${form.checkInTime}:00`, checkOutTime: `${form.checkOutTime}:00`, breakStartTime: `${form.breakStartTime}:00` });
            onSaved();
        } catch (err) { setError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    return <dialog ref={ref} className="wf-dialog" aria-labelledby={titleId} onCancel={(e) => { e.preventDefault(); if (!busy) onClose(); }}>
        <form onSubmit={save}><h2 id={titleId}>Chỉnh quy định chấm công</h2><p className="wf-note">Quy định chung đang áp dụng. Thay đổi có thể khiến kỳ chưa khóa cần kiểm tra lại; bảng công đã khóa vẫn giữ số liệu cũ.</p>
            <label>Tên quy định<input autoFocus required maxLength={150} value={form.name} onChange={(e) => update("name", e.target.value)} /></label>
            <div className="wf-grid">
                <label>Giờ vào<input type="time" required value={form.checkInTime} onChange={(e) => update("checkInTime", e.target.value)} /></label>
                <label>Giờ ra<input type="time" required value={form.checkOutTime} onChange={(e) => update("checkOutTime", e.target.value)} /></label>
                <label>Bắt đầu nghỉ<input type="time" required value={form.breakStartTime} onChange={(e) => update("breakStartTime", e.target.value)} /></label>
                <label>Số phút nghỉ<input type="number" required min={0} max={1440} value={form.breakMinutes} onChange={(e) => update("breakMinutes", Number(e.target.value))} /></label>
                <label>Giờ công chuẩn<input type="number" required min={1} max={24} value={form.standardHours} onChange={(e) => update("standardHours", Number(e.target.value))} /></label>
                <label>Dung sai đi trễ (phút)<input type="number" required min={0} max={1440} value={form.lateGraceMinutes} onChange={(e) => update("lateGraceMinutes", Number(e.target.value))} /></label>
                <label>Ngưỡng về sớm (phút)<input type="number" required min={0} max={1440} value={form.earlyLeaveThresholdMinutes} onChange={(e) => update("earlyLeaveThresholdMinutes", Number(e.target.value))} /></label>
            </div>
            <label>Mô tả<textarea maxLength={500} value={form.description || ""} onChange={(e) => update("description", e.target.value)} /></label>
            <label className="wf-inline-check"><input type="checkbox" checked={form.photoRequired} onChange={(e) => update("photoRequired", e.target.checked)} />Bắt buộc chụp ảnh</label>
            <label className="wf-inline-check"><input type="checkbox" checked={form.gpsRequired} onChange={(e) => update("gpsRequired", e.target.checked)} />Bắt buộc GPS</label>
            {error && <p className="att-error" role="alert">{error}</p>}
            <div className="wf-actions"><button type="button" className="wf-btn" disabled={busy} onClick={onClose}>Đóng</button>
                <button className="wf-btn wf-btn-primary" disabled={busy}>{busy ? "Đang lưu..." : "Lưu quy định"}</button></div>
        </form>
    </dialog>;
}
