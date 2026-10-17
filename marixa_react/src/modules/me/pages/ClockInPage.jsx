import { useState, useEffect, useCallback } from "react";
import { getAuth } from "../../../services/auth/auth";
import AppLayout from "../../../components/layout/AppLayout";
import AttendanceCard from "../components/AttendanceCard";
import relatedApi from "../api/relatedApi";
import { vnTodayKey } from "../../../utils/vnTime";
import "../attendance.css";

const AttendancePage = () => {
    const auth = getAuth();
    const userId = auth?.userId;
    const employeeId = auth?.employeeId;

    const [profile, setProfile] = useState(null);
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [rule, setRule] = useState(null);

    // Bản ghi chấm công của HÔM NAY
    const todayRecord =
        history.find(
            (r) =>
                r.attendanceDate &&
                r.attendanceDate.slice(0, 10) === vnTodayKey()
        ) || null;

    const loadAttendance = useCallback(async () => {
        if (!employeeId) return [];
        const res = await relatedApi.attendanceByEmployee(employeeId);
        const data = res.data.data || [];
        setHistory(data);
        return data;
    }, [employeeId]);

    useEffect(() => {
        const load = async () => {
            if (!userId) {
                setError("Chưa xác định được tài khoản đang đăng nhập.");
                setLoading(false);
                return;
            }

            try {
                const [profileRes, rulesRes] = await Promise.all([
                    relatedApi.employeeByUser(userId), relatedApi.attendanceRulesActive()
                ]);
                setProfile(profileRes.data.data || null);
                setRule(rulesRes.data.data?.[0] || null);

                if (employeeId) {
                    await loadAttendance();
                }
            } catch (err) {
                setError(
                    err.response?.data?.message ||
                        err.message ||
                        "Không thể tải dữ liệu chấm công."
                );
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [userId, employeeId, loadAttendance]);

    // Sau khi chấm công: làm mới lại bản ghi hôm nay
    const handleChecked = () => {
        loadAttendance().catch(() => {});
    };

    return (
        <AppLayout profile={profile}>
            {error && <div className="att-error">{error}</div>}
            {loading ? (
                <div className="att-loading">Đang tải...</div>
            ) : !error ? (
                <AttendanceCard
                    rule={rule}
                    employeeId={employeeId}
                    record={todayRecord}
                    history={history}
                    onChanged={handleChecked}
                />
            ) : <button type="button" className="att-kpi-nav" onClick={() => window.location.reload()}>Tải lại dữ liệu</button>}
        </AppLayout>
    );
};

export default AttendancePage;
