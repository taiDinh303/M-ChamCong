import "./page-banner.css";
import MetricCard from "../../../components/common/MetricCard";

// Summary metrics/actions; workspace-colors.css applies the shared palette.
// Page identity is available through document.title and the main landmark.
// props:
//   icon  : ký tự/emoji nhận diện trang
//   kpis  : array { icon, label, value, tone: blue|green|red|gray|gold|purple|pink|teal }
const PageBanner = ({ icon, kpis = [], action }) => {
    return (
        <div className="page-banner">
            <span className="page-banner-ico" aria-hidden="true">
                {icon}
            </span>
            {action != null && <div className="page-banner-action">{action}</div>}
            {kpis.length > 0 && (
                <div className="page-banner-kpis">
                    {kpis.map((k, i) => (
                        <MetricCard
                            key={i}
                            className={`page-banner-kpi${k.tone ? ` page-banner-kpi--${k.tone}` : ""}`}
                            icon={k.icon} label={k.label} value={k.value} tone={k.tone}
                        />
                    ))}
                </div>
            )}
        </div>
    );
};

export default PageBanner;
