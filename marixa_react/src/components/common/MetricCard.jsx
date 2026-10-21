/** Compact, presentational summary. Status is always expressed by its label too. */
export default function MetricCard({ icon, label, value, hint, tone = "blue", className = "", role }) {
    return <div className={`mx-metric ${className}`} data-tone={tone} role={role}>
        {icon != null && <span className="mx-metric-icon" aria-hidden="true">{icon}</span>}
        <div className="mx-metric-body">
            <span className="mx-metric-label">{label}</span>
            <strong className="mx-metric-value">{value}</strong>
            {hint != null && <small className="mx-metric-hint">{hint}</small>}
        </div>
    </div>;
}
