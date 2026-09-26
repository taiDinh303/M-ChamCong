// Tiện ích giờ Việt Nam (UTC+7) dùng cho module chấm công.

export const VN_TIMEZONE = "Asia/Ho_Chi_Minh";

// Giờ (0-23) tại Việt Nam cho thời điểm hiện tại.
export const getVnNowHour = () => {
    const now = new Date();
    const utc = now.getTime() + now.getTimezoneOffset() * 60000;
    return new Date(utc + 7 * 3600000).getHours();
};

// Giờ (0-23) tại Việt Nam cho một thời điểm ISO.
export const getVnHour = (iso) => {
    const d = new Date(iso);
    const utc = d.getTime() + d.getTimezoneOffset() * 60000;
    return new Date(utc + 7 * 3600000).getHours();
};

// Định dạng HH:mm tại Việt Nam (rỗng nếu không có giá trị).
export const formatVnTime = (iso) =>
    iso
        ? new Date(iso).toLocaleTimeString("vi-VN", {
              hour: "2-digit",
              minute: "2-digit",
              hour12: false,
              timeZone: VN_TIMEZONE,
          })
        : "";

// Ca hành chính theo giờ VN: 7h -> Ca sáng, 12h -> Ca chiều, 18h -> Ca tối.
const ADMIN_SHIFTS = [
    { name: "Ca tối", start: "18:00", end: "22:00", min: 18 },
    { name: "Ca chiều", start: "12:00", end: "18:00", min: 12 },
    { name: "Ca sáng", start: "07:00", end: "12:00", min: -24 },
];

const findAdminShift = (vnHour) =>
    vnHour >= 18 || vnHour < 5
        ? ADMIN_SHIFTS[0]
        : vnHour >= 12
            ? ADMIN_SHIFTS[1]
            : ADMIN_SHIFTS[2];

export const getAdministrativeShift = (vnHour) =>
    findAdminShift(vnHour).name;

// Khoảng giờ vào ca - ra ca của ca hành chính (ví dụ: "07:00 - 12:00").
export const getAdministrativeShiftWindow = (vnHour) => {
    const s = findAdminShift(vnHour);
    return `${s.start} - ${s.end}`;
};
