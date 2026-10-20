import ResponsiveTable from "../../../components/common/ResponsiveTable.jsx";
import { useState, useEffect, useCallback, useMemo } from "react";
import AdminAppLayout from "../layout/AdminAppLayout";
import adminApi from "../api/adminApi";
import { translate, useLanguage } from "../../../services/i18n/LanguageProvider";
import "../admin.css";
import "./roles.css";

const AVA_COLORS = ["#2f6df6", "#16a085", "#e67e22", "#8e44ad", "#c0392b", "#1565c0", "#00838f"];

// Vai trò hệ thống (đã seed): không cho xóa để tránh mất phân quyền (Admin...).
// UI ẩn nút Xóa; phía API cũng chặn (RoleService) -> 2 lớp bảo vệ.
// Khi sửa, TÊN của vai trò hệ thống bị khóa (chỉ được sửa mô tả) để tránh
// làm vỡ chuỗi phân quyền Authorize(Roles="Admin,Manager,HR").
const SYSTEM_ROLES = new Set(["Admin", "Employee", "Manager", "HR"]);

// Icon SVG dùng chung (gọn, không phụ thuộc emoji -> không lỗi hiển thị)
const PencilIcon = () => (
    <svg className="roles-ico" viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <path d="M12 20h9" /><path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4Z" />
    </svg>
);
const TrashIcon = () => (
    <svg className="roles-ico" viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <path d="M3 6h18" /><path d="M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" /><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6" /><path d="M10 11v6M14 11v6" />
    </svg>
);
const LockIcon = () => (
    <svg className="roles-ico" viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        <rect x="5" y="11" width="14" height="9" rx="2" /><path d="M8 11V7a4 4 0 0 1 8 0v4" />
    </svg>
);

const AdminRolesPage = () => {
    const { language } = useLanguage();
    const L = useCallback((t) => translate(t, language), [language]);
    const [roles, setRoles] = useState([]);
    const [users, setUsers] = useState([]);
    const [employees, setEmployees] = useState([]);
    const [userRolesMap, setUserRolesMap] = useState({});
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [showNew, setShowNew] = useState(false);
    const [form, setForm] = useState({ name: "", description: "" });
    const [viewUser, setViewUser] = useState(null);
    const [userFilter, setUserFilter] = useState("");

    // === Modal Sửa role + modal xác nhận Xóa (thay window.confirm) ===
    const [editRole, setEditRole] = useState(null);
    const [editForm, setEditForm] = useState({ name: "", description: "" });
    const [saveBusy, setSaveBusy] = useState(false);
    const [deleteConfirm, setDeleteConfirm] = useState(null);
    const [deleteBusy, setDeleteBusy] = useState(false);

    // user (id, userName) + employee info hợp nhất
    const userRows = useMemo(() => {
        const rows = [];
        const seen = new Set();
        users.forEach((u) => {
            if (seen.has(u.id)) return;
            seen.add(u.id);
            const emp = employees.find((x) => x.userId === u.id) || null;
            rows.push({
                ...u,
                employeeCode: emp?.employeeCode,
                fullName: emp?.fullName || u.userName,
                departmentName: emp?.departmentName,
                positionName: emp?.positionName,
            });
        });
        return rows.sort((a, b) => String(a.employeeCode || "zzz").localeCompare(String(b.employeeCode || "zzz"), "vi"));
    }, [users, employees]);

    const load = useCallback(async () => {
        setLoading(true);
        setError("");
        try {
            const [r, u, e] = await Promise.all([
                adminApi.roles(),
                adminApi.users(),
                adminApi.employees(),
            ]);
            const roleList = r.data.data?.items || [];
            const userList = u.data.data?.items || [];
            const empList = e.data.data?.items || [];
            setRoles(roleList);
            setUsers(userList);
            setEmployees(empList);
            // Lấy roles của từng user
            const map = {};
            const validUsers = userList.filter((user) => empList.find((x) => x.userId === user.id));
            const results = await Promise.all(
                validUsers.slice(0, 200).map(async (user) => {
                    try {
                        const res = await adminApi.userRoles(user.id);
                        return [user.id, res.data.data || []];
                    } catch {
                        return [user.id, []];
                    }
                })
            );
            results.forEach(([id, list]) => {
                map[id] = list;
            });
            setUserRolesMap(map);
        } catch (err) {
            setError(err.response?.data?.message || err.message || L("Không tải được dữ liệu phân quyền."));
        } finally {
            setLoading(false);
        }
    }, [L]);

    useEffect(() => { load(); }, [load]);

    // === Role CRUD ===
    const createRole = async () => {
        if (!form.name.trim()) return;
        try {
            await adminApi.createRole({ name: form.name.trim(), description: form.description.trim() });
            setShowNew(false);
            setForm({ name: "", description: "" });
            load();
        } catch (err) {
            setError(err.response?.data?.message || err.message);
        }
    };

    // Mở modal Sửa cho 1 vai trò.
    // Vai trò hệ thống: khóa trường Tên (chỉ sửa được Mô tả) để không vỡ Authorize.
    const openEdit = (role) => {
        setEditRole(role);
        setEditForm({ name: role.name, description: role.description || "" });
        setError("");
    };
    const closeEdit = () => { setEditRole(null); setEditForm({ name: "", description: "" }); setSaveBusy(false); };

    const saveRole = async () => {
        if (!editRole) return;
        const name = editRole.name; // Tên luôn giữ nguyên giá trị an toàn (trường bị khóa sẽ trùng role.name)
        if (!name.trim() || !editForm.name.trim()) return;
        setSaveBusy(true);
        try {
            await adminApi.updateRole({
                id: editRole.id,
                name: (SYSTEM_ROLES.has(editRole.name) ? name : editForm.name.trim()),
                description: editForm.description.trim(),
            });
            closeEdit();
            load();
        } catch (err) {
            setSaveBusy(false);
            setError(err.response?.data?.message || err.message);
            setEditRole(null);
        }
    };

    // Xác nhận xóa qua modal (thay window.confirm). Giữ guard hệ thống ở tầng UI.
    const confirmDelete = async () => {
        if (!deleteConfirm) return;
        const role = deleteConfirm;
        if (SYSTEM_ROLES.has(role.name)) {
            setDeleteConfirm(null);
            setError(L("Vai trò hệ thống (Admin, Employee, Manager, HR) không thể xóa."));
            return;
        }
        setDeleteBusy(true);
        try {
            await adminApi.deleteRole(role.id);
            setUserRolesMap((prev) => {
                const next = {};
                Object.entries(prev).forEach(([uid, list]) => {
                    next[uid] = list.filter((n) => n !== role.name);
                });
                return next;
            });
            setDeleteConfirm(null);
            setDeleteBusy(false);
            load();
        } catch (err) {
            setDeleteBusy(false);
            setDeleteConfirm(null);
            setError(err.response?.data?.message || err.message);
        }
    };

    // === Gán / gỡ role (matrix toggle) ===
    const addRoleToUser = async (user, role) => {
        try {
            await adminApi.addRoleToUser(user.id, role.id);
            setUserRolesMap((prev) => ({ ...prev, [user.id]: [...new Set([...(prev[user.id] || []), role.name])] }));
        } catch (err) {
            setError(err.response?.data?.message || err.message);
        }
    };

    const removeRoleFromUser = async (user, role) => {
        if (!window.confirm(`${L("Gỡ khỏi vai trò")} ${role.name}? ${user.fullName} (${user.employeeCode || user.userName})`)) return;
        try {
            await adminApi.removeRoleFromUser(user.id, role.id);
            setUserRolesMap((prev) => ({ ...prev, [user.id]: (prev[user.id] || []).filter((n) => n !== role.name) }));
        } catch (err) {
            setError(err.response?.data?.message || err.message);
        }
    };

    const isAssigned = (uid, roleName) => (userRolesMap[uid] || []).includes(roleName);
    const toggleRole = (user, role) => (isAssigned(user.id, role.name) ? removeRoleFromUser(user, role) : addRoleToUser(user, role));

    // Số thành viên của từng role
    const roleCount = useMemo(() => {
        const m = {};
        roles.forEach((r) => {
            m[r.name] = userRows.filter((u) => (userRolesMap[u.id] || []).includes(r.name)).length;
        });
        return m;
    }, [roles, userRows, userRolesMap]);

    // Lọc người dùng cho ma trận
    const filteredUsers = useMemo(() => {
        const q = userFilter.trim().toLocaleLowerCase("vi");
        if (!q) return userRows;
        return userRows.filter((u) =>
            `${u.employeeCode || ""} ${u.fullName || ""} ${u.email || ""} ${u.departmentName || ""} ${u.positionName || ""}`
                .toLocaleLowerCase("vi")
                .includes(q)
        );
    }, [userRows, userFilter]);

    const orphans = useMemo(
        () => userRows.filter((u) => (userRolesMap[u.id] || []).length === 0),
        [userRows, userRolesMap]
    );

    const kpi = useMemo(
        () => ({
            totalRoles: roles.length,
            totalUsers: userRows.length,
            assigned: userRows.reduce((sum, u) => sum + ((userRolesMap[u.id] || []).length ? 1 : 0), 0),
        }),
        [roles, userRows, userRolesMap]
    );

    const avaColor = (name = "") => {
        let h = 0;
        for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) | 0;
        return AVA_COLORS[Math.abs(h) % AVA_COLORS.length];
    };

    return (
        <AdminAppLayout title={L("Phân quyền")} subtitle={L("Quản lý vai trò (role) và người dùng trong từng vai trò")}>
            <div className="att-content">
                {error && (
                    <div className="att-error" role="alert">
                        {error}
                        <button onClick={() => setError("")}>×</button>
                    </div>
                )}
                {loading ? (
                    <div className="att-loading">{L("Đang tải...")}</div>
                ) : (
                    <>
                        {/* ===== Hero + KPI ===== */}
                        <div className="roles-hero">
                            <div className="roles-hero-left">
                                <span className="roles-hero-ico">🛡️</span>
                                <div>
                                    <h2>{L("Phân quyền tài khoản")}</h2>
                                    <p>{L("Tạo, sửa vai trò, gán / gỡ người dùng và theo dõi phạm vi truy cập trong hệ thống.")}</p>
                                </div>
                            </div>
                            <div className="roles-kpi-row">
                                <div className="admin-kpi roles-kpi roles-kpi--blue"><span className="roles-kpi-ico">🛡️</span><div><span className="admin-kpi-label">{L("Vai trò")}</span><strong>{kpi.totalRoles}</strong></div></div>
                                <div className="admin-kpi roles-kpi roles-kpi--teal"><span className="roles-kpi-ico">👤</span><div><span className="admin-kpi-label">{L("Người dùng")}</span><strong>{kpi.totalUsers}</strong></div></div>
                                <div className="admin-kpi roles-kpi roles-kpi--green"><span className="roles-kpi-ico">✅</span><div><span className="admin-kpi-label">{L("Đã có vai trò")}</span><strong>{kpi.assigned}</strong></div></div>
                            </div>
                        </div>

                        {/* ===== BẢNG 1: QUẢN LÝ VAI TRÒ ===== */}
                        <section className="att-card roles-section">
                            <div className="roles-section-head">
                                <h3>🛡️ {L("Danh sách vai trò")}</h3>
                                <button type="button" className="roles-add-btn" onClick={() => setShowNew((v) => !v)}>
                                    {showNew ? L("Đóng") : `+ ${L("Thêm vai trò")}`}
                                </button>
                            </div>

                            {showNew && (
                                <div className="roles-new-form">
                                    <input type="text" placeholder={L("Tên vai trò (VD: HR, Manager...)")} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} autoFocus />
                                    <input type="text" placeholder={L("Mô tả (không bắt buộc)")} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
                                    <button type="button" className="admin-link-btn" onClick={createRole}>{L("Lưu")}</button>
                                    <button type="button" className="admin-link-btn admin-link-btn--danger" onClick={() => { setShowNew(false); setForm({ name: "", description: "" }); }}>{L("Hủy")}</button>
                                </div>
                            )}

                            {roles.length === 0 ? (
                                <div className="roles-empty">{L("Chưa có vai trò nào. Hãy tạo vai trò đầu tiên.")}</div>
                            ) : (
                                <div className="att-table-wrap">
                                    <ResponsiveTable className="att-table roles-roles-table">
                                        <thead>
                                            <tr>
                                                <th>{L("Vai trò")}</th>
                                                <th>{L("Mô tả")}</th>
                                                <th className="roles-th-center">{L("Thành viên")}</th>
                                                <th className="roles-th-act">{L("Thao tác")}</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {roles.map((role) => {
                                                const isSystem = SYSTEM_ROLES.has(role.name);
                                                return (
                                                <tr key={role.id}>
                                                    <td>
                                                        <span className="roles-role-ava roles-role-ava--sm" style={{ background: avaColor(role.name) }}>
                                                            {(role.name || "?").slice(0, 1).toUpperCase()}
                                                        </span>
                                                        <strong>{role.name}</strong>
                                                    </td>
                                                    <td>{role.description || <span className="att-muted">—</span>}</td>
                                                    <td className="roles-th-center">
                                                        <span className="roles-member-count" style={{ background: avaColor(role.name) + "1a", color: avaColor(role.name) }}>
                                                            {roleCount[role.name] || 0} {L("người")}
                                                        </span>
                                                    </td>
                                                    <td className="roles-th-act">
                                                        <div className="roles-role-actions">
                                                            <button type="button" className="roles-role-edit" onClick={() => openEdit(role)} title={L("Sửa vai trò")}>
                                                                <PencilIcon />
                                                                {L("Sửa")}
                                                            </button>
                                                            {isSystem ? (
                                                                <span className="roles-sys-badge" title={L("Vai trò hệ thống không thể xóa (tên được khóa, chỉ sửa mô tả)")}>
                                                                    <LockIcon />
                                                                    {L("Hệ thống")}
                                                                </span>
                                                            ) : (
                                                                <button type="button" className="roles-role-delete" onClick={() => setDeleteConfirm(role)} title={L("Xóa vai trò")}>
                                                                    <TrashIcon />
                                                                    {L("Xóa")}
                                                                </button>
                                                            )}
                                                        </div>
                                                    </td>
                                                </tr>
                                                );
                                            })}
                                        </tbody>
                                    </ResponsiveTable>
                                </div>
                            )}
                        </section>

                        {/* ===== BẢNG 2: MA TRẬN NGƯỜI DÙNG × VAI TRÒ ===== */}
                        {roles.length > 0 && (
                            <section className="att-card roles-section">
                                <div className="roles-section-head">
                                    <h3>👤 {L("Gán vai trò cho người dùng")}</h3>
                                    <div className="roles-search">
                                        <span aria-hidden="true">⌕</span>
                                        <input
                                            type="search"
                                            placeholder={L("Tìm mã, tên, email, phòng ban...")}
                                            value={userFilter}
                                            onChange={(e) => setUserFilter(e.target.value)}
                                            aria-label={L("Tìm người dùng")}
                                        />
                                    </div>
                                </div>
                                <p className="att-muted roles-matrix-hint">
                                    {L("Bấm + để gán, bấm ✓ để gỡ vai trò. Dòng đậm = đang có vai trò.")}
                                </p>
                                <div className="att-table-wrap roles-matrix-wrap">
                                    <ResponsiveTable className="att-table roles-matrix" mode="scroll">
                                        <thead>
                                            <tr>
                                                <th className="roles-mx-user">{L("Người dùng")}</th>
                                                {roles.map((r) => (
                                                    <th key={r.id} className="roles-mx-role">
                                                        <span className="roles-mx-role-name">{r.name}</span>
                                                        <small>{roleCount[r.name] || 0} {L("người")}</small>
                                                    </th>
                                                ))}
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {filteredUsers.length === 0 ? (
                                                <tr><td colSpan={roles.length + 1} className="att-muted roles-member-empty">{L("Không tìm thấy người dùng phù hợp.")}</td></tr>
                                            ) : (
                                                filteredUsers.map((u) => {
                                                    const hasAny = (userRolesMap[u.id] || []).length > 0;
                                                    return (
                                                        <tr key={u.id} className={hasAny ? "roles-mx-hasrole" : ""}>
                                                            <td className="roles-mx-user">
                                                                <button type="button" className="roles-member-open" onClick={() => setViewUser(u)} aria-label={`${L("Xem thông tin")} ${u.fullName}`}>
                                                                <span className="roles-member-ava roles-member-ava--sm" style={{ background: avaColor(u.fullName || u.userName) }}>
                                                                    {(u.fullName || u.userName || "?").slice(0, 1).toUpperCase()}
                                                                </span>
                                                                <span className="roles-mx-user-text">
                                                                    <strong>{u.fullName}</strong>
                                                                    <small>{u.employeeCode || "—"}{u.departmentName ? ` · ${u.departmentName}` : ""}</small>
                                                                </span>
                                                                </button>
                                                            </td>
                                                            {roles.map((r) => {
                                                                const on = isAssigned(u.id, r.name);
                                                                return (
                                                                    <td key={r.id} className="roles-mx-role">
                                                                        <button
                                                                            type="button"
                                                                            className={`roles-mx-toggle${on ? " on" : ""}`}
                                                                            onClick={(e) => { e.stopPropagation(); toggleRole(u, r); }}
                                                                            title={on ? L("Gỡ khỏi vai trò") : L("Gán vào vai trò")}
                                                                            aria-label={`${on ? L("Gỡ khỏi vai trò") : L("Gán vào vai trò")} ${r.name}: ${u.fullName}`}
                                                                            aria-pressed={on}
                                                                        >
                                                                            {on ? "✓" : "+"}
                                                                        </button>
                                                                    </td>
                                                                );
                                                            })}
                                                        </tr>
                                                    );
                                                })
                                            )}
                                        </tbody>
                                    </ResponsiveTable>
                                </div>
                            </section>
                        )}

                        {/* ===== Người dùng chưa có vai trò ===== */}
                        {orphans.length > 0 && (
                            <section className="att-card roles-section">
                                <div className="roles-section-head">
                                    <h3>⚠️ {L("Người dùng chưa có vai trò")}</h3>
                                    <span className="roles-orphan-count">{orphans.length} {L("người")}</span>
                                </div>
                                <div className="roles-orphan-list">
                                    {orphans.map((u) => (
                                        <span key={u.id} className="roles-orphan-chip">
                                            <span className="roles-member-ava roles-member-ava--xs" style={{ background: avaColor(u.fullName || u.userName) }}>
                                                {(u.fullName || "?").slice(0, 1).toUpperCase()}
                                            </span>
                                            {u.employeeCode || "—"} · {u.fullName}
                                        </span>
                                    ))}
                                </div>
                            </section>
                        )}

                        {/* ===== Modal Sửa vai trò ===== */}
                        {editRole && (
                            <div className="att-guide-overlay roles-overlay" onMouseDown={(e) => e.target === e.currentTarget && closeEdit()}>
                                <section className="att-detail-modal roles-modal roles-edit-modal" role="dialog" aria-modal="true">
                                    <header className="att-detail-modal-head">
                                        <div>
                                            <h2>{L("Sửa vai trò")} · {editRole.name}</h2>
                                            <p>{L("Chỉnh sửa tên và mô tả vai trò.")}</p>
                                        </div>
                                        <button type="button" onClick={closeEdit} aria-label={L("Đóng")}>×</button>
                                    </header>
                                    <div className="roles-edit-form">
                                        <label className="roles-edit-field">
                                            <span>{L("Tên vai trò")}</span>
                                            <input
                                                type="text"
                                                value={editForm.name}
                                                disabled={SYSTEM_ROLES.has(editRole.name)}
                                                onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
                                                placeholder={L("VD: HR, Manager...")}
                                            />
                                            {SYSTEM_ROLES.has(editRole.name) && (
                                                <small className="roles-edit-locknote">
                                                    <LockIcon /> {L("Vai trò hệ thống: tên được khóa, chỉ có thể sửa mô tả.")}
                                                </small>
                                            )}
                                        </label>
                                        <label className="roles-edit-field">
                                            <span>{L("Mô tả (không bắt buộc)")}</span>
                                            <textarea
                                                rows={2}
                                                value={editForm.description}
                                                onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                                                placeholder={L("VD: Kế toán, Sale, Marketing...")}
                                            />
                                        </label>
                                    </div>
                                    <footer>
                                        <button type="button" className="admin-link-btn admin-link-btn--danger" onClick={closeEdit} disabled={saveBusy}>{L("Hủy")}</button>
                                        <button type="button" className="admin-link-btn" onClick={saveRole} disabled={saveBusy || !editForm.name.trim()}>
                                            {saveBusy ? L("Đang lưu...") : L("Lưu thay đổi")}
                                        </button>
                                    </footer>
                                </section>
                            </div>
                        )}

                        {/* ===== Modal xác nhận Xóa vai trò (thay window.confirm) ===== */}
                        {deleteConfirm && (
                            <div className="att-guide-overlay roles-overlay" onMouseDown={(e) => e.target === e.currentTarget && !deleteBusy && setDeleteConfirm(null)}>
                                <section className="att-detail-modal roles-modal roles-confirm-modal" role="dialog" aria-modal="true">
                                    <header className="att-detail-modal-head">
                                        <div>
                                            <h2>{L("Xóa vai trò")}</h2>
                                            <p>{L("Hành động này sẽ gỡ vai trò khỏi tất cả người dùng.")}</p>
                                        </div>
                                        <button type="button" onClick={() => !deleteBusy && setDeleteConfirm(null)} aria-label={L("Đóng")}>×</button>
                                    </header>
                                    <div className="roles-confirm-body">
                                        <p>{L("Vai trò")} <strong>{deleteConfirm.name}</strong>{deleteConfirm.description ? <span className="roles-confirm-desc"> — {deleteConfirm.description}</span> : null} {L("sẽ bị xóa mềm và mất liên kết với")} <strong>{roleCount[deleteConfirm.name] || 0}</strong> {L("người đang dùng.")}</p>
                                        <p className="roles-confirm-q">{L("Bạn có chắc chắn muốn xóa không?")}</p>
                                    </div>
                                    <footer>
                                        <button type="button" className="admin-link-btn admin-link-btn--danger" onClick={() => setDeleteConfirm(null)} disabled={deleteBusy}>{L("Hủy")}</button>
                                        <button type="button" className="roles-confirm-btn" onClick={confirmDelete} disabled={deleteBusy}>
                                            <TrashIcon />
                                            {deleteBusy ? L("Đang xóa...") : L("Xóa")}
                                        </button>
                                    </footer>
                                </section>
                            </div>
                        )}

                        {/* ===== Modal xem thông tin người dùng ===== */}
                        {viewUser && (
                            <div className="att-guide-overlay roles-overlay" onMouseDown={(e) => e.target === e.currentTarget && setViewUser(null)}>
                                <section className="att-detail-modal roles-modal">
                                    <header className="att-detail-modal-head">
                                        <div>
                                            <h2>{L("Thông tin người dùng")}</h2>
                                            <p>{viewUser.employeeCode || "—"} · {viewUser.fullName}</p>
                                        </div>
                                        <button type="button" onClick={() => setViewUser(null)} aria-label={L("Đóng")}>×</button>
                                    </header>
                                    <div className="roles-profile-grid">
                                        <span>{L("Mã nhân viên")}<strong>{viewUser.employeeCode || "—"}</strong></span>
                                        <span>{L("Họ tên")}<strong>{viewUser.fullName}</strong></span>
                                        <span>{L("Email")}<strong>{viewUser.email || "—"}</strong></span>
                                        <span>{L("Điện thoại")}<strong>{viewUser.phoneNumber || "—"}</strong></span>
                                        <span>{L("Phòng ban")}<strong>{viewUser.departmentName || "—"}</strong></span>
                                        <span>{L("Chức vụ")}<strong>{viewUser.positionName || "—"}</strong></span>
                                        <span>{L("Tài khoản")}<strong>{viewUser.userName}</strong></span>
                                        <span>{L("Vai trò")}<strong>{(userRolesMap[viewUser.id] || []).join(", ") || "—"}</strong></span>
                                    </div>
                                    <footer>
                                        <button type="button" className="admin-link-btn" onClick={() => setViewUser(null)}>{L("Đóng")}</button>
                                    </footer>
                                </section>
                            </div>
                        )}
                    </>
                )}
            </div>
        </AdminAppLayout>
    );
};

export default AdminRolesPage;
