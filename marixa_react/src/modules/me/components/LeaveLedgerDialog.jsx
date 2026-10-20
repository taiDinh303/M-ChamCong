import ResponsiveTable from "../../../components/common/ResponsiveTable.jsx";
import { useCallback, useEffect, useId, useRef, useState } from "react";
import { getAuth } from "../../../services/auth/auth";
import { apiErrorMessage, dataOf, itemsOf } from "../../../services/api/contracts";
import { formatVnDate, vnTodayKey } from "../../../utils/vnTime";
import employeeApi from "../../hr/api/employeeApi";
import relatedApi from "../api/relatedApi";
import { workforceApi } from "../api/workforceApi";
import useModalDialog from "../../../components/common/useModalDialog";
import "../workforce.css";

const ENTRY = { 1: "Cấp phép", 2: "Điều chỉnh", 3: "Trừ theo đơn", 4: "Hoàn theo đơn" };
function LedgerEntryForm({ employeeId, year, types, onClose, onSaved }) {
    const ref = useModalDialog();
    const titleId = useId();
    const [form, setForm] = useState({ entryType: 1, days: "", reason: "", leaveTypeId: "" });
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const save = async (event) => {
        event.preventDefault();
        if (!Number.isFinite(Number(form.days)) || Number(form.days) === 0) { setError("Số ngày phải khác 0."); return; }
        setBusy(true); setError("");
        try {
            await workforceApi.createLedgerEntry({ ...form, employeeId, year: Number(year), days: Number(form.days), reason: form.reason.trim(), leaveTypeId: form.leaveTypeId || null });
            onSaved();
        } catch (err) { setError(apiErrorMessage(err)); }
        finally { setBusy(false); }
    };
    return <dialog ref={ref} className="wf-dialog" aria-labelledby={titleId} onCancel={(event) => { event.preventDefault(); if (!busy) onClose(); }}>
        <form onSubmit={save}><h2 id={titleId}>Cấp / điều chỉnh phép năm {year}</h2>
            <p className="wf-note">Giao dịch được ghi thêm vào sổ phép. Trừ và hoàn phép của đơn nghỉ được xử lý qua duyệt/hủy đơn.</p>
            <label>Loại giao dịch<select autoFocus value={form.entryType} onChange={(e) => setForm({ ...form, entryType: Number(e.target.value), days: "" })}>
                <option value={1}>Cấp phép</option><option value={2}>Điều chỉnh tăng / giảm</option></select></label>
            <label>Loại phép<select value={form.leaveTypeId} onChange={(e) => setForm({ ...form, leaveTypeId: e.target.value })}><option value="">Không phân loại</option>
                {types.map((type) => <option key={type.id} value={type.id}>{type.name}</option>)}</select></label>
            <label>Số ngày {form.entryType === 2 ? "(số âm để giảm)" : "(số dương)"}<input type="number" step="0.01" min={form.entryType === 1 ? 0.01 : -999} max={999}
                required value={form.days} onChange={(e) => setForm({ ...form, days: e.target.value })} /></label>
            <label>Lý do<textarea required minLength={5} maxLength={500} rows={3} value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} /></label>
            {error && <p className="att-error" role="alert">{error}</p>}
            <div className="wf-actions"><button type="button" className="wf-btn" disabled={busy} onClick={onClose}>Đóng</button>
                <button className="wf-btn wf-btn-primary" disabled={busy || form.reason.trim().length < 5}>{busy ? "Đang ghi..." : "Ghi giao dịch"}</button></div>
        </form>
    </dialog>;
}

export default function LeaveLedgerDialog({ employeeId, manage = false, onClose, onChanged }) {
    const ref = useModalDialog();
    const titleId = useId();
    const allowed = manage && getAuth()?.roles?.some((role) => ["Admin", "HR"].includes(role));
    const [employees, setEmployees] = useState([]);
    const [selected, setSelected] = useState(employeeId || "");
    const [year, setYear] = useState(Number(vnTodayKey().slice(0, 4)));
    const [types, setTypes] = useState([]);
    const [entries, setEntries] = useState([]);
    const [balance, setBalance] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [creating, setCreating] = useState(false);
    const [optionsError, setOptionsError] = useState("");
    const request = useRef(0);
    useEffect(() => {
        if (!allowed) return;
        let active = true;
        Promise.all([employeeApi.employees(), relatedApi.leaveTypesAll()]).then(([people, leaveTypes]) => {
            if (!active) return;
            setEmployees(itemsOf(people)); setTypes(itemsOf(leaveTypes).filter((type) => type.isActive !== false));
        }).catch((err) => { if (active) setOptionsError(apiErrorMessage(err)); });
        return () => { active = false; };
    }, [allowed]);
    const load = useCallback(async () => {
        const current = ++request.current;
        setLoading(true); setError(""); setBalance(null); setEntries([]);
        if (!selected || !year || year < 1 || year > 9999) { setLoading(false); return; }
        try {
            const [summary, rows] = await Promise.all([workforceApi.ledgerSummary(selected, year), workforceApi.ledger(selected, year)]);
            if (current !== request.current) return;
            setBalance(dataOf(summary)); setEntries(itemsOf(rows));
        } catch (err) { if (current === request.current) setError(apiErrorMessage(err)); }
        finally { if (current === request.current) setLoading(false); }
    }, [selected, year]);
    useEffect(() => { load(); return () => { request.current += 1; }; }, [load]);
    return <dialog ref={ref} className="wf-dialog wf-ledger-dialog" aria-labelledby={titleId} onCancel={(event) => { event.preventDefault(); onClose(); }}>
        <div className="wf-heading"><h2 id={titleId}>Sổ phép</h2><button autoFocus type="button" className="wf-btn" onClick={onClose}>Đóng</button></div>
        <div className="wf-actions">
            {allowed && <label>Nhân viên<select value={selected} onChange={(e) => setSelected(e.target.value)}><option value="">Chọn nhân viên</option>
                {employees.map((employee) => <option value={employee.id} key={employee.id}>{employee.employeeCode} · {employee.fullName}</option>)}</select></label>}
            <label>Năm<input type="number" min={1} max={9999} value={year} onChange={(e) => setYear(Number(e.target.value))} /></label>
            <button type="button" className="wf-btn" disabled={loading} onClick={load}>Tải lại sổ phép</button>
            {allowed && selected && !loading && !error && !optionsError && balance && <button type="button" className="wf-btn wf-btn-primary" onClick={() => setCreating(true)}>Cấp / điều chỉnh phép</button>}
        </div>
        {error && <p className="att-error" role="alert">{error}</p>}
        {optionsError && <p className="att-error" role="alert">Không tải được danh sách nhân viên / loại phép: {optionsError}</p>}
        {!selected && <p>Chọn nhân viên để xem và điều chỉnh sổ phép.</p>}
        {loading ? <p>Đang tải sổ phép...</p> : !error && selected && balance && <>
            {balance && <p>Đã cấp: <strong>{balance.totalGranted}</strong> · Đã dùng: <strong>{balance.totalUsed}</strong> · Còn lại: <strong>{balance.remaining}</strong> ngày</p>}
            <div className="att-table-wrap"><ResponsiveTable className="att-table"><thead><tr><th>Ngày ghi</th><th>Loại</th><th>Số ngày</th><th>Lý do</th><th>Người ghi</th></tr></thead>
                <tbody>{entries.map((entry) => <tr key={entry.id}><td>{formatVnDate(entry.createdTime)}</td><td>{ENTRY[entry.entryType] || "Không rõ"}</td>
                    <td>{entry.days}</td><td>{entry.reason || entry.leaveRequestReason || "—"}</td><td>{entry.createdBy || "—"}</td></tr>)}
                    {entries.length === 0 && <tr><td colSpan={5}>Chưa có giao dịch sổ phép trong năm được chọn.</td></tr>}</tbody></ResponsiveTable></div>
        </>}
        {creating && <LedgerEntryForm employeeId={selected} year={year} types={types} onClose={() => setCreating(false)} onSaved={() => { setCreating(false); load(); onChanged?.(); }} />}
    </dialog>;
}
