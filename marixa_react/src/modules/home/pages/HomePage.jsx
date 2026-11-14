import { useEffect, useMemo } from "react";
import { Link } from "react-router-dom";
import Header from "../../../components/layout/Header";
import { canAccessModule } from "../../../services/auth/permission";
import "../home.css";

const MODULES = [
  {
    to: "/me",
    icon: "🕗",
    title: "Chấm công",
    detail: "Vào ca · Ra ca",
    color: "blue",
  },
  {
    to: "/hr/catalogs",
    icon: "▦",
    title: "Danh mục",
    detail: "Dữ liệu nền tảng",
    color: "purple",
  },
  {
    to: "/hr",
    icon: "👥",
    title: "Nhân sự",
    detail: "Hồ sơ nhân viên",
    color: "pink",
  },
  {
    to: "/work",
    icon: "📋",
    title: "Công việc",
    detail: "Việc cần xử lý",
    color: "amber",
  },
  {
    to: "/admin",
    icon: "📊",
    title: "Dashboard",
    detail: "Điều hành hệ thống",
    color: "teal",
  },
  {
    to: "/accounting",
    icon: "💰",
    title: "Kế toán",
    detail: "Bảng lương · Tra cứu",
    color: "lime",
  },
];

const HomePage = () => {
  useEffect(() => {
    document.title = "Trang chủ · MARIXA";
  }, []);
  const visible = useMemo(
    () => MODULES.filter((m) => canAccessModule(m.to)),
    [],
  );

  return (
    <div className="home-shell">
      <Header minimal />

      <main className="home-main">
        {/* ===== HERO (greeting ở góc dưới phải) ===== */}
        <section className="home-hero">
          <div className="home-hero__copy">
            <span className="home-eyebrow">MARIXA · PEOPLE OPERATIONS</span>
            <h1>
              Con người là trung tâm.
              <br />
              Công việc thật rõ ràng.
            </h1>
            <p>
              Chọn module để chấm công, quản lý hồ sơ và theo dõi các nghiệp vụ
              nhân sự hằng ngày.
            </p>
            <div className="home-hero__actions">
              <Link className="home-cta" to="/me">
                Bắt đầu chấm công <span aria-hidden="true">↗</span>
              </Link>
              {canAccessModule("/admin") && (
                <Link className="home-cta--ghost" to="/admin">
                  Xem dashboard
                </Link>
              )}
            </div>
          </div>

          <div className="home-hero__art" aria-hidden="true">
            <span>M</span>
            <i />
            <i />
            <i />
          </div>
        </section>

        {/* ===== LAUNCHER ===== */}
        <section className="home-launcher" aria-labelledby="home-title">
          <div className="home-heading">
            <div>
              <span className="home-eyebrow">WORKSPACE</span>
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
                <span
                  className={`home-icon home-icon--${m.color}`}
                  aria-hidden="true"
                >
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
        <section className="home-daily" aria-labelledby="home-daily-title">
          <div className="home-heading">
            <div>
              <span className="home-eyebrow">MỖI NGÀY RÕ RÀNG HƠN</span>
              <h2 id="home-daily-title">Bắt đầu từ những việc quen thuộc</h2>
            </div>
          </div>
          <div className="home-daily-grid">
            {[
              {
                step: "01",
                title: "Ghi nhận ngày làm việc",
                text: "Vào ca, ra ca và xem quy định ảnh, vị trí đang áp dụng trước khi chấm công.",
                to: "/me",
                label: "Mở chấm công",
              },
              {
                step: "02",
                title: "Đối chiếu thông tin của bạn",
                text: "Xem lại lịch sử chấm công, kiểm tra giờ vào ra và gửi yêu cầu sửa công khi cần.",
                to: "/me/attendance-history",
                label: "Xem lịch sử công",
              },
              {
                step: "03",
                title: "Chủ động kế hoạch nghỉ",
                text: "Theo dõi số dư phép, gửi đơn và xem kết quả xử lý trong không gian cá nhân.",
                to: "/me/leave",
                label: "Quản lý nghỉ phép",
              },
            ]
              .filter((item) => canAccessModule(item.to))
              .map((item) => (
                <article className="home-daily-card" key={item.to}>
                  <span className="home-step" aria-hidden="true">
                    {item.step}
                  </span>
                  <h3>{item.title}</h3>
                  <p>{item.text}</p>
                  <Link to={item.to}>
                    {item.label} <span aria-hidden="true">↗</span>
                  </Link>
                </article>
              ))}
          </div>
        </section>
        <section
          className="home-help"
          id="home-help"
          aria-labelledby="home-help-title"
        >
          <div>
            <span className="home-eyebrow">HƯỚNG DẪN NHANH</span>
            <h2 id="home-help-title">Một vài điều cần biết</h2>
            <p>Thông tin cần thiết để sử dụng Marixa mỗi ngày.</p>
          </div>
          <div className="home-help-list">
            <details>
              <summary>Tôi nên làm gì khi thiếu giờ vào hoặc giờ ra?</summary>
              <p>
                Mở Lịch sử chấm công để kiểm tra ngày cần đối chiếu và gửi yêu
                cầu sửa công. Ghi rõ thời gian, lý do để người phụ trách xử lý.
              </p>
              <Link to="/me/attendance-history">Mở lịch sử chấm công ↗</Link>
            </details>
            <details>
              <summary>Vì sao mỗi tài khoản thấy các module khác nhau?</summary>
              <p>
                Các lối tắt được hiển thị theo vai trò tài khoản. Nếu thiếu
                quyền cần cho công việc, hãy trao đổi với người quản lý hoặc bộ
                phận nhân sự.
              </p>
            </details>
            <details>
              <summary>Tôi muốn thay đổi thông tin trong hồ sơ?</summary>
              <p>
                Xem thông tin cá nhân tại Hồ sơ. Với dữ liệu nhân sự, hợp đồng
                hoặc thông tin chi trả cần chỉnh sửa, liên hệ người phụ trách
                nhân sự để kiểm tra và cập nhật.
              </p>
              <Link to="/profile">Xem hồ sơ của tôi ↗</Link>
            </details>
          </div>
        </section>
        <section className="home-closing" aria-labelledby="home-closing-title">
          <span className="home-eyebrow">SẴN SÀNG CHO NGÀY LÀM VIỆC</span>
          <h2 id="home-closing-title">
            Ít tìm kiếm hơn.
            <br />
            Tập trung vào công việc hơn.
          </h2>
          <p>
            Từ chấm công đến hồ sơ cá nhân, mọi lối tắt cần thiết đều ở đây.
          </p>
          <Link className="home-cta" to="/work">
            Mở không gian công việc <span aria-hidden="true">↗</span>
          </Link>
        </section>
      </main>
      <footer className="home-footer">
        <div className="home-footer-inner">
          <div className="home-footer-grid">
            <div className="home-footer-brand">
              <strong>
                MARIXA<span aria-hidden="true">.</span>
              </strong>
              <p>
                Không gian chung cho chấm công, hồ sơ và công việc hằng ngày của
                đội ngũ.
              </p>
              <span className="home-footer-note">
                Thông tin hiển thị theo vai trò tài khoản.
              </span>
            </div>
            <nav aria-label="Không gian làm việc ở chân trang">
              <h2>Không gian làm việc</h2>
              {visible.map((item) => (
                <Link key={item.to} to={item.to}>
                  {item.title}
                </Link>
              ))}
            </nav>
            <nav aria-label="Hướng dẫn và tài khoản">
              <h2>Hướng dẫn & tài khoản</h2>
              <a href="#home-help">Câu hỏi thường gặp</a>
              <Link to="/me/attendance-rules">Quy định chấm công</Link>
              <Link to="/profile">Hồ sơ của tôi</Link>
              <p className="home-footer-support">
                Cần điều chỉnh thông tin hoặc quyền truy cập? Liên hệ người quản
                lý hoặc bộ phận nhân sự của bạn.
              </p>
            </nav>
          </div>
          <div className="home-footer-bottom">
            <span>© {new Date().getFullYear()} MARIXA</span>
            <span>Chấm công & quản lý nhân sự</span>
            <span>Giờ Việt Nam · UTC+7</span>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default HomePage;
