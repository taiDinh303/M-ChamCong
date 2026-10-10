import { useState, useEffect } from "react";
import { getAuth } from "../../../services/auth/auth";
import AppLayout from "../../../components/layout/AppLayout";
import HistoryTable from "../components/HistoryTable";
import PageBanner from "../components/PageBanner";
import relatedApi from "../api/relatedApi";
import "../attendance.css";
import "../../employees/employee.css";

const AttendanceHistoryPage = () => {
    const auth = getAuth();
    const userId = auth?.userId;
    const employeeId = auth?.employeeId;

    const [profile, setProfile] = useState(null);
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        const load = async () => {
            try {
                if (userId) {
                    const p = await relatedApi.employeeByUser(userId);
                    setProfile(p.data.data || null);
                }
                if (employeeId) {
                    const res = await relatedApi.attendanceByEmployee(
                        employeeId
                    );
                    setHistory(res.data.data || []);
                }
            } catch (err) {
                setError(
                    err.response?.data?.message ||
                        err.message ||
                        "Không thể tải lịch sử chấm công."
                );
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [userId, employeeId]);

    // ===== KPI banner =====
    const total = history.length;
    const complete = history.filter((r) => r.checkInTime && r.checkOutTime).length;
    const missing = total - complete;

    return (
        <AppLayout profile={profile}>
            <div className="att-content">
                {error && <div className="att-error">{error}</div>}
                {!loading && !error && (
                    <PageBanner
                        icon="🗂️"
                        kpis={[
                            { icon: "▣", label: "Tổng lượt", value: total, tone: "blue" },
                            { icon: "✓", label: "Hoàn chỉnh", value: complete, tone: "green" },
                            { icon: "⚠", label: "Thiếu mốc", value: missing, tone: "red" },
                        ]}
                    />
                )}
                {loading ? (
                    <div className="att-loading">Đang tải...</div>
                ) : (
                    <HistoryTable history={history} />
                )}
            </div>
        </AppLayout>
    );
};

export default AttendanceHistoryPage;
