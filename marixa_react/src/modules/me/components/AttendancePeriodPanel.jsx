import { useCallback, useEffect, useRef, useState } from "react";
import { getAuth } from "../../../services/auth/auth";
import { apiErrorMessage, dataOf } from "../../../services/api/contracts";
import { downloadResponse } from "../../../utils/download";
import { formatVnDate } from "../../../utils/vnTime";
import { workforceApi } from "../api/workforceApi";
import ReasonDialog from "../../../components/common/ReasonDialog";
import "../workforce.css";

const STATE = ["Đang mở", "Đã kiểm tra", "Đã khóa"];
const ACTION = { review: "Xác nhận đã kiểm tra", lock: "Khóa kỳ công", reopen: "Mở lại kỳ công" };
export default function AttendancePeriodPanel({ year, month, staff = false, refreshKey = 0, onChanged }) {
    const roles = getAuth()?.roles || [];
    const admin = roles.includes("Admin");
    const canReview = admin || roles.includes("HR");
    const [period, setPeriod] = useState(null);
    const [history, setHistory] = useState([]);
    const [version, setVersion] = useState(0);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [action, setAction] = useState(null);
    const [actionError, setActionError] = useState("");
    const [notice, setNotice] = useState("");
    const request = useRef(0);
    const load = useCallback(async () => {
        const currentRequest = ++request.current;
        setLoading(true);
        setError("");
        setPeriod(null);
        setVersion(0);
        try {
            if (staff) {
                const [current, events] = await Promise.all([workforceApi.period(year, month), workforceApi.periodHistory(year, month)]);
                if (currentRequest !== request.current) return;
                const value = dataOf(current);
                setPeriod(value);
                setVersion(value.currentVersion);
                setHistory(dataOf(events) || []);
            } else {
                const current = dataOf(await workforceApi.snapshot(year, month));
                if (currentRequest !== request.current) return;
                setPeriod({ currentVersion: current.data.version });
                setVersion(current.data.version);
            }
        } catch (err) {
            if (currentRequest !== request.current) return;
            if (!staff && ["SNAPSHOT_NOT_FOUND", "NOT_FOUND"].includes(err.response?.data?.code)) setPeriod({ currentVersion: 0 });
            else setError(apiErrorMessage(err));
        } finally { if (currentRequest === request.current) setLoading(false); }
    }, [year, month, staff]);
    useEffect(() => { load(); return () => { request.current += 1; }; }, [load, refreshKey]);
    const apply = async (reason) => {
        setBusy(true); setActionError("");
        try {
            await workforceApi.periodAction(year, month, action, reason);
            setNotice(`${ACTION[action]} thành công.`);
            setAction(null);
            await load();
            onChanged?.();
        } catch (err) { setActionError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    const download = async () => {
        setBusy(true); setError("");
        try { downloadResponse(await workforceApi.exportPeriod(year, month, version), `Marixa-${year}${String(month).padStart(2, "0")}-v${version}.xlsx`); }
        catch (err) { setError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    return <section className="att-card wf-panel" aria-label="Kỳ công">
        <div className="wf-heading"><h2>Kỳ công {String(month).padStart(2, "0")}/{year}</h2>
            {period?.state != null && <span className="att-badge info">{STATE[period.state]}</span>}</div>
        {loading ? <p>Đang tải kỳ công...</p> : <>
            {error && <p className="att-error" role="alert">{error} <button type="button" className="wf-btn" onClick={load}>Thử lại</button></p>}
            {notice && <p role="status">{notice}</p>}
            {!error && <>
                <p className="att-muted">{period?.currentVersion > 0
                    ? "Bản xuất lấy từ bảng công đã khóa, giữ nguyên số liệu của phiên bản được chọn."
                    : "Chưa có phiên bản bảng công đã khóa trong tháng này."}</p>
                <div className="wf-actions">
                    {period?.currentVersion > 0 && <>
                        <label>Phiên bản <select value={version} onChange={(event) => setVersion(Number(event.target.value))} disabled={busy}>
                            {Array.from({ length: period.currentVersion }, (_, index) => index + 1).map((v) => <option key={v} value={v}>Phiên bản {v}</option>)}
                        </select></label>
                        <button type="button" className="wf-btn wf-btn-primary" disabled={busy} onClick={download}>Tải bảng công XLSX</button>
                    </>}
                    {staff && canReview && period?.state === 0 && <button type="button" className="wf-btn" disabled={busy} onClick={() => { setActionError(""); setAction("review"); }}>Đánh dấu đã kiểm tra</button>}
                    {staff && admin && period?.state === 1 && <button type="button" className="wf-btn wf-btn-primary" disabled={busy} onClick={() => { setActionError(""); setAction("lock"); }}>Khóa kỳ</button>}
                    {staff && admin && period?.state === 2 && <button type="button" className="wf-btn" disabled={busy} onClick={() => { setActionError(""); setAction("reopen"); }}>Mở lại kỳ</button>}
                </div>
                {staff && history.length > 0 && <details><summary>Lịch sử xử lý kỳ công</summary><ul className="wf-history">{history.map((event, index) => <li key={index}>
                    {formatVnDate(event.at)} · {ACTION[event.action?.toLowerCase()] || event.action} · {event.actorName}<br />{event.reason}
                </li>)}</ul></details>}
            </>}
        </>}
        {action && <ReasonDialog title={ACTION[action]} hint={action === "lock" ? "Kỳ đã khóa sẽ chặn thay đổi dữ liệu công và các đơn liên quan. Bản xuất được lưu thành phiên bản bất biến." : "Ý kiến sẽ được lưu trong lịch sử kỳ công."}
            busy={busy} error={actionError} onClose={() => setAction(null)} onSubmit={apply} />}
    </section>;
}
