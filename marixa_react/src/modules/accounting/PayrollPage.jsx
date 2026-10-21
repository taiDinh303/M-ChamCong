import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import AccountingLayout from "./AccountingLayout";
import { getAllPages } from "../../services/api/lists";
import { useAdminList } from "../admin/hooks/useAdminList";
import MetricCard from "../../components/common/MetricCard";
import ResponsiveTable from "../../components/common/ResponsiveTable";
import useModalDialog from "../../components/common/useModalDialog";
import { responseCache } from "../../services/api/responseCache.js";
import { toCsv, downloadCsv } from "../hr/hrUtils";
import { money, payrollMonth, filterPayrolls, payrollStatusLabels, payrollStatusTones } from "./payrollView.js";

const fetchPayrolls = () => getAllPages("/Payroll/get-all");
const monthLabel = (value) => /^\d{4}-\d{2}$/.test(value) ? `${value.slice(5)}/${value.slice(0, 4)}` : "—";
const amounts = [["basicSalary", "Lương cơ bản"], ["allowance", "Phụ cấp"], ["bonus", "Thưởng"], ["overtime", "Tiền tăng ca"], ["insurance", "Bảo hiểm"], ["tax", "Thuế"], ["deduction", "Khấu trừ"], ["netSalary", "Thực nhận"]];

function PayrollDetail({ row, onClose }) {
    const ref = useModalDialog();
    return <dialog ref={ref} className="accounting-dialog" aria-labelledby="payroll-detail-title" onCancel={onClose}>
        <header><h2 id="payroll-detail-title">Phiếu lương · {row.employeeName || "Chưa có tên"}</h2><button type="button" onClick={onClose} aria-label="Đóng chi tiết phiếu lương">×</button></header>
        <p>Kỳ {monthLabel(payrollMonth(row.payrollMonth))} · {payrollStatusLabels[row.status] || "Chưa rõ trạng thái"}</p>
        <dl>{amounts.map(([key, label]) => <div key={key}><dt>{label}</dt><dd>{money(row[key])}</dd></div>)}</dl>
        <p>Ngày trả: {row.payDate ? row.payDate.slice(0, 10).split("-").reverse().join("/") : "Chưa ghi nhận"}</p>
        <p className="att-muted">Các khoản trên là số liệu đã lưu trong phiếu, chưa phải kết quả tính lại từ bảng công.</p>
        <button type="button" className="admin-link-btn" onClick={onClose}>Đóng</button>
    </dialog>;
}

export default function PayrollPage({ overview = false }) {
    const { rows, loading, error, reload } = useAdminList(fetchPayrolls);
    const [filters, setFilters] = useState({ month: "", status: "", search: "" });
    const [selected, setSelected] = useState(null);
    const [page, setPage] = useState(1);
    const filtered = useMemo(() => filterPayrolls(rows, filters), [rows, filters]);
    const pageCount = Math.max(1, Math.ceil(filtered.length / 20));
    const safePage = Math.min(page, pageCount);
    const setFilter = (key, value) => { setFilters((old) => ({ ...old, [key]: value })); setPage(1); setSelected(null); };
    const totals = error || loading ? null : rows;
    const refresh = () => {
        setSelected(null);
        responseCache.invalidateWhere((uri) => uri.split("?")[0].endsWith("/Payroll/get-all"));
        reload();
    };
    const exportCsv = () => downloadCsv("bang-luong.csv", toCsv(filtered.map((row) => ({
        "Nhân viên": row.employeeName, "Kỳ lương": payrollMonth(row.payrollMonth),
        "Lương cơ bản": row.basicSalary, "Thực nhận": row.netSalary,
        "Trạng thái": payrollStatusLabels[row.status] || "Chưa rõ",
    })), ["Nhân viên", "Kỳ lương", "Lương cơ bản", "Thực nhận", "Trạng thái"]));
    return <AccountingLayout><div className="att-content accounting-page">
        {loading && <p role="status">Đang tải bảng lương…</p>}
        {error && <div role="alert" className="att-error">{error}<button type="button" className="admin-link-btn" onClick={refresh}>Thử lại</button></div>}
        <div className="accounting-metrics">
            <MetricCard icon="▤" label="Phiếu lương đã lưu" value={totals ? totals.length : "—"} tone="blue" />
            <MetricCard icon="◷" label="Phiếu nháp" value={totals ? totals.filter((row) => row.status === 1).length : "—"} tone="gold" />
            <MetricCard icon="✓" label="Đã duyệt" value={totals ? totals.filter((row) => row.status === 3).length : "—"} tone="green" />
            <MetricCard icon="▣" label="Đã trả" value={totals ? totals.filter((row) => row.status === 4).length : "—"} tone="teal" />
        </div>
        {overview ? <section className="att-card accounting-intro"><h2>Quản lý bảng lương</h2><p>Tra cứu kỳ lương, trạng thái và chi tiết các khoản đã lưu của từng nhân viên.</p><Link className="admin-link-btn" to="/accounting/payroll">Mở bảng lương</Link><p className="att-muted">Phần này hiện phục vụ tra cứu. Chưa có thao tác tự tính, duyệt hoặc chi lương.</p></section> : <section className="att-card accounting-register">
            <div className="accounting-filters">
                <label>Tìm nhân viên<input type="search" value={filters.search} onChange={(e) => setFilter("search", e.target.value)} /></label>
                <label>Kỳ lương<input type="month" value={filters.month} onChange={(e) => setFilter("month", e.target.value)} /></label>
                <label>Trạng thái<select value={filters.status} onChange={(e) => setFilter("status", e.target.value)}><option value="">Tất cả</option>{Object.entries(payrollStatusLabels).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
                <button type="button" className="admin-link-btn" onClick={() => { setFilters({ month: "", status: "", search: "" }); setPage(1); }}>Bỏ lọc</button>
                <button type="button" className="admin-link-btn" disabled={loading} onClick={refresh}>Tải lại</button>
                <button type="button" className="admin-link-btn" disabled={loading || Boolean(error) || !filtered.length} onClick={exportCsv}>Xuất CSV</button>
            </div>
            {!loading && !error && <><p>{filtered.length} phiếu khớp bộ lọc</p><ResponsiveTable className="mx-table"><thead><tr><th>Nhân viên</th><th>Kỳ lương</th><th>Lương cơ bản</th><th>Thực nhận</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>
                {filtered.slice((safePage - 1) * 20, safePage * 20).map((row) => <tr key={row.id}><td>{row.employeeName || "—"}</td><td>{monthLabel(payrollMonth(row.payrollMonth))}</td><td>{money(row.basicSalary)}</td><td>{money(row.netSalary)}</td><td><span className={`att-badge ${payrollStatusTones[row.status] || "muted"}`}>{payrollStatusLabels[row.status] || "Chưa rõ"}</span></td><td><button className="admin-link-btn" type="button" onClick={() => setSelected(row)}>Mở phiếu lương</button></td></tr>)}
                {filtered.length === 0 && <tr><td colSpan={6}>{rows.length ? "Không có phiếu khớp bộ lọc." : "Chưa có phiếu lương đã lưu."}</td></tr>}
            </tbody></ResponsiveTable><div className="accounting-pagination"><button type="button" disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>Trang trước</button><span>Trang {safePage}/{pageCount}</span><button type="button" disabled={safePage >= pageCount} onClick={() => setPage(safePage + 1)}>Trang sau</button></div></>}
        </section>}
        {selected && !error && <PayrollDetail row={selected} onClose={() => setSelected(null)} />}
    </div></AccountingLayout>;
}
