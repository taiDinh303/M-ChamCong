import { useMemo } from "react";
import { Link } from "react-router-dom";
import Header from "../../../components/layout/Header";
import { canAccessModule } from "../../../services/auth/permission";
import "../home.css";

const MODULES = [
    { to: "/attendance", icon: "🕗", title: "Chấm công", detail: "Vào ca · Ra ca", color: "blue" },
    { to: "/kich-hoat", icon: "🔑", title: "Kích hoạt", detail: "Đặt mật khẩu", color: "purple" },
    { to: "/employees", icon: "👥", title: "Nhân sự", detail: "Hồ sơ nhân viên", color: "pink" },
    { to: "/work", icon: "📋", title: "Công việc", detail: "Việc cần xử lý", color: "amber" },
    { to: "/admin", icon: "📊", title: "Dashboard", detail: "Điều hành hệ thống", color: "teal" },
    { to: "/finance", icon: "💰", title: "Kế toán", detail: "Tài chính · Báo cáo", color: "lime" },
];

const HomePage = () => {
    const visible = useMemo(
        () => MODULES.filter((m) => canAccessModule(m.to)),
        []
    );

    return (
        <div className="home-shell">
            <Header minimal />

            <main className="home-main">
                {/* ===== HERO (greeting ở góc dưới phải) ===== */}
                <section className="home-hero">
                    <div className="home-hero__copy">
                        <span className="home-eyebrow">MARIXA · PEOPLE OPERATIONS</span>
                        <h1>Mọi công việc<br />trong một không gian.</h1>
                        <p>Chọn module để chấm công, quản lý hồ sơ và theo dõi các nghiệp vụ nhân sự hằng ngày.</p>
                        <div className="home-hero__actions">
                            <Link className="home-cta" to="/attendance">
                                Bắt đầu chấm công <span aria-hidden="true">↗</span>
                            </Link>
                            {canAccessModule("/admin") && <Link className="home-cta--ghost" to="/admin">Xem dashboard</Link>}
                        </div>
                    </div>

                    <div className="home-hero__art" aria-hidden="true">
                        <span>M</span><i /><i /><i />
                    </div>
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

                    <div className="home-grid">
                        {visible.map((m, i) => (
                            <Link
                                className={`home-card home-card--${m.color}`}
                                to={m.to}
                                key={m.to}
                                style={{ "--i": i }}
                            >
                                <span className="home-card__index">
                                    {String(i + 1).padStart(2, "0")}
                                </span>
                                <span className={`home-icon home-icon--${m.color}`} aria-hidden="true">
                                    {m.icon}
                                </span>
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
