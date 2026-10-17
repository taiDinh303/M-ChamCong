import test from "node:test";
import assert from "node:assert/strict";
import { collectPages, itemsOf, apiErrorMessage } from "../src/services/api/contracts.js";
import { calcOvertime, regularHours, breakLabel, businessDate, vnTodayKey, attendanceTimes } from "../src/utils/vnTime.js";
import { computeStats, hoursShort, durHm } from "../src/modules/me/components/statUtils.js";

test("paginated lists honor server page counts even when it caps the requested size", async () => {
    const requests = [];
    const result = await collectPages(async (page) => {
        requests.push(page);
        return { data: { data: { items: [page], currentPage: page, totalPages: 3, pageSize: 1 } } };
    });
    assert.deepEqual(requests, [1, 2, 3]);
    assert.deepEqual(itemsOf(result), [1, 2, 3]);
});
test("empty paginated and direct-array responses both remain valid", async () => {
    assert.deepEqual(itemsOf(await collectPages(async () => ({ data: { data: { items: [], totalPages: 0 } } }))), []);
    assert.deepEqual(itemsOf(await collectPages(async () => ({ data: { data: [1, 2] } }))), [1, 2]);
});
test("a failed later page never silently returns an incomplete list", async () => {
    await assert.rejects(collectPages(async (page) => {
        if (page === 2) throw new Error("temporarily unavailable");
        return { data: { data: { items: [1], totalPages: 2 } } };
    }), /temporarily unavailable/);
});
test("an unexpected collection shape is reported instead of showing a zero total", async () => {
    await assert.rejects(collectPages(async () => ({ data: { data: {} } })), /định dạng/);
});
test("an empty middle page stops pagination with an error", async () => {
    await assert.rejects(collectPages(async (page) => ({ data: { data: { items: page === 1 ? [1] : [], totalPages: 3 } } })), /trang rỗng/);
});
test("the API error envelope keeps its actionable business message", () => {
    assert.equal(apiErrorMessage({ response: { status: 409, data: { code: "PERIOD_LOCKED", message: "Kỳ đã khóa." } } }), "Kỳ đã khóa.");
    assert.doesNotMatch(apiErrorMessage({ response: { status: 500, data: "<html>private stack</html>" } }), /html|private stack/);
});
test("late checkout never creates OT without an API-approved amount", () => {
    assert.equal(calcOvertime({ checkOutTime: "2026-10-16T16:00:00Z", actualHours: 12 }), 0);
    assert.equal(calcOvertime({ overtimeHours: 1.25, actualHours: 8 }), 1.25);
    assert.equal(calcOvertime({ overtimeHours: -2 }), 0);
});
test("normal hours retain the API decimal precision", () => {
    assert.equal(regularHours({ actualHours: 7.58 }), 7.58);
    assert.equal(hoursShort(7.58 * 60), "7.58h");
    assert.equal(durHm(59.9), "1h");
});
test("break labels use the configured start and duration", () => {
    assert.equal(breakLabel({ breakStartTime: "12:30:00", breakMinutes: 90 }), "12:30 – 14:00");
    assert.equal(breakLabel({ breakStartTime: "23:30:00", breakMinutes: 60 }), "23:30 – 00:30");
    assert.equal(breakLabel({ breakMinutes: 0 }), "Không trừ giờ nghỉ");
});
test("business date labels are not shifted and today's label uses Vietnam time", () => {
    const date = businessDate("2026-10-17T00:00:00Z");
    assert.equal(date.getDate(), 17);
    assert.equal(vnTodayKey(new Date("2026-10-16T18:00:00Z")), "2026-10-17");
});

test("manual attendance hours use VN time and checkout crosses month/year boundaries", () => {
    assert.deepEqual(attendanceTimes("2026-12-31", "23:30", "01:15"), {
        checkInTime: "2026-12-31T16:30:00.000Z", checkOutTime: "2026-12-31T18:15:00.000Z",
    });
    assert.equal(attendanceTimes("2026-10-31", "22:00", "08:00").checkOutTime, "2026-11-01T01:00:00.000Z");
    assert.deepEqual(attendanceTimes("2026-10-17", "", ""), { checkInTime: null, checkOutTime: null });
});
test("statistics use net normal hours and approved OT rather than elapsed time", () => {
    const stats = computeStats([{ attendanceDate: "2026-10-16", checkInTime: "2026-10-16T01:00:00Z", checkOutTime: "2026-10-16T11:00:00Z", actualHours: 8, overtimeHours: 1, status: 1 }], [], [], {}, { y: 2026, m: 9 }, { leaveBalance: { remaining: 4.5 }, standardHours: 7 });
    assert.equal(stats.hoursWorked, 480);
    assert.equal(stats.otWeekday, 60);
    assert.equal(stats.remainingLeave, 4.5);
    assert.equal(stats.standardMin, stats.expectedWorkdays * 420);
});
test("makeup days do not turn the full workday into holiday OT", () => {
    const stats = computeStats([{ attendanceDate: "2026-10-17", actualHours: 8, overtimeHours: 0.5 }], [], [], { "2026-10-17": 3 }, { y: 2026, m: 9 });
    assert.equal(stats.otWeekday, 30);
    assert.equal(stats.otHoliday, 0);
    assert.equal(stats.hasQuota, false);
    assert.equal(stats.remainingLeave, null);
});
