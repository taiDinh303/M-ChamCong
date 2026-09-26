import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import LoginPage from "./modules/login/pages/LoginPage";
import EmployeePage from "./modules/employees/pages/EmployeePage";
import AttendancePage from "./modules/attendance/pages/AttendancePage";
import LeavePage from "./modules/leave/pages/LeavePage";
import ContractPage from "./modules/contracts/pages/ContractPage";
import SalaryPage from "./modules/salary/pages/SalaryPage";
import InsurancePage from "./modules/insurance/pages/InsurancePage";
import BankPage from "./modules/bank-accounts/pages/BankPage";
import StatisticsPage from "./modules/statistics/pages/StatisticsPage";

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/employees" element={<EmployeePage />} />
                <Route path="/attendance" element={<AttendancePage />} />
                <Route path="/leave" element={<LeavePage />} />
                <Route path="/contracts" element={<ContractPage />} />
                <Route path="/salary" element={<SalaryPage />} />
                <Route path="/insurance" element={<InsurancePage />} />
                <Route path="/bank-accounts" element={<BankPage />} />
                <Route path="/statistics" element={<StatisticsPage />} />
                <Route path="*" element={<Navigate to="/attendance" replace />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;
