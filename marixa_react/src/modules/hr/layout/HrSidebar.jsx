import { NavLink } from "react-router-dom";

const HR_NAV_ITEMS = [
    { to: "/hr", label: "Danh sách" },
    { to: "/hr/attendance-history", label: "Duyệt công" },
    { to: "/hr/statistics", label: "Thống kê công" },
    { to: "/hr/work-schedule", label: "Lịch làm" },
    { to: "/hr/contracts", label: "Hợp đồng" },
    { to: "/hr/leaves", label: "Duyệt nghỉ phép" },
    { to: "/hr/handover", label: "Bàn giao" },
    { to: "/hr/reports", label: "Báo cáo" },
    { to: "/hr/accounts", label: "Cấp tài khoản" },
    { to: "/hr/promotions", label: "Thăng chức" },
];

const HrSidebar = ({ collapsed }) => (
    <aside className={`att-sidebar${collapsed ? " collapsed" : ""}`}>
        <nav className="att-sidebar-nav" aria-label="Điều hướng nhân sự">
            {HR_NAV_ITEMS.map((item) => (
                <NavLink
                    key={item.to}
                    to={item.to}
                    end={item.to === "/hr"}
                    title={collapsed ? item.label : undefined}
                    className={({ isActive }) =>
                        `att-nav-item${isActive ? " active" : ""}`
                    }
                >
                    {item.label}
                </NavLink>
            ))}
        </nav>
    </aside>
);

export default HrSidebar;
