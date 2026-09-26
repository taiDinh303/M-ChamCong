import { useMemo } from "react";
import { NavLink } from "react-router-dom";
import { getAuth } from "../../services/auth/auth";
import { formatVnDate } from "../../utils/vnTime";

const NAV_ITEMS = [
    { to: "/attendance", label: "Trang chủ" },
    { to: "/attendance-history", label: "Lịch sử chấm công" },
    { to: "/statistics", label: "Thống kê công" },
    { to: "/leave", label: "Nghỉ phép" },
    { to: "/contracts", label: "Hợp đồng" },
    { to: "/salary", label: "Bảng lương" },
    { to: "/insurance", label: "Bảo hiểm & thuế" },
    { to: "/bank-accounts", label: "Tài khoản ngân hàng" },
];

const statusLabel = (s) =>
    ({
        1: "Thử việc",
        2: "Đang làm",
        3: "Tạm nghỉ",
        4: "Đã nghỉ việc",
        5: "Chấm dứt",
    })[s] || "—";

const Sidebar = ({ profile }) => {
    const auth = getAuth();
    const initial = (profile?.fullName || auth?.userName || "?")
        .trim()
        .charAt(0)
        .toUpperCase();

    const profileRows = useMemo(
        () => [
            ["Mã NV", profile?.employeeCode || auth?.employeeCode || "—"],
            ["Phòng ban", profile?.departmentName || "—"],
            ["Chức vụ", profile?.positionName || "—"],
            ["Điện thoại", profile?.phoneNumber || "—"],
            ["Ngày vào", profile?.startDate ? formatVnDate(profile.startDate) : "—"],
            ["Trạng thái", statusLabel(profile?.status)],
        ],
        [profile, auth]
    );

    return (
        <aside className="att-sidebar">
            <div className="att-sidebar-brand">
                <span className="att-sidebar-logo">M</span>
                <span>MARIXA</span>
            </div>

            <nav className="att-sidebar-nav">
                {NAV_ITEMS.map((item) => (
                    <NavLink
                        key={item.to}
                        to={item.to}
                        className={({ isActive }) =>
                            `att-nav-item${isActive ? " active" : ""}`
                        }
                    >
                        {item.label}
                    </NavLink>
                ))}
            </nav>

            <div className="att-sidebar-foot">
                <div className="att-sidebar-profile">
                    <span className="att-sidebar-avatar" aria-hidden="true">
                        {initial}
                    </span>
                    <div>
                        <strong>{profile?.fullName || auth?.userName || "—"}</strong>
                        <span>{profile?.employeeCode || auth?.employeeCode || ""}</span>
                    </div>
                </div>
                <dl className="att-sidebar-info">
                    {profileRows.map(([label, value]) => (
                        <div key={label}>
                            <dt>{label}</dt>
                            <dd>{value}</dd>
                        </div>
                    ))}
                </dl>
                <button
                    type="button"
                    className="att-sidebar-logout"
                    onClick={() => (window.location.href = "/login")}
                >
                    Đăng xuất
                </button>
            </div>
        </aside>
    );
};

export default Sidebar;
