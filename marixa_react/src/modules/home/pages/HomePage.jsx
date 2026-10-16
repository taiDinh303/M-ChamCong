import { useMemo } from "react";
import { Link } from "react-router-dom";
import Header from "../../../components/layout/Header";
import { canAccessModule } from "../../../services/auth/permission";
import "../home.css";

const MODULES = [
    { to: "/attendance", icon: "🕗", title: "Chấm công", detail: "Vào ca · Ra ca", hint: "Nhanh nhất", color: "blue", featured: true },
    { to: "/kich-hoat", icon: "🔑", title: "Kích hoạt", detail: "Đặt mật khẩu", color: "purple" },
    { to: "/employees", icon: "👥", title: "Nhân sự", detail: "Hồ sơ nhân viên", color: "pink" },
    { to: "/work", icon: "📋", title: "Công việc", detail: "Việc cần xử lý", color: "amber" },
    { to: "/admin", icon: "📊", title: "Dashboard", detail: "Điều hành hệ thống", color: "teal" },
];

function greetByTime(hour) {
    if (hour < 5) return "Chào bạn, vẫn làm việc khuya?";
    if (hour < 11) return "Chào buổi sáng";
    if (hour < 14) return "Chào buổi trưa";
    if (hour < 18) return "Chào buổi chiều";
    return "Chào buổi tối";
}

const HomePage = () => {
    const visible = useMemo(
        () => MODULES.filter((m) => canAccessModule(m.to)),
        []
    );
    const featured = visible.filter((m) => m.featured);
    const secondary = visible.filter((m) => !m.featured);

    const today = useMemo(() => {
        const now = new Date();
        const dateStr = new Intl.DateTimeFormat("vi-VN", {
            weekday: "long",
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
        }).format(now);
        const hour = now.getHours();
        return { date: dateStr, greet: greetByTime(hour), time: now.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }) };
    }, []);

    return (
        <div className="home-shell">
            <Header minimal />
            <main className="home-main">
                {/* ===== GREETING / CONTEXT STRIP ===== */}
                <section className="home-greet" aria-label="Chào mừng">
                    <div className="home-greet__text">
                        <span className="home-greet__date">{today.date}</span>
                        <strong>{today.greet} 👋</strong>
                    </div>
                    <span className="home-greet__clock" aria-hidden="true">⏱ {today.time}</span>
                </section>

                {/* ===== HERO ===== */}
                <section className="home-hero">
                    <div className="home-hero__copy">
                        <span className="home-eyebrow">MARIXA · PEOPLE OPERATIONS</span>
                        <h1>Mọi công việc<br />trong một không gian.</h1>
                        <p>Chọn module để chấm công, quản lý hồ sơ và theo dõi các nghiệp vụ nhân sự hằng ngày.</p>
                        <div className="home-hero__actions">
                            <Link className="home-cta" to="/attendance">Bắt đầu chấm công <span aria-hidden="true">↗</span></Link>
                            <Link className="home-cta--ghost" to="/admin">Xem dashboard</Link>
                        </div>
                    </div>
                    <div className="home-hero__art" aria-hidden="true"><span>M</span><i /><i /><i /></div>
                </section>

                {/* ===== LAUNCHER ===== */}
                <section className="home-launcher" aria-labelledby="home-title">
                    <div className="home-heading">
                        <div>
                            <span className="home-eyebrow">BUSINESS LAUNCHER</span>
                            <h2 id="home-title">Không gian làm việc</h2>
                        </div>
                        <p>{visible.length} module khả dụng</p>
                    </div>

                    {featured.map((m, i) => (
                        <Link key={m.to} className={`home-feature home-feature--${m.color}`} to={m.to}>
                            <span className="home-feature__index">{String(i + 1).padStart(2, "0")}</span>
                            <span className={`home-icon home-icon--${m.color} home-feature__icon`} aria-hidden="true">{m.icon}</span>
                            <span className="home-feature__body">
                                <em>{m.hint || "Điểm khởi đầu"}</em>
                                <strong>{m.title}</strong>
                                <small>{m.detail}</small>
                            </span>
                            <span className="home-feature__cta" aria-hidden="true">→</span>
                        </Link>
                    ))}

                    <div className="home-grid">
                        {secondary.map((m, i) => (
                            <Link className={`home-card home-card--${m.color}`} to={m.to} key={m.to} style={{ "--i": i }}>
                                <span className="home-card__index">{String(featured.length + i + 1).padStart(2, "0")}</span>
                                <span className={`home-icon home-icon--${m.color}`} aria-hidden="true">{m.icon}</span>
                                <span className="home-card__meta">
                                    <strong>{m.title}</strong>
                                    <span className="home-card__detail">{m.detail}</span>
                                </span>
                            </Link>
                        ))}
                    </div>
                </section>

                <footer className="home-footer">
                    <strong>MARIXA</strong>
                    <span>Chấm công &amp; quản lý nhân sự</span>
                </footer>
            </main>
        </div>
    );
};

export default HomePage;
