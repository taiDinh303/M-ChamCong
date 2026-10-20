import { Children, cloneElement, Fragment, isValidElement } from "react";

// Flatten fragments and conditional columns in the same order React renders them.
export function tableChildren(children, prefix = "") {
    return Children.toArray(children).flatMap((child) =>
        isValidElement(child) && child.type === Fragment ? tableChildren(child.props.children, `${prefix}${child.key}/`)
            : [prefix && isValidElement(child) ? cloneElement(child, { key: `${prefix}${child.key}` }) : child]);
}

export function tableText(node) {
    if (typeof node === "string" || typeof node === "number") return String(node);
    if (isValidElement(node)) return tableText(node.props.children);
    return Array.isArray(node) ? node.map(tableText).join(" ").trim() : "";
}

function hasCheckbox(node) {
    return tableChildren(node).some((child) => isValidElement(child) &&
        ((child.type === "input" && child.props.type === "checkbox") || hasCheckbox(child.props.children)));
}

function hasEditor(node) {
    return tableChildren(node).some((child) => isValidElement(child) &&
        (["input", "select", "textarea"].includes(child.type) || hasEditor(child.props.children)));
}

const summaryField = /trạng thái|tình trạng|^duyệt$|duyệt chấm công|^ngày|số ngày|bắt đầu|kết thúc|hết hạn|thời gian|^giờ|công thường|tăng ca|^ot$|tổng|thực lĩnh|thực nhận|status|date|period|duration|hours|total|net pay/i;

export function tableColumns(children) {
    const head = tableChildren(children).find((child) => child?.type === "thead");
    const row = head && tableChildren(head.props.children).find((child) => child?.type === "tr");
    return tableChildren(row?.props.children).filter(isValidElement).map((cell) => {
        const selection = hasCheckbox(cell.props.children);
        const label = cell.props["data-label"] || cell.props["aria-label"] || tableText(cell.props.children) || (selection ? "Chọn" : "Thao tác");
        return { label, selection, action: !selection && (!tableText(cell.props.children) || /thao tác|hành động|yêu cầu/i.test(label)) };
    });
}

export function cardCells(cells, columns) {
    let field = 0;
    return tableChildren(cells).filter(isValidElement).map((cell, index) => {
        const column = columns[index] || { label: cell.props["data-label"] || "Thông tin" };
        const wide = Number(cell.props.colSpan || 1) > 1;
        const selection = !wide && column.selection;
        const action = !wide && column.action;
        const primary = !wide && !selection && !action && field === 0;
        const visible = wide || selection || action || hasEditor(cell.props.children) || (!selection && field < 2) || summaryField.test(column.label);
        if (!wide && !selection && !action) field++;
        return { cell, label: cell.props["data-label"] || column.label, selection, action, primary, visible, wide };
    });
}
