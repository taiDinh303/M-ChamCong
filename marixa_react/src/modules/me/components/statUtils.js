// ===== Logic thuần cho module Thống kê công =====
// Tách khỏi StatisticsPage để page mỏng, test được, không phụ thuộc React.
// Mọi hàm ở đây là pure (không có state / DOM).

import { calcOvertime, regularHours, businessDate } from "../../../utils/vnTime.js";

export const pad2 = (n) => String(n).padStart(2, "0");
export const dkey = (d) =>
    `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
export const durMin = (inIso, outIso) =>
    Math.round((new Date(outIso) - new Date(inIso)) / 60000);
// "8h04"
export const durHm = (min) => {
    if (min == null || min <= 0) return "0h";
    const total = Math.round(min);
    const h = Math.floor(total / 60);
    const m = total % 60;
    return `${h}h${m ? pad2(m) : ""}`;
};
// "8h"
export const hoursShort = (min) =>
    `${Number(Math.max(0, min / 60).toFixed(2))}h`;

// Trạng thái chấm công (đồng bộ AttendanceStatus).
export const statusLabel = (s) =>
    ({
        1: "Đủ công",
        2: "Thiếu công",
        3: "Thiếu công",
        4: "Vắng",
        5: "Nghỉ phép",
        6: "Lễ",
        7: "Ngoại tuần",
    })[s] || "Chưa chấm";

// ===== Lệnh công (calendar) =====
// code: full | short | absent | leave | future | nodata | rest
export const dotClass = (code) =>
    ({
        full: "ok",
        short: "warn",
        absent: "bad",
        leave: "info",
        future: "future",
        nodata: "nodata",
        rest: "rest",
    })[code] || "muted";

export const dayStatusText = (code) =>
    ({
        full: "Đủ công",
        short: "Thiếu công",
        absent: "Vắng",
        leave: "Nghỉ phép",
        future: "Chưa tới",
        nodata: "Chưa chấm",
        rest: "Nghỉ cuối tuần / lễ",
    })[code] || "";

// ===== Toán thống kê theo tháng =====
// holidayMap: { "YYYY-MM-DD": HolidayType }. Ngày lễ nghỉ loại 1,2,4; làm bù 3.
export const computeStats = (
    monthRows,
    leaveTypes,
    leaves,
    holidayMap,
    ym,
    options = {}
) => {
    const last = new Date(ym.y, ym.m + 1, 0).getDate();
    let expectedWorkdays = 0;
    const makeup = new Set();
    for (let d = 1; d <= last; d++) {
        const key = dkey(new Date(ym.y, ym.m, d));
        const dow = new Date(ym.y, ym.m, d).getDay();
        const weekend = dow === 0 || dow === 6;
        const ht = holidayMap[key];
        const isRest = ht === 1 || ht === 2 || ht === 4;
        const isMakeup = ht === 3;
        if (isRest) continue;
        if (isMakeup) {
            makeup.add(key);
            expectedWorkdays++;
            continue;
        }
        if (!weekend) expectedWorkdays++;
    }
    const standardMin = expectedWorkdays * (Number(options.standardHours) || 8) * 60;

    const worked = monthRows.filter((r) => r.checkInTime);
    const daysWorked = worked.length;
    const hoursWorked = worked.reduce((sum, r) => sum + regularHours(r) * 60, 0);
    const late = monthRows.filter((r) => r.status === 2).length;
    const leaveDays = monthRows.filter((r) => r.status === 5).length;

    // The ledger is authoritative (including grants, adjustments and refunds).
    const hasQuota = options.leaveBalance != null;
    const remainingLeave = hasQuota ? Number(options.leaveBalance.remaining) : null;

    // Tăng ca
    let otWeekday = 0;
    let otWeekend = 0;
    let otHoliday = 0;
    monthRows.forEach((r) => {
        const date = businessDate(r.attendanceDate);
        if (!date) return;
        const key = dkey(date);
        const mins = calcOvertime(r) * 60;
        const dow = date.getDay();
        if ([1, 2, 4].includes(holidayMap[key])) otHoliday += mins;
        else if ((dow === 0 || dow === 6) && !makeup.has(key)) otWeekend += mins;
        else otWeekday += mins;
    });

    return {
        expectedWorkdays,
        standardMin,
        daysWorked,
        hoursWorked,
        late,
        leaveDays,
        remainingLeave,
        hasQuota,
        otWeekday,
        otWeekend,
        otHoliday,
        makeup,
    };
};

// Trạng thái một ô lệnh công (pure, nhận ctx thay vì closure).
export const dayStatusOf = (date, ctx) => {
    const key = dkey(date);
    const row = ctx.byDate[key];
    const dow = date.getDay();
    const weekend = dow === 0 || dow === 6;
    const ht = ctx.holidayMap[key];
    if (ht === 1 || ht === 2 || ht === 4) return "rest";
    if (weekend && !ctx.stats.makeup.has(key)) return "rest";
    if (row) {
        if (row.status === 5) return "leave";
        if (row.status === 4) return "absent";
        if (
            row.checkInTime &&
            row.checkOutTime &&
            row.status !== 2 &&
            row.status !== 3
        )
            return "full";
        return "short";
    }
    if (key > ctx.todayKey) return "future";
    return "nodata";
};

// Lệnh công: danh sách tuần (bắt đầu T2), mỗi tuần là mảng ô {date, code}.
export const buildCalendar = (ym, dayStatus) => {
    const startDow = (new Date(ym.y, ym.m, 1).getDay() + 6) % 7; // T2=0
    const cells = [];
    for (let i = 0; i < 42; i++) {
        const date = new Date(ym.y, ym.m, 1 - startDow + i);
        if (date.getMonth() !== ym.m) continue;
        cells.push({ date, code: dayStatus(date) });
    }
    const weeks = [];
    for (let i = 0; i < cells.length; i += 7)
        weeks.push(cells.slice(i, i + 7));
    return weeks;
};

// Cảnh báo: chưa checkout, chờ duyệt, số ngày chưa có dữ liệu.
export const buildWarnings = (monthRows, dayStatus, ym) => {
    const list = [];
    monthRows.forEach((r) => {
        const dd = new Date(r.attendanceDate);
        const label = `${pad2(dd.getDate())}/${pad2(dd.getMonth() + 1)}`;
        if (r.checkInTime && !r.checkOutTime)
            list.push(`${label} bạn chưa checkout`);
        if (r.approvalStatus === 0)
            list.push(`${label} đang chờ duyệt công`);
    });
    const today = new Date();
    let noDataDays = 0;
    const todayKey = dkey(today);
    for (let d = 1; d <= new Date(ym.y, ym.m + 1, 0).getDate(); d++) {
        const key = dkey(new Date(ym.y, ym.m, d));
        if (key > todayKey) break;
        if (dayStatus(new Date(ym.y, ym.m, d)) === "nodata") noDataDays++;
    }
    return { list, noDataDays };
};
