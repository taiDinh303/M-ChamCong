// Tiện ích giờ Việt Nam (UTC+7) dùng cho module chấm công.

export const VN_TIMEZONE = "Asia/Ho_Chi_Minh";

export const vnTodayKey = (now = new Date()) =>
    new Intl.DateTimeFormat("sv-SE", { timeZone: VN_TIMEZONE }).format(now);

// API business dates are labels, not instants to shift using the browser zone.
export const businessDate = (value) => {
    if (!value) return null;
    const [y, m, d] = String(value).slice(0, 10).split("-").map(Number);
    return Number.isFinite(y + m + d) ? new Date(y, m - 1, d) : null;
};

export const regularHours = (row) => Math.max(0, Number(row?.actualHours) || 0);

// The form uses VN wall-clock hours. Advance checkout across real calendar
// boundaries rather than constructing impossible dates such as October 32.
export const attendanceTimes = (date, checkIn, checkOut) => {
    const start = checkIn ? new Date(`${date}T${checkIn}:00+07:00`) : null;
    let end = checkOut ? new Date(`${date}T${checkOut}:00+07:00`) : null;
    if (start && end && end < start) end = new Date(end.getTime() + 86400000);
    return { checkInTime: start?.toISOString() || null, checkOutTime: end?.toISOString() || null };
};

export const breakLabel = (rule) => {
    const minutes = Number(rule?.breakMinutes) || 0;
    if (minutes <= 0) return "Không trừ giờ nghỉ";
    const start = (rule?.breakStartTime || "12:00").slice(0, 5);
    const [h, m] = start.split(":").map(Number);
    const end = (h * 60 + m + minutes) % 1440;
    return `${start} – ${String(Math.floor(end / 60)).padStart(2, "0")}:${String(end % 60).padStart(2, "0")}`;
};

// Định dạng giờ HH:mm tại Việt Nam (rỗng nếu không có giá trị).
export const formatVnTime = (iso) =>
    iso
        ? new Date(iso).toLocaleTimeString("vi-VN", {
              hour: "2-digit",
              minute: "2-digit",
              hour12: false,
              timeZone: VN_TIMEZONE,
          })
        : "";

// Định dạng ngày DD/MM/YYYY tại Việt Nam (rỗng nếu không có giá trị).
export const formatVnDate = (d) =>
    d
        ? new Date(d).toLocaleDateString("vi-VN", { timeZone: VN_TIMEZONE })
        : "";

// Only the API can recognize actual work intersecting approved OT windows.
// Never infer approved OT from check-out time or raw elapsed hours in the UI.
export const calcOvertime = (row) => Math.max(0, Number(row?.overtimeHours) || 0);

// Dạng hiển thị "Xh" (rỗng nếu không có OT).
export const formatOvertime = (ot) => (ot > 0 ? `${ot}h` : "");
