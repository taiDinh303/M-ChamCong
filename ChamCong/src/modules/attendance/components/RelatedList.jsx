import { useNavigate } from "react-router-dom";

const RelatedList = ({ title, items, empty, render, to }) => {
    const navigate = useNavigate();

    const content =
        items && items.length > 0 ? (
            <ul className="att-list">
                {items.map((item) => (
                    <li key={item.id}>
                        {render(item)}
                    </li>
                ))}
            </ul>
        ) : (
            <p className="att-muted">{empty}</p>
        );

    return (
        <section
            className={`att-card att-related${to ? " att-clickable" : ""}`}
            onClick={to ? () => navigate(to) : undefined}
            role={to ? "link" : undefined}
            tabIndex={to ? 0 : undefined}
            onKeyDown={(e) => {
                if (to && (e.key === "Enter" || e.key === " ")) {
                    e.preventDefault();
                    navigate(to);
                }
            }}
        >
            <h2 className="att-related-title">
                {title}
                {to && <span className="att-related-arrow">›</span>}
            </h2>
            {content}
        </section>
    );
};

export default RelatedList;
