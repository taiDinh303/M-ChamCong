import "./hr-hero.css";
import MetricCard from "../../components/common/MetricCard";

// Keep only the page's useful metrics, without a duplicate title banner.
const HrHero = ({ kpis }) => {
    return (
        <>
            {kpis && kpis.length > 0 && (
                <div className="hr-hero">
                    <div className="hr-kpi-row" role="list">
                    {kpis.map((k, i) => (
                        <MetricCard
                            key={k.label + i}
                            role="listitem"
                            className={`hr-kpi${k.tone ? ` hr-kpi--${k.tone}` : ""}`}
                            icon={k.ico} label={k.label} value={k.value} hint={k.sub} tone={k.tone}
                        />
                    ))}
                    </div>
                </div>
            )}
        </>
    );
};

export default HrHero;
