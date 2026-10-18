import "./hr-hero.css";
import PageHeading from "../../components/layout/PageHeading";

// Promote the page heading into the shared header; keep KPIs in the content.
const HrHero = ({ ico, title, sub, kpis }) => {
    return (
        <>
            {title && <PageHeading icon={ico} title={title} description={sub} />}
            {kpis && kpis.length > 0 && (
                <div className="hr-hero">
                    <div className="hr-kpi-row" role="list">
                    {kpis.map((k, i) => (
                        <div
                            key={k.label + i}
                            role="listitem"
                            className={`hr-kpi${k.tone ? ` hr-kpi--${k.tone}` : ""}`}
                        >
                            {k.ico && <span className="hr-kpi-ico" aria-hidden="true">{k.ico}</span>}
                            <div>
                                <span className="hr-kpi-label">{k.label}</span>
                                <strong>{k.value}</strong>
                                {k.sub && <small className="hr-kpi-sub">{k.sub}</small>}
                            </div>
                        </div>
                    ))}
                    </div>
                </div>
            )}
        </>
    );
};

export default HrHero;
