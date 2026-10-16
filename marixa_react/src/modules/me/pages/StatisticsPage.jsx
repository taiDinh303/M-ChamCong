import { useState, useEffect, useMemo } from "react";
import { getAuth } from "../../../services/auth/auth";
import AppLayout from "../../../components/layout/AppLayout";
import relatedApi from "../api/relatedApi";
import "../attendance.css";
import StatKpis from "../components/StatKpis";
import StatCalendar from "../components/StatCalendar";
import StatDetailTable from "../components/StatDetailTable";
import StatWarnings from "../components/StatWarnings";
import StatSummary from "../components/StatSummary";
import PageBanner from "../components/PageBanner";
import { ChartCard, ColumnChart } from "../../../components/charts/charts";
import "../../../components/charts/charts.css";
import {
    dkey,
    computeStats,
    dayStatusOf,
    buildCalendar,
    buildWarnings,
} from "../components/statUtils";

const StatisticsPage = () => {
    const auth = getAuth();
    const userId = auth?.userId;
    const employeeId = auth?.employeeId;

    const now = new Date();
    const [ym, setYm] = useState({
        y: now.getFullYear(),
        m: now.getMonth(),
    });
    const [history, setHistory] = useState([]);
    const [profile, setProfile] = useState(null);
    const [leaveTypes, setLeaveTypes] = useState([]);
    const [leaves, setLeaves] = useState([]);
    const [holidays, setHolidays] = useState([]);
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
                const list = (r) =>
                    r?.status === "fulfilled"
                        ? r.value.data.data || []
                        : [];
                const pageList = (r) => {
                    const d = r?.status === "fulfilled" ? r.value.data.data : null;
                    return d?.items || d || [];
                };
                const results = await Promise.allSettled([
                    relatedApi.attendanceByEmployee(employeeId),
                    userId ? relatedApi.employeeByUser(userId) : Promise.resolve(null),
                    relatedApi.leaveTypesAll(),
                    relatedApi.holidayCalendarAll(),
                    relatedApi.leavesByEmployee(employeeId),
                ]);
                setHistory(list(results[0]));
                if (results[1]?.status === "fulfilled")
                    setProfile(results[1].value.data.data || null);
                setLeaveTypes(pageList(results[2]));
                setHolidays(pageList(results[3]));
                setLeaves(list(results[4]));
            } catch (err) {
                setError(
                    err.response?.data?.message ||
                        err.message ||
                        "Không thể tải dữ liệu thống kê."
                );
            } finally {
                setLoading(false);
            }
        };
        load();
    }, [userId, employeeId]);

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

    const byDate = useMemo(() => {
        const m = {};
        monthRows.forEach((r) => (m[dkey(new Date(r.attendanceDate))] = r));
        return m;
    }, [monthRows]);

    const holidayMap = useMemo(() => {
        const m = {};
        holidays.forEach((h) => {
            if (h.isActive !== false) m[dkey(new Date(h.date))] = h.type;
        });
        return m;
    }, [holidays]);

    const stats = useMemo(
        () =>
            computeStats(monthRows, leaveTypes, leaves, holidayMap, ym),
        [monthRows, leaveTypes, leaves, holidayMap, ym]
    );

    const dayStatus = (date) =>
        dayStatusOf(date, {
            byDate,
            holidayMap,
            stats,
            todayKey: dkey(new Date()),
        });

    const calendar = useMemo(
        () => buildCalendar(ym, dayStatus),
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [ym, byDate, holidayMap, stats]
    );

    const warnings = useMemo(
        () => buildWarnings(monthRows, dayStatus, ym),
        // eslint-disable-next-line react-hooks/exhaustive-deps
        [monthRows, ym, byDate, holidayMap, stats]
    );

    const dailyHours = useMemo(
        () =>
            monthRows.map((r) => {
                const d = new Date(r.attendanceDate);
                const mins = r.checkInTime && r.checkOutTime
                    ? Math.round((new Date(r.checkOutTime) - new Date(r.checkInTime)) / 60000)
                    : (r.actualHours || 0) * 60;
                return { name: String(d.getDate()), value: Math.round((mins / 60) * 10) / 10 };
            }),
        [monthRows]
    );
    const shiftMonth = (delta) =>
        setYm(({ y, m }) => {
            const d = new Date(y, m + delta, 1);
            return { y: d.getFullYear(), m: d.getMonth() };
        });

    return (
        <AppLayout profile={profile}>
            <div className="att-content">
                {error && <div className="att-error">{error}</div>}
                {loading && <div className="att-loading">Đang tải...</div>}

                {!loading && !error && (
                    <div className="att-stat-wrap">
                        <PageBanner
                            icon="📊"
                            kpis={[
                                {
                                    icon: "⏱",
                                    label: "Giờ làm",
                                    value: `${Math.round(stats.hoursWorked / 60)}h`,
                                    tone: "blue",
                                },
                                {
                                    icon: "📅",
                                    label: "Ngày công",
                                    value: `${stats.daysWorked}/${stats.expectedWorkdays}`,
                                    tone: "green",
                                },
                                {
                                    icon: "⚠",
                                    label: "Đi trễ",
                                    value: stats.late,
                                    tone: "gold",
                                },
                                {
                                    icon: "▲",
                                    label: "Tăng ca",
                                    value: `${Math.round((stats.otWeekday + stats.otWeekend + stats.otHoliday) / 60)}h`,
                                    tone: "red",
                                },
                            ]}
                        />
                        <div className="att-kpi-bar">
                            <button
                                type="button"
                                className="att-kpi-nav"
                                onClick={() => shiftMonth(-1)}
                                aria-label="Tháng trước"
                            >
                                &lsaquo;
                            </button>
                            <strong className="att-stat-month">
                                {new Date(ym.y, ym.m, 1)
                                    .toLocaleDateString("vi-VN", {
                                        month: "long",
                                        year: "numeric",
                                    })
                                    .toUpperCase()}
                            </strong>
                            <button
                                type="button"
                                className="att-kpi-nav"
                                onClick={() => shiftMonth(1)}
                                aria-label="Tháng sau"
                            >
                                &rsaquo;
                            </button>
                        </div>
                        <div className="att-stat-overview">
                            <StatWarnings warnings={warnings} />
                            <StatSummary stats={stats} />
                            <StatKpis ym={ym} stats={stats} />
                        </div>
                        <ChartCard
                            title="Giờ làm theo ngày"
                            hint="Giờ công từng ngày trong tháng"
                            empty={monthRows.length === 0}
                        >
                            <ColumnChart data={dailyHours} yKey="value" yTickFormatter={(v) => v + "h"} />
                        </ChartCard>
                        <StatCalendar ym={ym} weeks={calendar} />
                        <StatDetailTable monthRows={monthRows} holidayMap={holidayMap} />
                    </div>
                )}
            </div>
        </AppLayout>
    );
};

export default StatisticsPage;
