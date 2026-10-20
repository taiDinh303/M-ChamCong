import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import LoginPage from "./modules/login/pages/LoginPage";
import ActivatePage from "./modules/activate/pages/ActivatePage";
import HrPage from "./modules/hr/pages/HrPage";
import ReportsPage from "./modules/hr/pages/ReportsPage";
import WorkPage from "./modules/work/WorkPage";
import ProfilePage from "./modules/profile/pages/ProfilePage";
import AdminHomepage from "./modules/admin/pages/AdminHomepage";
import AdminContractPage from "./modules/admin/contracts/pages/AdminContractPage";
import AdminWorkPage from "./modules/admin/work/AdminWorkPage";
import AdminBlockAccountPage from "./modules/admin/block-account/AdminBlockAccountPage";
import AdminRolesPage from "./modules/admin/roles/AdminRolesPage";
import AdminLeaveAdminPage from "./modules/admin/leaves/pages/LeaveAdminPage";
import {
    PayrollAdminPage,
    ResignedPage as AdminResignedPage,
} from "./modules/admin/leaves/pages/PayrollResignedReport";
import AdminAccountIssuancePage from "./modules/admin/accounts/pages/AccountIssuancePage";
import adminApi from "./modules/admin/api/adminApi";
import employeeApi from "./modules/hr/api/employeeApi";
import EmployeeAttendanceHistoryPage from "./modules/hr/attendance/pages/EmployeeAttendanceHistoryPage";
import EmployeeWorkSchedulePage from "./modules/hr/attendance/pages/EmployeeWorkSchedulePage";
import EmployeeStatisticsPage from "./modules/hr/attendance/pages/EmployeeStatisticsPage";
import EmployeeLeavePage from "./modules/hr/leaves/pages/EmployeeLeavePage";
import EmployeeContractPage from "./modules/hr/contracts/pages/EmployeeContractPage";
import HomePage from "./modules/home/pages/HomePage";
import UnauthorizedPage from "./modules/unauthorized/pages/UnauthorizedPage";
import RequireModule from "./components/common/RequireModule";
import RequireSession from "./components/common/RequireSession";
import ConnectionStatus from "./components/common/ConnectionStatus.jsx";
import LegacyModuleRedirect from "./components/common/LegacyModuleRedirect";
import PromotionsPage from "./modules/hr/pages/PromotionsPage";

// ===== Module "me": các trang nhân viên xem thông tin của mình =====
import MeClockInPage from "./modules/me/pages/ClockInPage";
import MeHistoryPage from "./modules/me/pages/HistoryPage";
import MeRulesPage from "./modules/me/pages/RulesPage";
import MeStatisticsPage from "./modules/me/pages/StatisticsPage";
import MeLeavePage from "./modules/me/pages/LeavePage";
import MeReportsPage from "./modules/me/pages/ReportsPage";
import MePromotionsPage from "./modules/me/pages/PromotionsPage";
import MeHandoverPage from "./modules/me/pages/HandoverPage";
import MeContractPage from "./modules/me/pages/ContractPage";
import MeSalaryPage from "./modules/me/pages/SalaryPage";
import MeInsurancePage from "./modules/me/pages/InsurancePage";
import MeBankPage from "./modules/me/pages/BankPage";

const guarded = (to, el) => <RequireModule to={to}>{el}</RequireModule>;

// Short legacy URLs resolve directly to the canonical personal module.
const REDIRECTS = [
    ["/attendance-history", "/me/attendance-history"],
    ["/attendance-rules", "/me/attendance-rules"],
    ["/statistics", "/me/statistics"],
    ["/leave", "/me/leave"],
    ["/reports", "/me/reports"],
    ["/promotions", "/me/promotions"],
    ["/handover", "/me/handover"],
    ["/contracts", "/me/contracts"],
    ["/salary", "/me/salary"],
    ["/insurance", "/me/insurance"],
    ["/bank-accounts", "/me/bank-accounts"],
];

function App() {
    return (
        <BrowserRouter>
            <ConnectionStatus />
            <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/kich-hoat" element={<ActivatePage />} />
                <Route path="/unauthorized" element={<UnauthorizedPage />} />
                <Route element={<RequireSession />}>
                <Route path="/" element={<Navigate to="/home" replace />} />
                <Route path="/home" element={<HomePage />} />
                <Route path="/finance" element={<Navigate to="/me/salary" replace />} />

                {/* Khu quản trị */}
                <Route
                    path="/hr"
                    element={guarded("/hr", <HrPage />)}
                />
                <Route
                    path="/hr/attendance-history"
                    element={guarded(
                        "/hr/attendance-history",
                        <EmployeeAttendanceHistoryPage hrMode />
                    )}
                />
                <Route
                    path="/hr/work-schedule"
                    element={guarded("/hr/work-schedule", <EmployeeWorkSchedulePage />)}
                />
                <Route
                    path="/hr/statistics"
                    element={guarded(
                        "/hr/statistics",
                        <EmployeeStatisticsPage hrMode />
                    )}
                />
                <Route path="/hr/contracts" element={guarded("/hr/contracts", <EmployeeContractPage />)} />
                <Route path="/hr/payroll" element={<Navigate to="/hr" replace />} />
                <Route
                    path="/hr/leaves"
                    element={guarded(
                        "/hr/leaves",
                        <EmployeeLeavePage hrMode />
                    )}
                />
                <Route
                    path="/hr/reports"
                    element={guarded("/hr/reports", <ReportsPage />)}
                />
                {/* Cấp tài khoản — HR vào (layout HR, dùng employeeApi) */}
                <Route
                    path="/hr/accounts"
                    element={guarded(
                        "/hr/accounts",
                        <AdminAccountIssuancePage layout="hr" api={employeeApi} />
                    )}
                />
                <Route path="/hr/promotions" element={guarded("/hr/promotions", <PromotionsPage />)} />
                <Route path="/hr/handover" element={guarded("/hr/handover", <MeHandoverPage reviewMode />)} />
                <Route
                    path="/admin/contracts"
                    element={guarded(
                        "/admin/contracts",
                        <AdminContractPage />
                    )}
                />
                <Route
                    path="/admin/leaves"
                    element={guarded(
                        "/admin/leaves",
                        <AdminLeaveAdminPage />
                    )}
                />
                <Route
                    path="/admin/payroll"
                    element={guarded(
                        "/admin/payroll",
                        <PayrollAdminPage />
                    )}
                />
                <Route
                    path="/admin/resigned"
                    element={guarded(
                        "/admin/resigned",
                        <AdminResignedPage />
                    )}
                />
                <Route
                    path="/admin/reports"
                    element={guarded(
                        "/admin/reports",
                        <ReportsPage admin />
                    )}
                />
                {/* Cấp tài khoản — Admin vào (layout Admin, dùng adminApi) */}
                <Route
                    path="/admin/accounts"
                    element={guarded(
                        "/admin/accounts",
                        <AdminAccountIssuancePage layout="admin" api={adminApi} />
                    )}
                />
                <Route path="/admin/employees" element={<Navigate to="/hr" replace />} />
                <Route path="/admin/employees/leaves" element={guarded("/admin/employees/leaves", <AdminLeaveAdminPage hrMode />)} />
                <Route path="/admin/employees/handover" element={guarded("/admin/employees/handover", <MeHandoverPage admin />)} />
                <Route path="/admin/employees/reports" element={guarded("/admin/employees/reports", <ReportsPage admin />)} />
                {/* URL cũ /admin/employees/accounts → redirect về /admin/accounts */}
                <Route path="/admin/employees/accounts" element={<Navigate to="/admin/accounts" replace />} />
                <Route path="/admin/employees/promotions" element={<Navigate to="/hr/promotions" replace />} />
                <Route path="/admin/employees/contracts" element={guarded("/admin/employees/contracts", <AdminContractPage hrMode />)} />
                <Route path="/admin/work" element={guarded("/admin/work", <AdminWorkPage />)} />
                <Route path="/admin/roles" element={guarded("/admin/roles", <AdminRolesPage />)} />
                <Route path="/admin/block-accounts" element={guarded("/admin/block-accounts", <AdminBlockAccountPage />)} />
                <Route path="/admin" element={guarded("/admin", <AdminHomepage />)} />

                {/* Khu nhân viên (module me) */}
                <Route path="/me" element={<MeClockInPage />} />
                <Route path="/me/attendance-history" element={<MeHistoryPage />} />
                <Route path="/me/attendance-rules" element={<MeRulesPage />} />
                <Route path="/me/statistics" element={<MeStatisticsPage />} />
                <Route path="/me/leave" element={<MeLeavePage />} />
                <Route path="/me/reports" element={guarded("/me/reports", <MeReportsPage />)} />
                <Route path="/me/promotions" element={guarded("/me/promotions", <MePromotionsPage selfMode />)} />
                <Route path="/me/handover" element={guarded("/me/handover", <MeHandoverPage />)} />
                <Route path="/me/contracts" element={<MeContractPage />} />
                <Route path="/me/salary" element={<MeSalaryPage />} />
                <Route path="/me/insurance" element={<MeInsurancePage />} />
                <Route path="/me/bank-accounts" element={<MeBankPage />} />

                {/* Công việc & hồ sơ (không nằm trong nhóm gộp) */}
                <Route path="/work" element={guarded("/work", <WorkPage />)} />
                <Route path="/profile" element={<ProfilePage />} />

                {/* Preserve bookmarked module URLs, including their query and fragment. */}
                <Route path="/attendance/*" element={<LegacyModuleRedirect />} />
                <Route path="/employees/*" element={<LegacyModuleRedirect />} />

                {/* Redirect các URL ngắn cũ -> /me/* */}
                {REDIRECTS.map(([from, to]) => (
                    <Route key={from} path={from} element={<Navigate to={to} replace />} />
                ))}

                </Route>
                <Route path="*" element={<Navigate to="/home" replace />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;
