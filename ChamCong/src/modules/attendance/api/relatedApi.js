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
    checkin(employeeId, type, photoUrl, note) {
        return axiosClient.post("/Attendance/checkin", {
            employeeId,
            type,
            photoUrl,
            note,
        });
    },

    leavesByEmployee(employeeId) {
        return axiosClient.get(
            `/LeaveRequest/by-employee/${employeeId}`
        );
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
};

export default relatedApi;
