import { getAllPages } from "../../../services/api/lists";
import axiosClient from "../../../services/api/axiosClient";

// API cho các entity liên quan đến nhân viên (đang đăng nhập)
const relatedApi = {
    employeeByUser(userId) {
        return axiosClient.get(`/Employee/get-by-user/${userId}`);
    },

    shiftsByEmployee(employeeId) {
        return axiosClient.get(
            `/EmployeeShift/by-employee/${employeeId}`
        );
    },

    attendanceByEmployee(employeeId) {
        return axiosClient.get(
            `/Attendance/by-employee/${employeeId}`
        );
    },

    // Chấm công thực tế (VÀO CA / RA CA)
    checkin(employeeId, type, photoUrl, note, coordinates = {}) {
        return axiosClient.post("/Attendance/checkin", {
            employeeId,
            type,
            photoUrl,
            note,
            ...coordinates,
        });
    },

    // Upload ảnh chấm công -> trả về đường dẫn tương đối /uploads/...
    // type: "checkin" (vào ca) / "checkout" (ra ca) -> 2 thư mục riêng.
    // QUAN TRỌNG: KHÔNG gán "Content-Type" thủ công. Khi body là FormData,
    // trình duyệt (adapter XHR của axios) tự đặt "multipart/form-data;
    // boundary=...". Gán giá trị cố định thiếu boundary khiến server không
    // tách được file -> IFormFile = null -> ảnh không được lưu (DB ra NULL).
    uploadPhoto(file, type) {
        const form = new FormData();
        form.append("file", file);
        // QUAN TRỌNG: bỏ đè default "Content-Type: application/json" của
        // axiosClient (instance dùng chung). Không làm vậy, axios sẽ JSON.stringify
        // luôn FormData -> server tách file = null -> ảnh không được lưu (DB ra NULL).
        // Đặt "Content-Type: undefined" để trình duyệt tự sinh
        // "multipart/form-data; boundary=..." (bắt buộc cho IFormFile).
        return axiosClient.post(
            `/Upload/photo?type=${encodeURIComponent(type || "checkin")}`,
            form,
            { headers: { "Content-Type": undefined } }
        );
    },

    myLeaves() {
        return axiosClient.get("/LeaveRequest/mine");
    },

    createLeaveRequest(payload) {
        return axiosClient.post("/LeaveRequest/create", payload);
    },
    leaveBalance(employeeId, year) {
        return axiosClient.get(`/LeaveLedger/summary/${employeeId}/year/${year}`);
    },
    cancelLeave(id, reason) {
        return axiosClient.post(`/LeaveRequest/cancel/${id}`, { reason });
    },
    leavePdf(id) {
        return axiosClient.get(`/LeaveRequest/${id}/pdf`, { responseType: "blob" });
    },

    contractsByEmployee(employeeId) {
        return axiosClient.get(
            `/EmployeeContract/by-employee/${employeeId}`
        );
    },

    salariesByEmployee(employeeId) {
        return axiosClient.get(
            `/EmployeeSalary/by-employee/${employeeId}`
        );
    },

    insuranceByEmployee(employeeId) {
        return axiosClient.get(
            `/EmployeeInsurance/by-employee/${employeeId}`
        );
    },

    bankAccountsByEmployee(employeeId) {
        return axiosClient.get(
            `/EmployeeBankAccount/by-employee/${employeeId}`
        );
    },

    // Dữ liệu cho thống kê: loại nghỉ phép (mức "phép còn lại")
    leaveTypesAll() {
        return getAllPages("/LeaveType/get-all");
    },

    // Lịch lễ của công ty (đánh dấu ngày lễ trên lệnh công)
    holidayCalendarAll() {
        return getAllPages("/HolidayCalendar/get-all");
    },

    // Quy định chấm công (quy tắc hiện hành) - trang "Quy định"
    attendanceRulesActive() {
        return axiosClient.get("/AttendanceRulePublic/active");
    },
};

export default relatedApi;
