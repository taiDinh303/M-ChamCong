import { useState, useEffect, useMemo, useCallback } from "react";
import AdminAppLayout from "../../layout/AdminAppLayout";
import HrAppLayout from "../../../hr/layout/HrAppLayout";
import adminApi from "../../api/adminApi";
import "../../../../modules/me/attendance.css";
import "../../admin.css";
import "../account-issuance.css";
import { translate, useLanguage } from "../../../../services/i18n/LanguageProvider";

const workingStatus = [1, 2, 3];
const activationCodeValidityMinutes = 1440;
const employeeStatus = { 1: "Đang thử việc", 2: "Đang làm", 3: "Tạm nghỉ" };
const date = (value) => value ? new Date(value).toLocaleDateString("vi-VN") : "—";
const isCodeActive = (code) => !code.isUsed && new Date(code.expiresAt) > new Date();
const initials = (employee) => `${employee.givenName?.[0] || ""}${employee.familyName?.[0] || ""}`.toLocaleUpperCase("vi");
const AVA_HUES = ["#2f6df6", "#16a085", "#e67e22", "#8e44ad", "#c0392b", "#1565c0", "#00838f", "#d8436b"];
const avaColor = (seed = "") => {
    let h = 0;
    for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) | 0;
    return AVA_HUES[Math.abs(h) % AVA_HUES.length];
};
// eslint-disable-next-line no-control-regex
const xmlEscape = (value) => String(value ?? "").replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F]/g, "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&apos;");

const downloadUnlinkedEmployees = (employees) => {
    const headers = ["Mã nhân viên", "Họ và tên", "Email", "Số điện thoại", "Phòng ban", "Chức vụ", "Người quản lý", "Ngày vào làm", "Trạng thái"];
    const rows = employees.map((e) => [e.employeeCode, e.fullName, e.email, e.phoneNumber, e.departmentName, e.positionName, e.managerName, date(e.startDate), employeeStatus[e.status] || "Khác"]);
    const row = (cells) => `<Row>${cells.map((value) => `<Cell><Data ss:Type="String">${xmlEscape(value)}</Data></Cell>`).join("")}</Row>`;
    const workbook = `<?xml version="1.0" encoding="UTF-8"?><?mso-application progid="Excel.Sheet"?><Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet"><Worksheet ss:Name="Chưa có tài khoản"><Table>${row(headers)}${rows.map(row).join("")}</Table></Worksheet></Workbook>`;
    const blob = new Blob(["\uFEFF", workbook], { type: "application/vnd.ms-excel;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `nhan-vien-chua-co-tai-khoan-${new Date().toISOString().slice(0, 10)}.xls`;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
};

// Bộ lọc nhanh
const FILTERS = {
    all:   (s) => true,
    ready: (s) => s.key === "none",
    has:   (s) => s.key === "has" || s.key === "activated",
    none:  (s) => s.key === "none",
    issue: (s) => s.key === "invalid" || s.key === "dup",
    expired: (s) => s.key === "expired",
};

// Ưu tiên sắp xếp: "Chưa có" lên đầu, rồi lỗi dữ liệu, hết hạn, cuối cùng đã có
const PRIORITY = { none: 0, invalid: 1, dup: 1, expired: 2, has: 3, activated: 3 };

const AccountIssuancePage = ({ layout = "admin", api = adminApi }) => {
    const [employees, setEmployees] = useState([]);
    const [users, setUsers] = useState([]);
    const [codes, setCodes] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [search, setSearch] = useState("");
    const [selected, setSelected] = useState(new Set());
    const [issued, setIssued] = useState("");
    const [filter, setFilter] = useState("all");
    const [busy, setBusy] = useState(false);
    const [confirming, setConfirming] = useState(null);
    const [confirmBusy, setConfirmBusy] = useState(false);
    const [, refreshExpiry] = useState(0);

    const { language } = useLanguage();
    const L = useCallback((text) => translate(text, language), [language]);

    const load = useCallback(async () => {
        setLoading(true);
        try {
            const [e, u, c] = await Promise.all([api.employees(), api.users(), api.activationCodes()]);
            setEmployees(e.data.data?.items || []);
            setUsers(u.data.data?.items || []);
            setCodes(c.data.data?.items || []);
        } catch (err) {
            setError(err.response?.data?.message || err.message || L("Không tải được danh sách tài khoản."));
        } finally { setLoading(false); }
    }, [api, L]);

    useEffect(() => { load(); }, [load]);
    useEffect(() => {
        const timer = setInterval(() => refreshExpiry(Date.now()), 30_000);
        return () => clearInterval(timer);
    }, []);

    const userIds = useMemo(() => new Set(users.map((user) => user.employeeId).filter(Boolean)), [users]);
    const isLinked = useCallback((employee) => Boolean(employee.userId || userIds.has(employee.id)), [userIds]);
    const activeCodeFor = useCallback((employeeId) => codes.find((code) => code.employeeId === employeeId && isCodeActive(code)), [codes]);
    const rowState = useCallback((employee) => {
        const hasAccount = isLinked(employee);
        const email = (employee.email || "").trim();
        const invalidEmail = email !== "" && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
        const duplicateCode = employees.some((other) => other.employeeCode === employee.employeeCode && other.id !== employee.id);
        const latestCode = codes
            .filter((code) => code.employeeId === employee.id)
            .sort((a, b) => new Date(b.createdTime) - new Date(a.createdTime))[0];

        if (duplicateCode) return { key: "dup", label: "Trùng mã", cls: "bad" };
        if (invalidEmail)  return { key: "invalid", label: "Email không hợp lệ", cls: "bad" };
        if (latestCode && isCodeActive(latestCode)) {
            if (hasAccount && latestCode.createdBy !== "Reissue" && !codes.some((c) => c.employeeId === employee.id && c.isUsed))
                return { key: "has", label: "Đã có tài khoản", cls: "ok" };
            return { key: "none", label: "Chưa có", cls: "warn" };
        }
        if (latestCode?.isUsed) return { key: "activated", label: "Đã kích hoạt", cls: "ok" };
        if (latestCode && hasAccount) return { key: "expired", label: "Mã hết hạn", cls: "warn" };
        if (hasAccount) return { key: "has", label: "Đã có tài khoản", cls: "ok" };
        return { key: "none", label: "Chưa có", cls: "warn" };
    }, [employees, isLinked, codes]);

    const working = useMemo(() => employees.filter((employee) => workingStatus.includes(employee.status)), [employees]);

    // Lọc + sort: "Chưa có" luôn ở trên cùng; thứ tự: none → invalid/dup → expired → has/activated
    const filtered = useMemo(() => {
        const query = search.trim().toLocaleLowerCase("vi");
        const base = working.filter((employee) =>
            FILTERS[filter](rowState(employee)) &&
            `${employee.employeeCode} ${employee.fullName} ${employee.email || ""}`
                .toLocaleLowerCase("vi")
                .includes(query)
        );
        return base.sort((a, b) => {
            const pa = PRIORITY[rowState(a).key] ?? 9;
            const pb = PRIORITY[rowState(b).key] ?? 9;
            if (pa !== pb) return pa - pb;
            return (a.employeeCode || "").localeCompare(b.employeeCode || "");
        });
    }, [working, filter, search, rowState]);

    const unlinked = employees.filter((employee) => !isLinked(employee));

    const kpis = useMemo(() => ({
        total: employees.length,
        working: working.length,
        has: working.filter((employee) => isLinked(employee) && rowState(employee).key !== "none").length,
        none: working.filter((employee) => rowState(employee).key === "none" || !isLinked(employee)).length,
        invalid: working.filter((employee) => rowState(employee).key === "invalid").length,
        dup: working.filter((employee) => rowState(employee).key === "dup").length,
        ready: working.filter((employee) => rowState(employee).key === "none").length,
        choosing: selected.size,
    }), [employees.length, working, selected, isLinked, rowState]);

    const toggle = (id) => setSelected((previous) => {
        const next = new Set(previous);
        next.has(id) ? next.delete(id) : next.add(id);
        return next;
    });

    const generateCode = () => `ACT-${Math.random().toString(36).slice(2, 8).toUpperCase()}`;

    const issueCodes = async () => {
        const ids = [...selected].filter((id) => {
            const employee = working.find((item) => item.id === id);
            return employee && rowState(employee).key === "none";
        });
        if (!ids.length) { setError(L("Chọn nhân viên hợp lệ chưa có tài khoản.")); return; }
        setBusy(true); setError("");
        let success = 0;
        for (const id of ids) {
            try {
                await api.createActivationCode({ employeeId: id, code: generateCode(), validMinutes: activationCodeValidityMinutes });
                success++;
            } catch { /* report total below */ }
        }
        setIssued(`${L("Đã cấp")} ${success}/${ids.length} ${L("mã kích hoạt (hạn 1 ngày).")}`);
        setSelected(new Set());
        setBusy(false);
        await load();
    };

    const askReissue = (employee, fresh = false) => {
        setConfirmBusy(false);
        setConfirming({ employee, fresh });
    };

    const doReissue = async () => {
        if (!confirming) return;
        const { employee, fresh } = confirming;
        setConfirmBusy(true);
        try {
            await api.createActivationCode({
                employeeId: employee.id,
                code: generateCode(),
                validMinutes: activationCodeValidityMinutes,
            });
            setIssued(
                `${L(fresh ? "Đã cấp mã kích hoạt cho" : "Đã cấp lại mã kích hoạt cho")} ${employee.fullName}. ` +
                L("Bàn giao mã cho nhân viên để kích hoạt / đặt lại mật khẩu.")
            );
            setConfirming(null);
            setConfirmBusy(false);
            await load();
        } catch (err) {
            setConfirmBusy(false);
            setConfirming(null);
            setError(err.response?.data?.message || L("Không cấp được mã kích hoạt."));
        }
    };

    const PageLayout = layout === "hr" ? HrAppLayout : AdminAppLayout;

    const heroKpis = [
        { label: L("Đang làm việc"), value: kpis.working, tone: "blue",  ico: "👥" },
        { label: L("Đã có tài khoản"), value: kpis.has, tone: "green",  ico: "✅" },
        { label: L("Chưa có TK"), value: kpis.none, tone: "amber", ico: "🔑" },
        { label: L("Sẵn sàng cấp"), value: kpis.ready, tone: "teal", ico: "⚡" },
    ];

    const filterPills = [
        ["all",     L("Tất cả"), kpis.working],
        ["ready",   L("Sẵn sàng cấp"), kpis.ready],
        ["has",     L("Đã có TK"), kpis.has],
        ["issue",   L("Cần xử lý"), kpis.invalid + kpis.dup],
        ["expired", L("Mã hết hạn"), working.filter((e) => rowState(e).key === "expired").length],
    ];

    return <PageLayout
        title={L("Cấp tài khoản")}
        subtitle={L("Cấp mã kích hoạt và theo dõi tình trạng tài khoản nhân viên")}
    >
        <div className="att-content account-issuance">
            {error && <div className="att-error ai-err"><span>{error}</span><button onClick={() => setError("")}>×</button></div>}
            {issued && <div className="att-notice ai-notice">✓ {issued}</div>}
            {loading && <div className="att-loading">{L("Đang tải...")}</div>}

            {!loading && <>
                {/* ===== Hero ===== */}
                <div className="ai-hero">
                    <div className="ai-hero-head">
                        <span className="ai-hero-ico">🔐</span>
                        <div>
                            <h2>{L("Cấp & quản lý tài khoản")}</h2>
                            <p>{L("Cấp mã kích hoạt và theo dõi tình trạng tài khoản nhân viên. Cấp mã đã tạo tài khoản, nhân viên tự kích hoạt là vào được.")}</p>
                        </div>
                    </div>
                    <div className="ai-hero-kpis">
                        {heroKpis.map((k) => (
                            <div key={k.label} className={`ai-kpi ai-kpi--${k.tone}`}>
                                <span className="ai-kpi-ico">{k.ico}</span>
                                <div>
                                    <span className="ai-kpi-label">{k.label}</span>
                                    <strong>{k.value}</strong>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* ===== Bảng ===== */}
                <div className="ai-card">
                    <div className="ai-toolbar">
                        <div className="ai-filters">
                            {filterPills.map(([key, label, count]) => (
                                <button
                                    key={key}
                                    type="button"
                                    className={`ai-filter${filter === key ? " active" : ""}`}
                                    onClick={() => setFilter(key)}
                                >
                                    <span>{label}</span><i>{count}</i>
                                </button>
                            ))}
                        </div>
                        <div className="ai-actions">
                            <label className="ai-search">
                                <span aria-hidden="true">⌕</span>
                                <input
                                    type="search"
                                    value={search}
                                    onChange={(event) => setSearch(event.target.value)}
                                    placeholder={L("Tìm mã, họ tên, email...")}
                                    aria-label={L("Tìm nhân viên")}
                                />
                            </label>
                            <button type="button" className="ai-btn" onClick={() => downloadUnlinkedEmployees(unlinked)}>
                                ⬇ {L("Xuất Excel")} <i>{unlinked.length}</i>
                            </button>
                            <button
                                type="button"
                                className="ai-btn ai-btn--primary"
                                onClick={issueCodes}
                                disabled={busy || !selected.size}
                            >
                                ＋ {L("Cấp mã")} <i>{selected.size}</i>
                            </button>
                        </div>
                    </div>

                    {/* Batch bar */}
                    {selected.size > 0 && (
                        <div className="ai-batchbar">
                            <span>Đã chọn <strong>{selected.size}</strong> nhân viên sẵn sàng cấp mã (hạn 1 ngày)</span>
                            <div>
                                <button type="button" className="ai-batchbtn" onClick={() => setSelected(new Set())}>
                                    {L("Bỏ chọn")}
                                </button>
                                <button
                                    type="button"
                                    className="ai-batchbtn ai-batchbtn--go"
                                    onClick={issueCodes}
                                    disabled={busy}
                                >
                                    {busy ? L("Đang cấp…") : `＋ ${L("Cấp mã ngay")}`}
                                </button>
                            </div>
                        </div>
                    )}

                    <div className="ai-table-wrap">
                        <table className="ai-table">
                            <thead>
                                <tr>
                                    <th className="ai-th-check">
                                        <input
                                            type="checkbox"
                                            aria-label={L("Chọn tất cả")}
                                            checked={
                                                filtered.length > 0 &&
                                                filtered.every((e) => rowState(e).key === "none" && selected.has(e.id))
                                            }
                                            onChange={(e) => {
                                                const readyIds = filtered
                                                    .filter((x) => rowState(x).key === "none")
                                                    .map((x) => x.id);
                                                setSelected(e.target.checked ? new Set(readyIds) : new Set());
                                            }}
                                        />
                                    </th>
                                    <th>{L("Nhân viên")}</th>
                                    <th>{L("Email")}</th>
                                    <th>{L("Trạng thái")}</th>
                                    <th className="ai-th-code">{L("Mã kích hoạt")}</th>
                                    <th className="ai-th-act">{L("Thao tác")}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filtered.length === 0 ? (
                                    <tr>
                                        <td colSpan={6} className="ai-empty">
                                            {search
                                                ? L("Không tìm thấy nhân viên phù hợp.")
                                                : L("Không có nhân viên đang làm.")}
                                        </td>
                                    </tr>
                                ) : filtered.map((employee) => {
                                    const state = rowState(employee);
                                    const code = activeCodeFor(employee.id);
                                    const canIssue = state.key === "none";
                                    return (
                                        <tr
                                            key={employee.id}
                                            className={
                                                state.cls === "bad" ? "ai-row--bad" :
                                                state.cls === "ok" ? "ai-row--ok" :
                                                ""
                                            }
                                        >
                                            <td className="ai-th-check">
                                                <input
                                                    type="checkbox"
                                                    className="ai-check"
                                                    checked={selected.has(employee.id)}
                                                    disabled={!canIssue}
                                                    onChange={() => toggle(employee.id)}
                                                    aria-label={L("Chọn")}
                                                />
                                            </td>
                                            <td>
                                                <div className="ai-user">
                                                    <span
                                                        className="ai-ava"
                                                        style={{ background: avaColor(employee.employeeCode || employee.fullName) }}
                                                    >
                                                        {initials(employee) || "?"}
                                                    </span>
                                                    <div className="ai-user-meta">
                                                        <strong>{employee.fullName}</strong>
                                                        <small>
                                                            {employee.employeeCode}
                                                            {employee.departmentName ? ` · ${employee.departmentName}` : ""}
                                                        </small>
                                                    </div>
                                                </div>
                                            </td>
                                            <td className="ai-email">{employee.email || "—"}</td>
                                            <td>
                                                <span className={`att-badge ${state.cls}`}>{L(state.label)}</span>
                                            </td>
                                            <td className="ai-th-code">
                                                {code
                                                    ? <code className="ai-code">{code.code}</code>
                                                    : <span className="ai-muted">—</span>}
                                            </td>
                                            <td className="ai-th-act">
                                                <div className="ai-act">
                                                    {code ? (
                                                        <span
                                                            className="ai-pending-badge"
                                                            title={L("Mã đã cấp, nhân viên sẽ tự kích hoạt tại trang Kích hoạt")}
                                                        >
                                                            ⏳ {L("Chờ NV kích hoạt")}
                                                        </span>
                                                    ) : canIssue ? (
                                                        <button
                                                            type="button"
                                                            className="ai-issue-one-btn"
                                                            onClick={() => askReissue(employee, true)}
                                                            title={L("Cấp mã kích hoạt cho nhân viên chưa có tài khoản")}
                                                        >
                                                            {L("Cấp mã")}
                                                        </button>
                                                    ) : (
                                                        <button
                                                            type="button"
                                                            className="ai-reissue-btn"
                                                            onClick={() => askReissue(employee, false)}
                                                            title={L("Cấp lại mã để nhân viên đặt lại mật khẩu")}
                                                        >
                                                            {L("Cấp lại mã")}
                                                        </button>
                                                    )}
                                                </div>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                </div>
            </>}

            {/* ===== Modal xác nhận cấp / cấp lại mã ===== */}
            {confirming && (
                <div
                    className="ai-overlay"
                    onMouseDown={(e) => e.target === e.currentTarget && !confirmBusy && setConfirming(null)}
                >
                    <div className="ai-modal ai-confirm-modal" role="dialog" aria-modal="true">
                        <header>
                            <div className="ai-modal-title">
                                <span className="ai-modal-ico">{confirming.fresh ? "🔑" : "🔁"}</span>
                                <div>
                                    <h2>{L(confirming.fresh ? "Cấp mã kích hoạt" : "Cấp lại mã kích hoạt")}</h2>
                                    <p>{confirming.employee.fullName} · {confirming.employee.employeeCode}</p>
                                </div>
                            </div>
                            <button type="button" onClick={() => !confirmBusy && setConfirming(null)} aria-label={L("Đóng")}>×</button>
                        </header>
                        <div className="ai-confirm-body">
                            <p>{L("Thao tác sẽ tạo tài khoản (nếu nhân viên chưa có) và vô hiệu các mã cũ chưa dùng. Hạn mã 1 ngày.")}</p>
                            <p className="ai-confirm-hint">{L("Bàn giao mã cho nhân viên để kích hoạt / đặt mật khẩu tại trang Kích hoạt.")}</p>
                        </div>
                        <footer>
                            <button type="button" onClick={() => setConfirming(null)} disabled={confirmBusy}>
                                {L("Hủy")}
                            </button>
                            <button
                                type="button"
                                className="ai-confirm-btn"
                                onClick={doReissue}
                                disabled={confirmBusy}
                            >
                                {confirmBusy ? L("Đang cấp…") : L("Xác nhận")}
                            </button>
                        </footer>
                    </div>
                </div>
            )}
        </div>
    </PageLayout>;
};

export default AccountIssuancePage;
