import test from "node:test";
import assert from "node:assert/strict";
import { assignmentValidation, assignmentEndLimit, calendarDaysInclusive, attendanceState } from "../src/modules/hr/attendance/schedulePolicy.js";
import { filterPayrolls, money } from "../src/modules/accounting/payrollView.js";

test("assignment includes both endpoints, accepts one employee and caps 31 calendar days across year boundaries", () => {
    const form = { shiftId: "shift", effectiveFrom: "2026-12-01", effectiveTo: "2026-12-31" };
    assert.equal(assignmentValidation(form, ["employee"]), "");
    assert.match(assignmentValidation({ ...form, effectiveTo: "2027-01-01" }, ["employee"]), /31 ngày/);
    assert.match(assignmentValidation(form, []), /ít nhất 1/);
    assert.equal(assignmentEndLimit("2026-12-15"), "2027-01-14");
    assert.equal(calendarDaysInclusive("2026-10-26", "2026-11-01"), 7);
    assert.match(assignmentValidation({ ...form, effectiveTo: "2026-11-30" }, ["employee"]), /Ngày kết thúc/);
});

test("payroll filters combine month, status and Vietnamese employee search without shifting business dates", () => {
    const rows = [
        { id: 1, payrollMonth: "2026-10-01T00:00:00", employeeName: "Đinh Văn Tài", status: 3 },
        { id: 2, payrollMonth: "2026-10-01", employeeName: "Nhân viên B", status: 1 },
        { id: 3, payrollMonth: "2026-11-01", employeeName: "Đinh Văn Tài", status: 3 },
    ];
    assert.deepEqual(filterPayrolls(rows, { month: "2026-10", status: "3", search: "VĂN TÀI" }).map((row) => row.id), [1]);
    assert.equal(filterPayrolls(rows, {}).length, 3);
    assert.equal(filterPayrolls(rows, { month: "2025-10" }).length, 0);
    assert.equal(money(null), "—");
    assert.equal(money("invalid"), "—");
    assert.match(money(0), /^0/);
});

test("schedule distinguishes not clocked, ongoing, missing exit and explicit absence without inventing absence", () => {
    assert.equal(attendanceState(null, "2026-10-20", "2026-10-21").label, "Chưa chấm công");
    assert.equal(attendanceState({}, "2026-10-20", "2026-10-21").label, "Chưa chấm công");
    assert.equal(attendanceState({ checkInTime: "08:00" }, "2026-10-21", "2026-10-21").label, "Đang trong ca");
    assert.equal(attendanceState({ checkInTime: "08:00" }, "2026-10-20", "2026-10-21").label, "Thiếu lượt ra");
    assert.equal(attendanceState({ checkOutTime: "17:00" }, "2026-10-21", "2026-10-21").label, "Thiếu lượt vào");
    assert.equal(attendanceState({ status: 4 }, "2026-10-20", "2026-10-21").label, "Vắng");
    assert.equal(attendanceState({ status: 5 }, "2026-10-20", "2026-10-21").label, "Nghỉ phép");
    assert.equal(attendanceState({ checkInTime: "08:00", checkOutTime: "17:00" }, "2026-10-21", "2026-10-21").label, "Đủ vào/ra");
});
