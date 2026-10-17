import { getAllPages } from "../../../../services/api/lists";
import axiosClient from "../../../../services/api/axiosClient";

// API quản trị chấm công (role Admin): toàn bộ nhân viên + thêm/sửa/xóa bản ghi
// + duyệt công + log chấm công (để tính KPI bất thường).
const employeeAttendanceApi = {
    attendanceAll() {
        return getAllPages("/Attendance/get-all");
    },
    employeesAll() {
        return getAllPages("/Employee/get-all");
    },
    employeeShiftsAll() {
        return getAllPages("/EmployeeShift/get-all");
    },
    createEmployeeShifts(payload) {
        return axiosClient.post("/EmployeeShift/create-bulk", payload);
    },
    // Sửa 1 bản ghi gán ca: { id, employeeId, shiftId, effectiveFrom, effectiveTo?, note? }
    updateEmployeeShifts(payload) {
        return axiosClient.put("/EmployeeShift/update", payload);
    },
    shiftsAll() {
        return getAllPages("/Shift/get-all");
    },
    // Sửa thông tin ca (giờ làm, tên...): payload là object Shift đầy đủ + id
    updateShift(payload) {
        return axiosClient.put("/Shift/update", payload);
    },
    create(payload) {
        return axiosClient.post("/Attendance/create", payload);
    },
    update(payload) {
        return axiosClient.put("/Attendance/update", payload);
    },
    softDelete(id) {
        return axiosClient.delete(`/Attendance/soft-delete/${id}`);
    },
    // Duyệt / từ chối công (approvedBy = nhân viên admin đang duyệt)
    approve(payload) {
        return axiosClient.post("/Attendance/approve", payload);
    },
    logsAll() {
        return getAllPages("/AttendanceLog/get-all");
    },
};

export default employeeAttendanceApi;
