import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import AdminAppLayout from "../layout/AdminAppLayout";
import "../../../modules/me/attendance.css";
import "../admin.css";
import adminApi from "../api/adminApi";
import { ChartCard, TrendLine } from "../../../components/charts/charts";
import "../../../components/charts/charts.css";
import { localeForLanguage, translate, useLanguage } from "../../../services/i18n/LanguageProvider";

// ===== Đồng hồ giờ Việt Nam (UTC+7) =====
// Lấy "bây giờ" theo giờ VN dù máy chạy ở múi giờ khác.
const vnNow = () => {
    const utc = Date.now() + new Date().getTimezoneOffset() * 60000;
    return new Date(utc + 7 * 3600000);
};

const pad = (n) => String(n).padStart(2, "0");

// Các chức năng chính trên trang chủ admin (icon + nhãn phụ).
// Nhóm A: vận hành (module nghiệp vụ). Nhóm B: quản trị (tài khoản / phân quyền).
const ICONS = [
    { to: "/hr", icon: "👥", label: "Nhân sự", sub: "Hồ sơ & tuyển mới" },
    { to: "/admin/contracts", icon: "📄", label: "Hợp đồng", sub: "Ký & theo dõi hạn" },
    { to: "/admin/leaves", icon: "🌴", label: "Nghỉ phép", sub: "Duyệt đơn xin nghỉ" },
    { to: "/admin/payroll", icon: "💰", label: "Lương", sub: "Bảng lương tháng" },
    { to: "/admin/resigned", icon: "📦", label: "Nghỉ việc", sub: "Lưu trữ nhân viên" },
    { to: "/admin/reports", icon: "📑", label: "Báo cáo", sub: "Tổng hợp công việc" },
    { to: "/admin/work", icon: "🧾", label: "Công tác", sub: "Quá trình công tác" },
    { to: "/admin/roles", icon: "🛡️", label: "Phân quyền", sub: "Vai trò & người dùng" },
    { to: "/admin/block-accounts", icon: "🔒", label: "Chặn tài khoản", sub: "Chặn / mở chặn" },
    { to: "/admin/accounts", icon: "🔑", label: "Cấp tài khoản", sub: "Mã kích hoạt" },
];

const VnClock = () => {
    const [now, setNow] = useState(vnNow());
    const { language } = useLanguage();
    const locale = localeForLanguage(language);
    const L = (text) => translate(text, language);

    useEffect(() => {
        const id = setInterval(() => setNow(vnNow()), 1000);
        return () => clearInterval(id);
    }, []);

    const minuteDeg = (now.getMinutes() / 60) * 360;
    const hourDeg = ((now.getHours() % 12) + now.getMinutes() / 60) * 30;
    const dateLabel = now.toLocaleDateString(locale, {
        weekday: "long",
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
    });

    return (
        <div className="admhome-clock" aria-label="Đồng hồ giờ Việt Nam">
            <div className="admhome-clock-face">
                {/* 12 chấm số giờ */}
                {Array.from({ length: 12 }).map((_, i) => (
                    <span
                        key={i}
                        className="admhome-clock-tick"
                        style={{
                            transform: `rotate(${i * 30}deg) translateY(-34px)`,
                        }}
                    />
                ))}
                <div
                    className="admhome-clock-hand admhome-clock-hand--hour"
                    style={{ transform: `rotate(${hourDeg}deg)` }}
                />
                <div
                    className="admhome-clock-hand admhome-clock-hand--minute"
                    style={{ transform: `rotate(${minuteDeg}deg)` }}
                />
                <div className="admhome-clock-pin" />
            </div>
            <div className="admhome-clock-meta">
                <strong>
                    {pad(now.getHours())}:{pad(now.getMinutes())}:{pad(now.getSeconds())}
                </strong>
                <span style={{ textTransform: "capitalize" }}>{dateLabel}</span>
                <span className="admhome-clock-tz">{L("Giờ Việt Nam · GMT+7")}</span>
            </div>
        </div>
    );
};

const AdminHomepage = () => {
    const navigate = useNavigate();
    const { language } = useLanguage();
    const L = (text) => translate(text, language);

    const [trendData, setTrendData] = useState([]);
    const [trendLoading, setTrendLoading] = useState(true);
    const [trendError, setTrendError] = useState("");
    const [trendVersion, setTrendVersion] = useState(0);
    useEffect(() => {
        let active = true;
        const load = async () => {
            setTrendLoading(true);
            setTrendError("");
            try {
                const r = await adminApi.attendances();
                const all = r.data.data?.items || [];
                const days = [];
                for (let i = 6; i >= 0; i--) {
                    const d = vnNow();
                    d.setDate(d.getDate() - i);
                    const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
                    const set = new Set(all.filter((a) => a.attendanceDate && a.attendanceDate.startsWith(key) && a.checkInTime).map((a) => a.employeeId));
                    days.push({ name: d.toLocaleDateString("vi-VN", { weekday: "short" }), full: key, value: set.size });
                }
                if (active) setTrendData(days);
            } catch {
                if (active) setTrendError("Không thể tải số liệu chấm công. Vui lòng thử lại.");
            } finally {
                if (active) setTrendLoading(false);
            }
        };
        load();
        return () => { active = false; };
    }, [trendVersion]);

    return (
        <AdminAppLayout
            title={L("Trang chủ")}
            subtitle={L("Chọn chức năng để quản lý")}
        >
            <div className="att-content">
                <div className="admhome">
                    <div className="admhome-clock-card">
                        <VnClock />
                    </div>

                    <ChartCard
                        title={L("Check-in 7 ngày")}
                        hint={L("Số người có giờ vào ca trong 7 ngày gần nhất")}
                        empty={trendData.length === 0}
                        loading={trendLoading}
                        error={trendError}
                        onRetry={() => setTrendVersion((version) => version + 1)}
                    >
                        <TrendLine data={trendData} yKey="value" />
                    </ChartCard>
                    <div className="admhome-grid">
                        {ICONS.map((x) => (
                            <button
                                key={x.to}
                                type="button"
                                className="admhome-tile"
                                onClick={() => navigate(x.to)}
                            >
                                <span className="admhome-tile-ico">{x.icon}</span>
                                <span className="admhome-tile-label">{L(x.label)}</span>
                                <span className="admhome-tile-sub">{L(x.sub)}</span>
                            </button>
                        ))}
                    </div>
                </div>
            </div>
        </AdminAppLayout>
    );
};

export default AdminHomepage;
