import { cloneElement, isValidElement, useId, useState } from "react";
import { cardCells, tableChildren, tableColumns } from "./tableModel";

function CardRow({ row, columns }) {
    const [expanded, setExpanded] = useState(false);
    const generatedId = useId();
    const id = row.props.id || generatedId;
    const cells = cardCells(row.props.children, columns);
    const hasDetails = cells.some((cell) => !cell.visible);
    return cloneElement(row, { role: "row", id, "data-expanded": expanded }, <>
        {cells.map(({ cell, label, selection, action, primary, visible, wide }, index) => cloneElement(cell, {
            key: cell.key ?? index, role: "cell", "data-label": label,
            "data-card-selection": selection || undefined, "data-card-action": action || undefined,
            "data-card-primary": primary || undefined, "data-card-visible": visible || undefined,
            "data-card-wide": wide || undefined,
        }, selection ? <label className="mx-table-selection">{cell.props.children}</label> : cell.props.children))}
        {hasDetails && <td className="mx-table-toggle" role="cell" colSpan={columns.length || 1}>
            <button type="button" onClick={() => setExpanded((value) => !value)} aria-expanded={expanded} aria-controls={id}>
                {expanded ? "Thu gọn" : "Xem chi tiết"}<span aria-hidden="true">{expanded ? " ↑" : " ↓"}</span>
            </button>
        </td>}
    </>);
}

/** One set of controls/data: native desktop table, expandable mobile records. */
export default function ResponsiveTable({ children, className = "", mode = "cards", ...props }) {
    const columns = tableColumns(children);
    const content = mode === "scroll" ? children : tableChildren(children).map((section) => {
        if (!isValidElement(section)) return section;
        if (section.type === "thead") return cloneElement(section, {
            role: "rowgroup", "data-has-selection": columns.some((column) => column.selection) || undefined,
        }, tableChildren(section.props.children).map((row) => row?.type === "tr" ? cloneElement(row, { role: "row" },
            tableChildren(row.props.children).map((cell, index) => isValidElement(cell) ? cloneElement(cell, {
                role: "columnheader", scope: "col", "data-card-selection": columns[index]?.selection || undefined,
            }, columns[index]?.selection ? <label className="mx-table-selection">{cell.props.children}</label> : cell.props.children) : cell)) : row));
        if (section.type !== "tbody") return section;
        return cloneElement(section, { role: "rowgroup" }, tableChildren(section.props.children).map((row, index) =>
            row?.type === "tr" ? <CardRow key={row.key ?? index} row={row} columns={columns} /> : row));
    });
    const table = <table {...props} role="table" className={`${className} mx-table mx-table--${mode}`}>{content}</table>;
    return mode === "scroll" ? <div className="mx-table-matrix">
        <p className="mx-table-scroll-hint">Vuốt trong bảng để xem các cột. Cột đầu được giữ lại để đối chiếu.</p>
        <div className="mx-table-scroll" tabIndex={0} role="region" aria-label={props["aria-label"] || "Bảng so sánh, cuộn ngang để xem các cột"}>{table}</div>
    </div> : table;
}
