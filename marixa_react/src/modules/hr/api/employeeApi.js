import { getAllPages } from "../../../services/api/lists";
import axiosClient from "../../../services/api/axiosClient";

// Company lists collect every server page before client-side filtering/totals.

const employeeApi = {
    employees() {
        return getAllPages("/Employee/get-all");
    },
    archivedEmployees() {
        return getAllPages("/Employee/get-archived");
    },
    restoreEmployee(id) {
        return axiosClient.post(`/Employee/restore/${id}`);
    },
    attendanceLogs() {
        return getAllPages("/AttendanceLog/get-all");
    },
    leaveRequests() {
        return getAllPages("/LeaveRequest/get-all");
    },
    payrolls() {
        return getAllPages("/Payroll/get-all");
    },
    contracts() {
        return getAllPages("/EmployeeContract/get-all");
    },
    activationCodes() {
        return getAllPages("/ActivationCode/get-all");
    },
    users() {
        return getAllPages("/User/get-all");
    },
    // Thêm: dữ liệu cần cho bộ lọc + KPI
    departments() {
        return getAllPages("/Department/get-all");
    },
    positions() {
        return getAllPages("/Position/get-all");
    },
    insurance() {
        return getAllPages("/EmployeeInsurance/get-all");
    },
    bankAccounts() {
        return getAllPages("/EmployeeBankAccount/get-all");
    },
    salaries() {
        return getAllPages("/EmployeeSalary/get-all");
    },
    banks() {
        return getAllPages("/Bank/get-all");
    },
    createActivationCode(payload) {
        return axiosClient.post("/Auth/create-activation-code", payload);
    },
    // === Quản trị: chấm công / hợp đồng / nghỉ phép / tài khoản ===
    attendances() {
        return getAllPages("/Attendance/get-all");
    },
    employeeReports(params = {}) {
        return axiosClient.get("/EmployeeReport/get-all", { params });
    },
    reviewEmployeeReport(id, payload) {
        return axiosClient.post(`/EmployeeReport/review/${id}`, payload);
    },
    forwardEmployeeReport(id, payload) {
        return axiosClient.post(`/EmployeeReport/forward/${id}`, payload);
    },
    upperDecision(id, payload) {
        return axiosClient.post(`/EmployeeReport/upper-decision/${id}`, payload);
    },
    respondToUpperRequest(id, payload) {
        return axiosClient.post(`/EmployeeReport/respond/${id}`, payload);
    },
    completeEmployeeReport(id) {
        return axiosClient.post(`/EmployeeReport/complete/${id}`);
    },
    reportViewed(id) {
        return axiosClient.post(`/EmployeeReport/viewed/${id}`);
    },
    uploadReportToUpper(id, formData) {
        return axiosClient.post(`/EmployeeReport/upper/${id}`, formData, { headers: { "Content-Type": "multipart/form-data" } });
    },
    markReportSent(id) {
        return axiosClient.post(`/EmployeeReport/mark-sent/${id}`);
    },
    downloadEmployeeReport(versionId) {
        return axiosClient.get(`/EmployeeReport/download/${versionId}`, { responseType: "blob" });
    },
    downloadReportAttachment(id) {
        return axiosClient.get(`/EmployeeReport/download-attachment/${id}`, { responseType: "blob" });
    },
    downloadUpperReport(id) {
        return axiosClient.get(`/EmployeeReport/upper-download/${id}`, { responseType: "blob" });
    },
    approveAttendance(payload) {
        return axiosClient.post("/Attendance/approve", payload);
    },
    createContract(payload) {
        return axiosClient.post("/EmployeeContract/create", payload);
    },
    updateContract(payload) {
        return axiosClient.put("/EmployeeContract/update", payload);
    },
    updateLeave(payload) {
        return axiosClient.put("/LeaveRequest/update", payload);
    },
    approveLeave(id, note = "") {
        return axiosClient.post(`/LeaveRequest/approve/${id}`, { note });
    },
    rejectLeave(id, note = "") {
        return axiosClient.post(`/LeaveRequest/reject/${id}`, { note });
    },
    cancelLeave(id, reason = "") {
        return axiosClient.post(`/LeaveRequest/cancel/${id}`, { reason });
    },
    updatePayroll(payload) {
        return axiosClient.put("/Payroll/update", payload);
    },
    createUser(payload) {
        return axiosClient.post("/User/create", payload);
    },
    createEmployee(payload) {
        return axiosClient.post("/Employee/create", payload);
    },
    updateEmployee(payload) {
        return axiosClient.put("/Employee/update", payload);
    },
    softDeleteEmployee(id) {
        return axiosClient.delete(`/Employee/soft-delete/${id}`);
    },
    createSalary(payload) {
        return axiosClient.post("/EmployeeSalary/create", payload);
    },
    updateSalary(payload) {
        return axiosClient.put("/EmployeeSalary/update", payload);
    },
    createInsurance(payload) {
        return axiosClient.post("/EmployeeInsurance/create", payload);
    },
    updateInsurance(payload) {
        return axiosClient.put("/EmployeeInsurance/update", payload);
    },
    createBankAccount(payload) {
        return axiosClient.post("/EmployeeBankAccount/create", payload);
    },
    updateBankAccount(payload) {
        return axiosClient.put("/EmployeeBankAccount/update", payload);
    },
};

export default employeeApi;
