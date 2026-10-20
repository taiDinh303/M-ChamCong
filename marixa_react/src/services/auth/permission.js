import { getAuth } from "./auth.js";
import { MODULES } from "../../constants/modules.js";
import { canonicalModulePath } from "../../constants/routes.js";

// Role "điều hành / quản trị" (nhiều module hơn nhân viên thường).
export const ADMIN_ROLES = ["Admin"];

// Lấy danh sách role của tài khoản hiện tại (từ auth đã lưu).
export const getRoles = () => getAuth()?.roles || [];

// Có quyền admin hay không.
export const hasAdminRole = () =>
    getRoles().some((r) => ADMIN_ROLES.includes(r));

// Tài khoản có 1 role nào trong danh sách không.
const hasAnyRole = (roles) =>
    Array.isArray(roles) && roles.length > 0
        ? getRoles().some((r) => roles.includes(r))
        : false;

// Module (đường dẫn) có được truy cập không với tài khoản hiện tại.
// - có "roles": phải thuộc đúng 1 role trong danh sách (ưu tiên).
// - "access: admin": chỉ role quản trị (Admin).
// - "access: user" / không khai báo: mọi tài khoản đã đăng nhập.
export const canAccessModule = (to) => {
    const path = canonicalModulePath(to);
    const m = MODULES.filter((x) => x.to === path || path.startsWith(`${x.to}/`))
        .sort((a, b) => b.to.length - a.to.length)[0];
    if (!m) return true; // đường dẫn tự do (login, hồ sơ...)
    if (m.roles) return hasAnyRole(m.roles);
    if (m.access === "admin") return hasAdminRole();
    return true;
};

// Sidebar khu chấm công chỉ chứa các module dùng hằng ngày của nhân viên.
export const visibleModules = () =>
    MODULES.filter(
        (m) =>
            m.access === "user" &&
            !m.hideInUserSidebar &&
            canAccessModule(m.to)
    );
