import { useState, useEffect, useMemo } from "react";
import { getAuth } from "../../../services/auth/auth";
import AppLayout from "../../../components/layout/AppLayout";
import relatedApi from "../api/relatedApi";
import "../attendance.css";
import StatKpis from "../components/StatKpis";
import StatCalendar from "../components/StatCalendar";
import StatDetailTable from "../components/StatDetailTable";
import StatWarnings from "../components/StatWarnings";
import AttendancePeriodPanel from "../components/AttendancePeriodPanel";
import { regularHours, businessDate, vnTodayKey } from "../../../utils/vnTime";
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

    const now = businessDate(vnTodayKey());
    const [ym, setYm] = useState({
        y: now.getFullYear(),
        m: now.getMonth(),
    });
    const [history, setHistory] = useState([]);
    const [profile, setProfile] = useState(null);
    const [leaveTypes, setLeaveTypes] = useState([]);
    const [leaves, setLeaves] = useState([]);
    const [holidays, setHolidays] = useState([]);
    const [rule, setRule] = useState(null);
    const [balance, setBalance] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        let active = true;
        const load = async () => {
            setLoading(true);
            setError("");
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
                    relatedApi.myLeaves(),
                    relatedApi.leaveBalance(employeeId, ym.y),
                    relatedApi.attendanceRulesActive(),
                ]);
                const failure = results.find((result) => result.status === "rejected");
                if (failure) throw failure.reason;
                if (!active) return;
                setHistory(list(results[0]));
                setProfile(results[1].value?.data?.data || null);
                setLeaveTypes(pageList(results[2]));
                setHolidays(pageList(results[3]));
                setLeaves(list(results[4]));
                setBalance(results[5].value.data.data);
                setRule(results[6].value.data.data?.[0] || null);
            } catch (err) {
                if (!active) return;
                setError(
                    err.response?.data?.message ||
                        err.message ||
                        "Không thể tải dữ liệu thống kê."
                );
            } finally {
                if (active) setLoading(false);
            }
        };
        load();
        return () => { active = false; };
    }, [userId, employeeId, ym.y]);

    const monthRows = useMemo(
        () =>
            history
                .filter((r) => {
                    const d = businessDate(r.attendanceDate);
                    return d.getFullYear() === ym.y && d.getMonth() === ym.m;
                })
                .sort((a, b) => new Date(a.attendanceDate) - new Date(b.attendanceDate)),
        [history, ym]
    );

    const byDate = useMemo(() => {
        const m = {};
        monthRows.forEach((r) => (m[r.attendanceDate.slice(0, 10)] = r));
        return m;
    }, [monthRows]);

    const holidayMap = useMemo(() => {
        const m = {};
        holidays.forEach((h) => {
            if (h.isActive !== false) m[dkey(businessDate(h.date))] = h.type;
        });
        return m;
    }, [holidays]);

    const stats = useMemo(
        () =>
            computeStats(monthRows, leaveTypes, leaves, holidayMap, ym, { standardHours: rule?.standardHours, leaveBalance: balance }),
        [monthRows, leaveTypes, leaves, holidayMap, ym, rule, balance]
    );

    const dayStatus = (date) =>
        dayStatusOf(date, {
            byDate,
            holidayMap,
            stats,
            todayKey: vnTodayKey(),
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
                const d = businessDate(r.attendanceDate);
                return { name: String(d.getDate()), value: regularHours(r) };
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
                        <StatKpis stats={stats} />
                        <StatWarnings warnings={warnings} />
                        <div className="me-stat-visuals">
                        <ChartCard
                            title="Giờ làm theo ngày"
                            hint="Giờ công từng ngày trong tháng"
                            empty={monthRows.length === 0}
                        >
                            <ColumnChart data={dailyHours} yKey="value" yTickFormatter={(v) => v + "h"} />
                        </ChartCard>
                        <StatCalendar ym={ym} weeks={calendar} />
                        </div>
                        <AttendancePeriodPanel key={`${ym.y}-${ym.m}`} year={ym.y} month={ym.m + 1} />
                        <StatDetailTable monthRows={monthRows} holidayMap={holidayMap} />
                    </div>
                )}
            </div>
        </AppLayout>
    );
};

export default StatisticsPage;
