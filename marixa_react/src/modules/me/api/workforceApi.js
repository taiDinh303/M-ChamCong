import axiosClient from "../../../services/api/axiosClient";
import { getAllPages } from "../../../services/api/lists";

const periodPath = (year, month) => `/AttendancePeriod/${year}/${month}`;
export const workforceApi = {
    period: (year, month) => axiosClient.get(periodPath(year, month)),
    periodHistory: (year, month) => axiosClient.get(`${periodPath(year, month)}/history`),
    snapshot: (year, month, version) => axiosClient.get(`${periodPath(year, month)}/snapshot`, { params: { version } }),
    periodAction: (year, month, action, reason) => axiosClient.post(`${periodPath(year, month)}/${action}`, { reason }),
    exportPeriod: (year, month, version) => axiosClient.get(`${periodPath(year, month)}/export.xlsx`, { params: { version }, responseType: "blob" }),
    overtime: (employeeId, reviewMode) => reviewMode ? getAllPages("/OvertimeRequest/get-all")
        : axiosClient.get(`/OvertimeRequest/by-employee/${employeeId}`),
    createOvertime: (payload) => axiosClient.post("/OvertimeRequest/create", payload),
    updateOvertime: (payload) => axiosClient.put("/OvertimeRequest/update", payload),
    reviewOvertime: (id, status, approvedBy, note) => axiosClient.post("/OvertimeRequest/review", { id, status, approvedBy, note }),
    updateRule: (rule) => axiosClient.put("/AttendanceRule/update", rule),
    corrections: (employeeId, staff) => staff ? getAllPages("/AttendanceCorrection/get-all")
        : axiosClient.get(`/AttendanceCorrection/by-employee/${employeeId}`),
    createCorrection: (payload) => axiosClient.post("/AttendanceCorrection/create", payload),
    reviewCorrection: (id, status, reviewedBy, reviewNote) => axiosClient.post("/AttendanceCorrection/review", { id, status, reviewedBy, reviewNote }),
    ledger: (employeeId, year) => axiosClient.get(`/LeaveLedger/by-employee/${employeeId}/year/${year}`),
    ledgerSummary: (employeeId, year) => axiosClient.get(`/LeaveLedger/summary/${employeeId}/year/${year}`),
    createLedgerEntry: (payload) => axiosClient.post("/LeaveLedger/create-entry", payload),
};
