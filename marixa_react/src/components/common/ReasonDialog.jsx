import { useId, useState } from "react";
import useModalDialog from "./useModalDialog";
import "../../modules/me/workforce.css";

export default function ReasonDialog({ title, hint, minLength = 5, maxLength = 1000, busy, error, onClose, onSubmit }) {
    const [reason, setReason] = useState("");
    const ref = useModalDialog();
    const titleId = useId();
    return <dialog className="wf-dialog" ref={ref} aria-labelledby={titleId}
        onCancel={(event) => { event.preventDefault(); if (!busy) onClose(); }}>
        <form onSubmit={(event) => { event.preventDefault(); onSubmit(reason.trim()); }}>
            <h2 id={titleId}>{title}</h2>
            {hint && <p>{hint}</p>}
            <label>Lý do / ý kiến {minLength ? `(tối thiểu ${minLength} ký tự)` : "(tùy chọn)"}
                <textarea autoFocus rows={4} value={reason} onChange={(event) => setReason(event.target.value)}
                    required={minLength > 0} minLength={minLength} maxLength={maxLength} disabled={busy} />
            </label>
            {error && <p className="att-error" role="alert">{error}</p>}
            <div className="wf-actions"><button type="button" className="wf-btn" disabled={busy} onClick={onClose}>Đóng</button>
                <button className="wf-btn wf-btn-primary" disabled={busy || reason.trim().length < minLength}>
                    {busy ? "Đang xử lý..." : "Xác nhận"}
                </button></div>
        </form>
    </dialog>;
}
