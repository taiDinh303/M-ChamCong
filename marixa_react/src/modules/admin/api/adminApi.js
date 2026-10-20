import { getAllPages } from "../../../services/api/lists";
import axiosClient from "../../../services/api/axiosClient";

// API cấp công ty (admin) - dùng get-all với pageSize lớn, tính phía client.

const adminApi = {
    employees() {
        return getAllPages("/Employee/get-all");
    },
    archivedEmployees() {
        return getAllPages("/Employee/get-archived");
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
    updateLeave(payload) {
        return axiosClient.put("/LeaveRequest/update", payload);
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
    createInsurance(payload) {
        return axiosClient.post("/EmployeeInsurance/create", payload);
    },
    createBankAccount(payload) {
        return axiosClient.post("/EmployeeBankAccount/create", payload);
    },
    blockUser(userId, payload) {
        return axiosClient.post(`/User/${userId}/block`, payload);
    },
    unblockUser(userId) {
        return axiosClient.post(`/User/${userId}/unblock`);
    },
    // === Phân quyền (Roles / gán role cho user) ===
    roles() {
        return getAllPages("/Role/get-all");
    },
    createRole(payload) {
        return axiosClient.post("/Role/create", payload);
    },
    updateRole(payload) {
        return axiosClient.put("/Role/update", payload);
    },
    deleteRole(id) {
        return axiosClient.delete(`/Role/soft-delete/${id}`);
    },
    userRoles(userId) {
        return axiosClient.get(`/UserRole/get-roles/${userId}`);
    },
    addRoleToUser(userId, roleId) {
        return axiosClient.post(`/UserRole/add-role?UserId=${userId}&RoleId=${roleId}`);
    },
    removeRoleFromUser(userId, roleId) {
        return axiosClient.delete(`/UserRole/remove-role?UserId=${userId}&RoleId=${roleId}`);
    },
    // === GIAO VIỆC + THEO DÕI + KÉO-THẢ + COMMENT (WorkTask) ===
    myWorkTasks() {
        return axiosClient.get("/WorkTask/mine");
    },
    assignedWorkTasks() {
        return axiosClient.get("/WorkTask/assigned");
    },
    allWorkTasks() {
        return axiosClient.get("/WorkTask/get-all");
    },
    workTask(id) {
        return axiosClient.get(`/WorkTask/${id}`);
    },
    createWorkTask(payload) {
        return axiosClient.post("/WorkTask/create", payload);
    },
    assignableEmployees() {
        return axiosClient.get("/WorkTask/assignable");
    },
    moveWorkTask(id, payload) {
        return axiosClient.put(`/WorkTask/${id}/move`, payload);
    },
    deleteWorkTask(id) {
        return axiosClient.delete(`/WorkTask/${id}`);
    },
    workTaskComments(id) {
        return axiosClient.get(`/WorkTask/${id}/comments`);
    },
    addWorkTaskComment(id, payload) {
        return axiosClient.post(`/WorkTask/${id}/comments`, payload);
    },
    // === ĐÁNH GIÁ HIỆU SUẤT (WorkPerformance) ===
    myWorkPerformance() {
        return axiosClient.get("/WorkPerformance/mine");
    },
    allWorkPerformance() {
        return axiosClient.get("/WorkPerformance/get-all");
    },
    workPerformance(id) {
        return axiosClient.get(`/WorkPerformance/${id}`);
    },
    createWorkPerformance(payload) {
        return axiosClient.post("/WorkPerformance/create", payload);
    },
    confirmWorkPerformance(id, payload) {
        return axiosClient.post(`/WorkPerformance/${id}/confirm`, payload);
    },
    // === DỰ ÁN / GÓI CÔNG VIỆC (WorkProject) ===
    workProjects() {
        return axiosClient.get("/WorkProject/mine");
    },
    workProjectsAll() {
        return axiosClient.get("/WorkProject/get-all");
    },
    selectableEmployees() {
        return axiosClient.get("/WorkProject/subordinates");
    },
    createWorkProject(formData) {
        return axiosClient.post("/WorkProject/create", formData, {
            headers: { "Content-Type": "multipart/form-data" },
        });
    },
    };

export default adminApi;
