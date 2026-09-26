import { useState, useEffect, useMemo } from "react";
import { Link } from "react-router-dom";
import { getAuth } from "../../../services/auth/auth";
import relatedApi from "../../attendance/api/relatedApi";
import "../../attendance/attendance.css";

const WEEKDAYS = [
    "Chủ nhật",
    "Thứ 2",
    "Thứ 3",
    "Thứ 4",
    "Thứ 5",
    "Thứ 6",
    "Thứ 7",
];

const statusLabel = (s) =>
    ({
        1: "Đủ mặt",
        2: "Đi trễ",
        3: "Về sớm",
        4: "Vắng mặt",
        5: "Nghỉ phép",
        6: "Lễ",
        7: "Ngoại tuần",
    })[s] || "Chưa đánh giá";

const statusClass = (s) =>
    ({ 1: "ok", 2: "warn", 3: "warn", 4: "bad", 5: "info", 6: "info", 7: "info" })[
        s
    ] || "";

const approvalLabel = (s) =>
    ({ 0: "Chờ duyệt", 1: "Đã duyệt", 2: "Từ chối" })[s] ?? "Chờ duyệt";

const approvalClass = (s) => ({ 0: "warn", 1: "ok", 2: "bad" })[s] || "";

const StatisticsPage = () => {
    const auth = getAuth();
    const employeeId = auth?.employeeId;

    const today = new Date();
    const [ym, setYm] = useState({ y: today.getFullYear(), m: today.getMonth() });
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        const load = async () => {
            try {
                if (!employeeId) {
                    setError("Chưa xác định được nhân viên đang đăng nhập.");
                    setLoading(false);
                    return;
                }
                const res = await relatedApi.attendanceByEmployee(employeeId);
                setHistory(res.data.data || []);
            } catch (err) {
                setError(
                    err.response?.data?.message ||
                        err.message ||
                        "Không thể tải dữ liệu."
                );
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [employeeId]);

    const monthRows = useMemo(
        () =>
            history
                .filter((r) => {
                    const d = new Date(r.attendanceDate);
                    return d.getFullYear() === ym.y && d.getMonth() === ym.m;
                })
                .sort((a, b) => new Date(a.attendanceDate) - new Date(b.attendanceDate)),
        [history, ym]
    );

    const kpi = useMemo(
        () => ({
            total: monthRows.length,
            worked: monthRows.filter((r) => [1, 2, 3].includes(r.status)).length,
            late: monthRows.filter((r) => r.status === 2).length,
            absent: monthRows.filter((r) => r.status === 4).length,
            leave: monthRows.filter((r) => r.status === 5).length,
            hours: monthRows.reduce((sum, r) => sum + (r.actualHours || 0), 0),
        }),
        [monthRows]
    );

    const shiftMonth = (delta) =>
        setYm(({ y, m }) => {
            const d = new Date(y, m + delta, 1);
            return { y: d.getFullYear(), m: d.getMonth() };
        });

    const monthLabel = new Date(ym.y, ym.m, 1).toLocaleDateString(
        "vi-VN",
        { month: "long", year: "numeric" }
    );

    return (
        <div className="att-page">
            <header className="att-header">
                <div>
                    <h1>Thống kê công</h1>
                    <p>Theo dõi số ngày công làm việc theo tháng</p>
                </div>
                <Link to="/attendance" className="att-logout">
                    &larr; Chấm công
                </Link>
            </header>

            <div className="att-content">
                {error && <div className="att-error">{error}</div>}
                {loading && <div className="att-loading">Đang tải...</div>}

                {!loading && !error && (
                    <>
                        <div className="att-kpi-bar">
                            <button
                                type="button"
                                className="att-kpi-nav"
                                onClick={() => shiftMonth(-1)}
                                aria-label="Tháng trước"
                            >
                                &lsaquo;
                            </button>
                            <strong>{monthLabel}</strong>
                            <button
                                type="button"
                                className="att-kpi-nav"
                                onClick={() => shiftMonth(1)}
                                aria-label="Tháng sau"
                            >
                                &rsaquo;
                            </button>
                        </div>

                        <div className="att-kpi-grid">
                            <div className="att-card att-kpi">
                                <span className="att-kpi-label">Số ngày đã làm</span>
                                <strong>{kpi.worked}</strong>
                                <span className="att-muted">
                                    / {kpi.total} bản ghi trong tháng
                                </span>
                            </div>
                            <div className="att-card att-kpi">
                                <span className="att-kpi-label">Đi trễ</span>
                                <strong>{kpi.late}</strong>
                                <span className="att-muted">lần trong tháng</span>
                            </div>
                            <div className="att-card att-kpi">
                                <span className="att-kpi-label">Vắng mặt</span>
                                <strong>{kpi.absent}</strong>
                                <span className="att-muted">lần trong tháng</span>
                            </div>
                            <div className="att-card att-kpi">
                                <span className="att-kpi-label">Tổng giờ thực tế</span>
                                <strong>{kpi.hours}h</strong>
                                <span className="att-muted">
                                    {kpi.leave} ngày nghỉ phép
                                </span>
                            </div>
                        </div>

                        <div className="att-card">
                            <h2>Chi tiết ngày công trong tháng</h2>
                            {monthRows.length === 0 ? (
                                <p className="att-muted">
                                    Chưa có bản ghi chấm công trong tháng này.
                                </p>
                            ) : (
                                <div className="att-table-wrap">
                                    <table className="att-table">
                                        <thead>
                                            <tr>
                                                <th>Ngày</th>
                                                <th>Ca</th>
                                                <th>Trạng thái</th>
                                                <th>Giờ thực tế</th>
                                                <th>Duyệt</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {monthRows.map((row) => {
                                                const d = new Date(
                                                    row.attendanceDate
                                                );
                                                return (
                                                    <tr key={row.id}>
                                                        <td>
                                                            {d.toLocaleDateString(
                                                                "vi-VN"
                                                            )}{" "}
                                                            <span className="att-muted">
                                                                {WEEKDAYS[
                                                                    d.getDay()
                                                                ]}
                                                            </span>
                                                        </td>
                                                        <td>
                                                            {row.plannedShiftName ||
                                                                "—"}
                                                        </td>
                                                        <td>
                                                            <span
                                                                className={`att-badge ${statusClass(
                                                                    row.status
                                                                )}`}
                                                            >
                                                                {statusLabel(
                                                                    row.status
                                                                )}
                                                            </span>
                                                        </td>
                                                        <td>
                                                            {row.actualHours !=
                                                            null
                                                                ? `${row.actualHours}h`
                                                                : "—"}
                                                        </td>
                                                        <td>
                                                            <span
                                                                className={`att-badge ${approvalClass(
                                                                    row.approvalStatus
                                                                )}`}
                                                            >
                                                                {approvalLabel(
                                                                    row.approvalStatus
                                                                )}
                                                            </span>
                                                        </td>
                                                    </tr>
                                                );
                                            })}
                                        </tbody>
                                    </table>
                                </div>
                            )}
                        </div>
                    </>
                )}
            </div>
        </div>
    );
};

export default StatisticsPage;
