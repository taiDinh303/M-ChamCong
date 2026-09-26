// Tiện ích giờ Việt Nam (UTC+7) dùng cho module chấm công.

export const VN_TIMEZONE = "Asia/Ho_Chi_Minh";

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
