import ResponsiveTable from "../../../../components/common/ResponsiveTable.jsx";
import { useState, useEffect, useMemo, useCallback } from "react";
import HrAppLayout from "../../layout/HrAppLayout";
import AdminAppLayout from "../../../admin/layout/AdminAppLayout";
import HrHero from "../../HrHero";
import employeeApi from "../../api/employeeApi";
import { getAuth } from "../../../../services/auth/auth";
import relatedApi from "../../../me/api/relatedApi";
import { downloadResponse } from "../../../../utils/download";
import { apiErrorMessage } from "../../../../services/api/contracts";
import ReasonDialog from "../../../../components/common/ReasonDialog";
import LeaveLedgerDialog from "../../../me/components/LeaveLedgerDialog";
import "../../employee.css";
import "../leaves.css";

const LABELS = { 1: "Chờ duyệt", 2: "Đã duyệt", 3: "Từ chối", 4: "Đã hủy" };
const TONES = { 1: "warn", 2: "ok", 3: "bad", 4: "muted" };
const STATUS_FILTERS = [
    ["", "Tất cả"],
    ["1", "Chờ duyệt"],
    ["2", "Đã duyệt"],
    ["3", "Từ chối"],
    ["4", "Đã hủy"],
];

const AVA_COLORS = ["#2f6df6", "#16a085", "#e67e22", "#8e44ad", "#c0392b", "#1565c0", "#00838f", "#d8436b"];
const avaColor = (seed = "") => {
    let h = 0;
    for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) | 0;
    return AVA_COLORS[Math.abs(h) % AVA_COLORS.length];
};
const fmtDay = (v, locale) => (v ? new Date(v).toLocaleDateString(locale) : "—");

// ===== Trang Nghỉ phép (nhân sự) — giao diện riêng, mobile-friendly =====
const EmployeeLeavePage = ({ layout = "hr" }) => {
    const Layout = layout === "admin" ? AdminAppLayout : HrAppLayout;
    const auth = getAuth();
    const canCancelApproved = auth?.roles?.some((role) => ["Admin", "HR"].includes(role));
    const [cancellation, setCancellation] = useState(null);
    const [cancelError, setCancelError] = useState("");
    const [reviewAction, setReviewAction] = useState(null);
    const [reviewError, setReviewError] = useState("");
    const [ledgerOpen, setLedgerOpen] = useState(false);
    const [rows, setRows] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [search, setSearch] = useState("");
    const [status, setStatus] = useState("");
    const [busy, setBusy] = useState("");
    const [notice, setNotice] = useState("");

    const load = useCallback(async () => {
        setLoading(true);
        setError("");
        try {
            const res = await employeeApi.leaveRequests();
            setRows(res.data.data?.items || []);
        } catch (e) {
            setError(e.response?.data?.message || e.message || "Không tải được đơn nghỉ phép.");
        } finally {
            setLoading(false);
        }
    }, []);
    useEffect(() => { load(); }, [load]);

    const flash = (m) => { setNotice(m); setTimeout(() => setNotice(""), 3000); };

    const reviewLeave = async (note) => {
        if (!reviewAction || busy) return;
        const { row, kind } = reviewAction;
        setBusy(kind + "-" + row.id);
        setReviewError("");
        try {
            if (kind === "approve") await employeeApi.approveLeave(row.id, note);
            else await employeeApi.rejectLeave(row.id, note);
            setReviewAction(null);
            flash(`${kind === "approve" ? "Đã duyệt" : "Đã từ chối"} đơn của ${row.employeeName}.`);
            await load();
        } catch (e) {
            setReviewError(apiErrorMessage(e));
        } finally {
            setBusy("");
        }
    };

    const cancel = async (reason) => {
        setBusy("cancel"); setCancelError("");
        try { await employeeApi.cancelLeave(cancellation.id, reason); setCancellation(null); flash("Đã hủy đơn và ghi nhận hoàn phép theo sổ."); await load(); }
        catch (err) { setCancelError(apiErrorMessage(err)); }
        finally { setBusy(""); }
    };
    const pdf = async (row) => {
        try { downloadResponse(await relatedApi.leavePdf(row.id), `Don-nghi-${row.id}.pdf`); }
        catch (err) { setError(apiErrorMessage(err)); }
    };

    const filtered = useMemo(() => {
        const q = search.trim().toLowerCase();
        return rows.filter((r) => {
            if (status && String(r.status) !== status) return false;
            if (!q) return true;
            return `${r.employeeCode || ""} ${r.employeeName || ""} ${r.leaveTypeName || ""} ${r.reason || ""}`
                .toLowerCase().includes(q);
        });
    }, [rows, search, status]);

    const kpi = useMemo(() => ({
        total: rows.length,
        pending: rows.filter((r) => r.status === 1).length,
        approved: rows.filter((r) => r.status === 2).length,
        rejected: rows.filter((r) => r.status === 3).length,
    }), [rows]);

    const kpis = [
        { label: "Tổng đơn", value: kpi.total, tone: "blue", ico: "📄" },
        { label: "Chờ duyệt", value: kpi.pending, tone: "warn", ico: "⏳" },
        { label: "Đã duyệt", value: kpi.approved, tone: "green", ico: "✅" },
        { label: "Từ chối", value: kpi.rejected, tone: "red", ico: "⛔" },
    ];

    return (
        <Layout title="Nghỉ phép" subtitle="Xem & duyệt đơn nghỉ phép của nhân viên">
            <div className="att-content lp-page">
                <HrHero
                    ico="🏖️"
                    title="Đơn nghỉ phép"
                    kpis={kpis}
                />
                {error && <div className="lp-err">{error}<button onClick={() => setError("")}>×</button></div>}
                {notice && <div className="lp-notice">✓ {notice}</div>}
                {canCancelApproved && <button type="button" className="wf-btn" onClick={() => setLedgerOpen(true)}>Sổ phép · Cấp / điều chỉnh</button>}

                {loading ? (
                    <div className="att-loading">Đang tải...</div>
                ) : (
                    <div className="lp-card">
                        {/* Toolbar: filter pills + search */}
                        <div className="lp-toolbar">
                            <div className="lp-filters">
                                {STATUS_FILTERS.map(([val, label]) => {
                                    const count = val === "" ? kpi.total : rows.filter((r) => String(r.status) === val).length;
                                    return (
                                        <button
                                            key={val}
                                            type="button"
                                            className={`lp-filter${status === val ? " active" : ""}`}
                                            onClick={() => setStatus(val)}
                                        >
                                            <span>{label}</span><i>{count}</i>
                                        </button>
                                    );
                                })}
                            </div>
                            <label className="lp-search">
                                <span aria-hidden="true">⌕</span>
                                <input
                                    type="search"
                                    value={search}
                                    onChange={(e) => setSearch(e.target.value)}
                                    placeholder="Tìm mã, tên, loại phép, lý do..."
                                    aria-label="Tìm đơn nghỉ phép"
                                />
                            </label>
                        </div>

                        {/* Table */}
                        <div className="lp-table-wrap">
                            <ResponsiveTable className="lp-table">
                                <thead>
                                    <tr>
                                        <th>Nhân viên</th>
                                        <th>Loại phép</th>
                                        <th>Thời gian</th>
                                        <th>Số ngày</th>
                                        <th>Lý do</th>
                                        <th>Trạng thái</th>
                                        <th className="lp-th-act">Thao tác</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {filtered.length === 0 ? (
                                        <tr><td colSpan={7} className="lp-empty">Không có đơn nghỉ phép phù hợp.</td></tr>
                                    ) : (
                                        filtered.map((r) => (
                                            <tr key={r.id} className={r.status === 3 ? "lp-row--bad" : r.status === 2 ? "lp-row--ok" : ""}>
                                                <td>
                                                    <div className="lp-user">
                                                        <span className="lp-ava" style={{ background: avaColor(r.employeeCode || r.employeeName) }}>
                                                            {(r.employeeName || "?").slice(0, 1).toUpperCase()}
                                                        </span>
                                                        <div className="lp-user-meta">
                                                            <strong>{r.employeeName || "—"}</strong>
                                                            <small>{r.employeeCode || ""}</small>
                                                        </div>
                                                    </div>
                                                </td>
                                                <td>{r.leaveTypeName || "—"}</td>
                                                <td>
                                                    <div className="lp-date">
                                                        <strong>{fmtDay(r.fromDate, "vi-VN")}</strong>
                                                        <span>→</span>
                                                        <strong>{fmtDay(r.toDate, "vi-VN")}</strong>
                                                    </div>
                                                </td>
                                                <td>{r.totalDays != null ? `${r.totalDays} ngày` : "—"}</td>
                                                <td className="lp-reason">{r.reason || "—"}</td>
                                                <td>
                                                    <span className={`lp-badge lp-badge--${TONES[r.status] || "muted"}`}>{LABELS[r.status] || "—"}</span>
                                                    {r.approverName && <small className="lp-approver">{r.approverName}</small>}
                                                </td>
                                                <td className="lp-th-act">
                                                    <div className="lp-actions">
                                                    {r.status === 1 && r.employeeId !== auth?.employeeId ? (
                                                        <>
                                                            <button
                                                                type="button"
                                                                className="lp-btn lp-btn--ok"
                                                                onClick={() => { setReviewError(""); setReviewAction({ row: r, kind: "approve" }); }}
                                                                disabled={busy === "approve-" + r.id}
                                                            >
                                                                {busy === "approve-" + r.id ? "Đang duyệt…" : "✓ Duyệt"}
                                                            </button>
                                                            <button
                                                                type="button"
                                                                className="lp-btn lp-btn--bad"
                                                                onClick={() => { setReviewError(""); setReviewAction({ row: r, kind: "reject" }); }}
                                                                disabled={busy === "reject-" + r.id}
                                                            >
                                                                {busy === "reject-" + r.id ? "Đang từ chối…" : "Từ chối"}
                                                            </button>
                                                        </>
                                                    ) : null}
                                                    <button type="button" className="lp-btn" onClick={() => pdf(r)}>PDF</button>
                                                    {r.status === 2 && canCancelApproved && r.employeeId !== auth?.employeeId && <button type="button" className="lp-btn lp-btn--bad"
                                                        onClick={() => { setCancelError(""); setCancellation(r); }}>Hủy đơn đã duyệt</button>}
                                                    </div>
                                                    {r.status === 1 && r.employeeId === auth?.employeeId && <small className="lp-self-note">Cần người khác duyệt</small>}
                                                </td>
                                            </tr>
                                        ))
                                    )}
                                </tbody>
                            </ResponsiveTable>
                        </div>
                    </div>
                )}
            </div>
            {cancellation && <ReasonDialog title="Hủy đơn nghỉ đã duyệt" hint="Lý do được lưu cùng lịch sử hủy; phần phép đã trừ sẽ được hoàn một lần."
                maxLength={500} busy={busy === "cancel"} error={cancelError} onClose={() => setCancellation(null)} onSubmit={cancel} />}
            {reviewAction && <ReasonDialog title={reviewAction.kind === "approve" ? "Duyệt đơn nghỉ phép" : "Từ chối đơn nghỉ phép"}
                hint={`${reviewAction.row.employeeName} · ${fmtDay(reviewAction.row.fromDate, "vi-VN")} → ${fmtDay(reviewAction.row.toDate, "vi-VN")} · ${reviewAction.row.totalDays} ngày. Lý do: ${reviewAction.row.reason || "Chưa cung cấp"}`}
                minLength={0} maxLength={500} busy={Boolean(busy)} error={reviewError}
                onClose={() => setReviewAction(null)} onSubmit={reviewLeave} />}
            {ledgerOpen && <LeaveLedgerDialog employeeId={auth?.employeeId} manage onClose={() => setLedgerOpen(false)} onChanged={load} />}
        </Layout>
    );
};

export default EmployeeLeavePage;
