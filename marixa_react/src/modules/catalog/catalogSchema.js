export const catalogs = {
  Department: {
    title: "Phòng ban",
    subtitle: "Cấu trúc tổ chức và đơn vị làm việc",
    fields: [
      ["code", "Mã phòng ban"],
      ["name", "Tên phòng ban"],
      ["description", "Mô tả"],
      ["managerId", "Quản lý phòng ban", "employee"],
    ],
  },
  Position: {
    title: "Chức vụ",
    subtitle: "Vai trò và vị trí trong tổ chức",
    fields: [
      ["code", "Mã chức vụ"],
      ["name", "Tên chức vụ"],
      ["description", "Mô tả"],
    ],
  },
  LeaveType: {
    title: "Loại nghỉ phép",
    subtitle: "Các loại nghỉ và mức hưởng hiện hành",
    fields: [
      ["code", "Mã loại phép"],
      ["name", "Tên loại phép"],
      ["maxDays", "Số ngày tối đa", "number"],
      ["isPaid", "Có trả lương", "checkbox"],
    ],
  },
  HolidayCalendar: {
    title: "Lịch ngày nghỉ",
    subtitle: "Ngày lễ, ngày nghỉ và ngày làm bù",
    fields: [
      ["name", "Tên ngày"],
      ["date", "Ngày áp dụng", "date"],
      ["type", "Loại ngày", "holiday"],
    ],
  },
  Bank: {
    title: "Ngân hàng",
    subtitle: "Danh sách ngân hàng dùng trong hồ sơ chi trả",
    fields: [
      ["code", "Mã ngân hàng"],
      ["name", "Tên ngân hàng"],
      ["shortName", "Tên viết tắt"],
    ],
  },
  EmployeeContract: {
    title: "Hợp đồng lao động",
    subtitle: "Thông tin hợp đồng, thời hạn và lịch sử thay đổi",
    fields: [
      ["employeeId", "Nhân viên", "employee"],
      ["contractNumber", "Số hợp đồng"],
      ["contractType", "Loại hợp đồng", "contract"],
      ["startDate", "Ngày bắt đầu", "date"],
      ["endDate", "Ngày kết thúc", "date"],
      ["note", "Ghi chú"],
    ],
  },
};
export function recordPayload(kind, form) {
  const result = { ...form };
  delete result.lastUpdatedTime;
  delete result.createdTime;
  if (kind === "Department") result.managerId = result.managerId || null;
  if (kind === "HolidayCalendar") {
    result.year = Number(result.date.slice(0, 4));
    result.type = Number(result.type);
  }
  if (kind === "EmployeeContract") {
    result.contractType = Number(result.contractType);
    result.endDate = result.endDate || null;
  }
  if (kind === "LeaveType")
    result.maxDays =
      result.maxDays === "" || result.maxDays == null
        ? null
        : Number(result.maxDays);
  return result;
}
