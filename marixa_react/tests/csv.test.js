import test from "node:test";
import assert from "node:assert/strict";
import { parseCsv, toCsv } from "../src/modules/hr/hrUtils.js";

test("CSV export treats spreadsheet formulas as text and keeps numeric adjustments numeric", () => {
    const payloads = ["=HYPERLINK(\"https://example.invalid\")", "+SUM(1,2)", "-1+2", "@SUM(A1)", "  =1+1", "\t=1+1", "\r=1+1"];
    const rows = parseCsv(toCsv(payloads.map((value) => ({ value })), ["value"]));
    assert.deepEqual(rows.slice(1).map(([value]) => value), payloads.map((value) => "'" + value));
    assert.equal(parseCsv(toCsv([{ value: -0.5 }], ["value"]))[1][0], "-0.5");
});

test("CSV export preserves Vietnamese text, commas, quotes and line breaks", () => {
    const value = 'Nghỉ phép, "đã duyệt"\r\nNgày tiếp theo';
    assert.equal(parseCsv(toCsv([{ value }], ["value"]))[1][0], value);
});
