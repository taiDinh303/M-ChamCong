import "./page-banner.css";

// Banner trang kiểu "block-accounts": nền tối gradient + icon + dải KPI.
// Không có tiêu đề (sidebar đã hiện tiêu đề trang).
// props:
//   icon  : ký tự/emoji nhận diện trang
//   kpis  : array { icon, label, value, tone: blue|green|red|gray|gold }
//   accent: màu viền trái (optional)
const PageBanner = ({ icon, kpis = [], accent, action }) => {
    return (
        <div
            className="page-banner"
            style={accent ? { borderLeft: `3px solid ${accent}` } : undefined}
        >
            <span className="page-banner-ico" aria-hidden="true">
                {icon}
            </span>
            {action != null && <div className="page-banner-action">{action}</div>}
            {kpis.length > 0 && (
                <div className="page-banner-kpis">
                    {kpis.map((k, i) => (
                        <div
                            key={i}
                            className={`page-banner-kpi${k.tone ? ` page-banner-kpi--${k.tone}` : ""}`}
                        >
                            {k.icon != null && (
                                <span className="page-banner-kpi-ico" aria-hidden="true">
                                    {k.icon}
                                </span>
                            )}
                            <div>
                                <span className="page-banner-kpi-label">{k.label}</span>
                                <strong>{k.value}</strong>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};

export default PageBanner;
