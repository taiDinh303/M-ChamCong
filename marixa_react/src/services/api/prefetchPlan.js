export function workspacePlan(auth, today = new Date().toLocaleDateString("sv-SE", { timeZone: "Asia/Ho_Chi_Minh" })) {
    if (!auth?.token) return [];
    const tasks = [];
    const get = (url, paginated = false) => tasks.push({ url, paginated });
    const roles = auth.roles || [];
    const admin = roles.includes("Admin");
    const staff = admin || roles.some((role) => ["HR", "Manager"].includes(role));
    const employee = encodeURIComponent(auth.employeeId || "");
    if (auth.userId && auth.employeeId) get(`/Employee/get-by-user/${encodeURIComponent(auth.userId)}`);
    get("/AttendanceRulePublic/active");
    get("/LeaveType/get-all", true);
    get("/HolidayCalendar/get-all", true);
    get("/EmployeePromotion/options");
    if (employee) {
        for (const name of ["Attendance", "EmployeeContract", "EmployeeSalary", "EmployeeInsurance", "EmployeeBankAccount", "EmployeeShift", "OvertimeRequest", "AttendanceCorrection"])
            get(`/${name}/by-employee/${employee}`);
        get(`/LeaveLedger/summary/${employee}/year/${today.slice(0, 4)}`);
        get("/LeaveRequest/mine");
        get("/EmployeeReport/mine");
        get("/EmployeePromotion/mine");
        get("/EmployeeHandover/mine");
    }
    get("/WorkTask/mine");
    get("/WorkPerformance/mine");
    if (staff) {
        for (const name of ["Employee", "Department", "Position", "Attendance", "EmployeeContract", "EmployeeSalary", "EmployeeInsurance", "EmployeeBankAccount", "Bank", "LeaveRequest", "OvertimeRequest", "AttendanceCorrection", "Shift", "EmployeeShift", "User"])
            get(`/${name}/get-all`, true);
        for (const name of ["EmployeeReport", "EmployeePromotion", "EmployeeHandover", "WorkPerformance"])
            get(`/${name}/get-all`);
        get("/WorkProject/mine"); get("/WorkProject/subordinates"); get("/WorkTask/assigned");
    }
    if (admin) {
        get("/Payroll/get-all", true);
        get("/Role/get-all", true);
        get("/Employee/get-archived", true);
        get("/WorkTask/get-all");
    }
    // Files, security codes/tokens, logs and per-record histories load on demand.
    return tasks;
}

export async function runPrefetch(tasks, load, { concurrency = 4, signal, onProgress = () => {} } = {}) {
    let next = 0;
    let completed = 0;
    let failed = 0;
    const worker = async () => {
        while (next < tasks.length && !signal?.aborted) {
            const task = tasks[next++];
            try { await load(task); } catch { failed++; }
            completed++;
            onProgress({ completed, failed, total: tasks.length });
        }
    };
    await Promise.all(Array.from({ length: Math.min(concurrency, tasks.length) }, worker));
    return { completed, failed, total: tasks.length };
}
