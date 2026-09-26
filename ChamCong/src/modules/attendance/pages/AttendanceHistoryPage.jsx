import { useState, useEffect } from "react";
import { getAuth } from "../../../services/auth/auth";
import Sidebar from "../../../components/layout/Sidebar";
import HistoryTable from "../components/HistoryTable";
import relatedApi from "../api/relatedApi";
import "../attendance.css";

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

    return (
        <div className="att-shell">
            <Sidebar profile={profile} />

            <main className="att-main">
                <header className="att-main-head">
                    <div>
                        <h1>Lịch sử chấm công</h1>
                        <p>Tất cả các bản ghi chấm công của bạn</p>
                    </div>
                </header>

                {error && <div className="att-error">{error}</div>}
                {loading ? (
                    <div className="att-loading">Đang tải...</div>
                ) : (
                    <HistoryTable history={history} />
                )}
            </main>
        </div>
    );
};

export default AttendanceHistoryPage;
