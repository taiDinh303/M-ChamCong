export const calendarDaysInclusive = (from, to) => {
    if (!/^\d{4}-\d{2}-\d{2}$/.test(from || "") || !/^\d{4}-\d{2}-\d{2}$/.test(to || "")) return 0;
    const start = Date.parse(`${from}T00:00:00Z`);
    const end = Date.parse(`${to}T00:00:00Z`);
    if (!Number.isFinite(start) || !Number.isFinite(end)) return 0;
    return Math.floor((end - start) / 86400000) + 1;
};

export const assignmentEndLimit = (from) => {
    const date = new Date(`${from}T00:00:00Z`);
    if (!Number.isFinite(date.getTime())) return undefined;
    date.setUTCDate(date.getUTCDate() + 30);
    return date.toISOString().slice(0, 10);
};

export const assignmentValidation = (form, employeeIds) => {
    if (employeeIds.length < 1) return "Vui lòng chọn ít nhất 1 nhân viên.";
    if (!form.shiftId) return "Vui lòng chọn ca làm.";
    const days = calendarDaysInclusive(form.effectiveFrom, form.effectiveTo);
    if (days < 1) return "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.";
    if (days > 31) return "Mỗi lượt gán tối đa 31 ngày, tính cả ngày đầu và ngày cuối.";
    return "";
};

export const attendanceState = (row, date, today) => {
    const hasIn = Boolean(row?.checkInTime);
    const hasOut = Boolean(row?.checkOutTime);
    if (hasIn && hasOut) return { label: "Đủ vào/ra", tone: "ok" };
    if (hasIn) return { label: date === today ? "Đang trong ca" : "Thiếu lượt ra", tone: "warn" };
    if (hasOut) return { label: "Thiếu lượt vào", tone: "warn" };
    if (Number(row?.status) === 4) return { label: "Vắng", tone: "bad" };
    if ([5, 6, 7].includes(Number(row?.status))) return {
        label: { 5: "Nghỉ phép", 6: "Ngày lễ", 7: "Ngày nghỉ tuần" }[row.status], tone: "info",
    };
    return { label: "Chưa chấm công", tone: "muted" };
};
