const RelatedList = ({ title, items, empty, render }) => (
    <section className="att-card att-related">
        <h2>{title}</h2>
        {items && items.length > 0 ? (
            <ul className="att-list">
                {items.map((item) => (
                    <li key={item.id}>
                        {render(item)}
                    </li>
                ))}
            </ul>
        ) : (
            <p className="att-muted">{empty}</p>
        )}
    </section>
);

export default RelatedList;
