import { useState, useEffect, useCallback } from "react";
import { getAuth } from "../../../services/auth/auth";
import Sidebar from "../../../components/layout/Sidebar";
import AttendanceCard from "../components/AttendanceCard";
import { GuideModal } from "../components/GuideModal";
import relatedApi from "../api/relatedApi";
import { formatVnDate } from "../../../utils/vnTime";
import "../attendance.css";

const AttendancePage = () => {
    const auth = getAuth();
    const userId = auth?.userId;
    const employeeId = auth?.employeeId;

    const [profile, setProfile] = useState(null);
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [guideOpen, setGuideOpen] = useState(false);

    // Bản ghi chấm công của HÔM NAY (để card chấm công + lịch sử đồng bộ)
    const todayRecord =
        history.find(
            (r) =>
                r.attendanceDate &&
                new Date(r.attendanceDate).toDateString() ===
                    new Date().toDateString()
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
                const profileRes = await relatedApi.employeeByUser(userId);
                setProfile(profileRes.data.data || null);

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

    const fullName =
        profile?.fullName || auth?.userName || "Nhân viên";
    const code = profile?.employeeCode || auth?.employeeCode || "";
    const todayStr = formatVnDate(new Date().toISOString());

    return (
        <div className="att-shell">
            <Sidebar profile={profile} />

            <main className="att-main">
                {/* Header nav */}
                <header className="att-topbar">
                    <div>
                        <h1>Bảng của tôi</h1>
                        <p>Chấm công · Ngày công · Quyền lợi của bạn</p>
                    </div>
                    <div className="att-topbar-actions">
                        <button
                            type="button"
                            className="att-topbar-btn"
                            onClick={() => (window.location.href = "/login")}
                        >
                            Đăng xuất
                        </button>
                        <button
                            type="button"
                            className="att-topbar-btn att-topbar-guide"
                            onClick={() => setGuideOpen(true)}
                        >
                            Hướng dẫn
                        </button>
                    </div>
                </header>

                {/* Banner nhân viên */}
                <section className="att-banner">
                    <div>
                        <h1>{fullName}</h1>
                        <p>
                            {code ? `${code} · ` : ""}Hôm nay {todayStr}
                        </p>
                    </div>
                </section>

                {error && <div className="att-error">{error}</div>}
                {loading ? (
                    <div className="att-loading">Đang tải...</div>
                ) : (
                    <AttendanceCard
                        employeeId={employeeId}
                        record={todayRecord}
                        history={history}
                        onChanged={handleChecked}
                    />
                )}

                {/* Nút hướng dẫn: mở lên khi nhấn */}
                {guideOpen && (
                    <GuideModal onClose={() => setGuideOpen(false)} />
                )}
            </main>
        </div>
    );
};

export default AttendancePage;
