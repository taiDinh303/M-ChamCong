import test from "node:test";
import assert from "node:assert/strict";
import { createElement as h, Fragment } from "react";
import { cardCells, tableChildren, tableColumns } from "../src/components/common/tableModel.js";
import { guidanceFor } from "../src/components/layout/routeGuidance.js";

test("conditional/fragment headers stay aligned with their data columns", () => {
    const head = h("thead", null, h("tr", null, h("th", null, "Nhân viên"), false,
        h(Fragment, null, h("th", null, "Ngày"), h("th", null, "Trạng thái")), h("th")));
    const columns = tableColumns([head]);
    assert.deepEqual(columns.map((column) => column.label), ["Nhân viên", "Ngày", "Trạng thái", "Thao tác"]);
    const rows = cardCells([h("td", null, "A"), h(Fragment, null, h("td", null, "19/10"), h("td", null, "Chờ duyệt")), h("td", null, h("button", null, "Duyệt"))], columns);
    assert.deepEqual(rows.map((cell) => cell.label), columns.map((column) => column.label));
    assert.equal(rows.at(-1).action, true);
});

test("checkbox column does not consume the primary employee field", () => {
    const head = h("thead", null, h("tr", null, h("th", null, h("input", { type: "checkbox" })), h("th", null, "Nhân viên"), h("th", null, "Phòng ban")));
    const columns = tableColumns(head);
    const cells = cardCells([h("td", null, h("input", { type: "checkbox" })), h("td", null, "A"), h("td", null, "Kỹ thuật")], columns);
    assert.equal(cells[0].selection, true);
    assert.equal(cells[0].primary, false);
    assert.equal(cells[1].primary, true);
});

test("mobile cards keep status and actions visible while long fields can expand", () => {
    const labels = ["Nhân viên", "Phòng ban", "Lý do", "Trạng thái", "Thao tác"];
    const columns = tableColumns(h("thead", null, h("tr", null, labels.map((label) => h("th", { key: label }, label)))));
    const cells = cardCells(labels.map((label) => h("td", { key: label }, label)), columns);
    assert.deepEqual(cells.map((cell) => cell.visible), [true, true, false, true, true]);
});

test("spanning empty/error messages stay visible without a fabricated field label", () => {
    const [cell] = cardCells(h("td", { colSpan: 8 }, "Không có bản ghi"), [{ label: "Nhân viên" }]);
    assert.equal(cell.wide, true);
    assert.equal(cell.visible, true);
});

test("empty results in a checkbox table never inherit its selection layout", () => {
    const [cell] = cardCells(h("td", { colSpan: 7 }, "Không tìm thấy nhân viên"), [{ label: "Chọn", selection: true }]);
    assert.equal(cell.selection, false);
    assert.equal(cell.action, false);
    assert.equal(cell.primary, false);
    assert.equal(cell.wide, true);
});

test("review cards show dates and totals before the user decides, and never hide form fields", () => {
    const labels = ["Nhân viên", "Loại phép", "Thời gian", "Số ngày", "Trạng thái", "Ghi chú"];
    const columns = labels.map((label) => ({ label }));
    const cells = cardCells(labels.map((label) => h("td", { key: label }, label === "Ghi chú" ? h("input", { required: true }) : label)), columns);
    assert.ok(cells.every((cell) => cell.visible));
});

test("cell controls, events and explicit labels survive the presentation model", () => {
    let clicks = 0;
    const button = h("button", { onClick: () => clicks++ }, "Duyệt");
    const original = h("td", { "data-label": "Duyệt công" }, button);
    const [cell] = cardCells(original, [{ label: "Thao tác", action: true }]);
    assert.equal(cell.cell.props.children, button);
    assert.equal(cell.label, "Duyệt công");
    cell.cell.props.children.props.onClick();
    assert.equal(clicks, 1);
});

test("flattening skips absent conditional columns and preserves actual elements", () => {
    const input = h("input", { value: "A", readOnly: true });
    const flattened = tableChildren([null, false, h(Fragment, null, input)]);
    assert.equal(flattened.length, 1);
    assert.equal(flattened[0].type, "input");
    assert.deepEqual(flattened[0].props, input.props);
});

test("separate fragment key scopes stay unique and stable after flattening", () => {
    const children = [h("td", null, "A"), h(Fragment, { key: "dates" }, h("td", { key: "value" }, "19/10")),
        h(Fragment, { key: "amounts" }, h("td", { key: "value" }, 8))];
    const keys = tableChildren(children).map((cell) => cell.key);
    assert.equal(new Set(keys).size, keys.length);
    assert.deepEqual(tableChildren(children).map((cell) => cell.key), keys);
});

test("HR and Admin guidance follows the actual page rather than the module root", () => {
    assert.match(guidanceFor("/hr/leaves/").title, /nghỉ phép/);
    assert.match(guidanceFor("/admin/roles").steps.join(" "), /ít nhất một Admin/);
    assert.match(guidanceFor("/hr/attendance-history").title, /duyệt công/);
});

test("personal and Home pages do not receive HR/Admin workflow instructions", () => {
    for (const path of ["/home", "/me/leave", "/profile", "/work", "/administrator"]) assert.equal(guidanceFor(path), null);
});
