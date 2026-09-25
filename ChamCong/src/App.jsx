import { BrowserRouter, Routes, Route } from "react-router-dom";
import LoginPage from "./modules/login/pages/LoginPage";
import EmployeePage from "./modules/employees/pages/EmployeePage";

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/employees" element={<EmployeePage />} />
            </Routes>
        </BrowserRouter>
    );
}

export default App;