import { NavLink } from "react-router-dom";
import AuthenticatedShell from "../../components/layout/AuthenticatedShell";
import "../me/attendance.css";
import "./accounting.css";

const Sidebar = ({ collapsed }) => <aside className={`att-sidebar${collapsed ? " collapsed" : ""}`}>
    <nav className="att-sidebar-nav" aria-label="Điều hướng kế toán">
        {[ ["/accounting", "Tổng quan"], ["/accounting/payroll", "Bảng lương"] ].map(([to, label]) =>
            <NavLink key={to} to={to} end title={collapsed ? label : undefined} className={({ isActive }) => `att-nav-item${isActive ? " active" : ""}`}>{label}</NavLink>)}
    </nav>
</aside>;

export default function AccountingLayout({ children }) {
    return <AuthenticatedShell storageKey="marixa_accounting_sidebar_collapsed" sidebar={(collapsed) => <Sidebar collapsed={collapsed} />}>{children}</AuthenticatedShell>;
}
