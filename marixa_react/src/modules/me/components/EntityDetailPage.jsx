import { useState, useEffect } from "react";
import { getAuth } from "../../../services/auth/auth";
import AppLayout from "../../../components/layout/AppLayout";
import relatedApi from "../api/relatedApi";
import "../attendance.css";

const FETCHERS = {
    contract: (id) => relatedApi.contractsByEmployee(id),
    salary: (id) => relatedApi.salariesByEmployee(id),
    insurance: (id) => relatedApi.insuranceByEmployee(id),
    bank: (id) => relatedApi.bankAccountsByEmployee(id),
};

const ICONS = {
    contract: "📋",
    salary: "💰",
    insurance: "🛡️",
    bank: "🏦",
};

const KPI_LABEL = {
    contract: "hợp đồng",
    salary: "bản ghi lương",
    insurance: "chế độ",
    bank: "tài khoản",
};

const EntityDetailPage = ({ kind, columns, emptyMessage, subtitle }) => {
    const auth = getAuth();
    const userId = auth?.userId;
    const employeeId = auth?.employeeId;

    const [items, setItems] = useState([]);
    const [profile, setProfile] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [reloadVersion, setReloadVersion] = useState(0);

    useEffect(() => {
        const load = async () => {
            setLoading(true);
            setError("");
            try {
                if (!employeeId) {
                    setError("Chưa xác định được nhân viên đang đăng nhập.");
                    setLoading(false);
                    return;
                }
                const [res, profileRes] = await Promise.allSettled([
                    FETCHERS[kind](employeeId),
                    userId ? relatedApi.employeeByUser(userId) : Promise.resolve(null),
                ]);
                if (profileRes.status === "fulfilled" && profileRes.value) {
                    setProfile(profileRes.value.data.data || null);
                }
                if (res.status === "rejected") throw res.reason;
                setItems(res.value.data.data || []);
            } catch (err) {
                setError(err.response?.data?.message || err.message || "Không thể tải dữ liệu.");
            } finally {
                setLoading(false);
            }
        };
        load();
    }, [kind, userId, employeeId, reloadVersion]);

    return (
        <AppLayout profile={profile}>
            <div className="entd-page">
                <div className="me-record-summary">
                    <span className="me-record-icon" aria-hidden="true">{ICONS[kind]}</span>
                    <div><strong>{loading ? "Đang tải…" : error ? "Chưa tải được dữ liệu" : `${items.length} ${KPI_LABEL[kind]}`}</strong>
                        <p>{subtitle}</p></div>
                </div>

                {error && <div className="att-error" role="alert">{error} <button type="button" className="me-action" onClick={() => setReloadVersion((value) => value + 1)}>Thử lại</button></div>}

                {loading ? (
                    <div className="att-loading">Đang tải...</div>
                ) : error ? null : items.length === 0 ? (
                    <div className="entd-empty">
                        <span className="entd-empty-icon">{ICONS[kind] || "📄"}</span>
                        <p>{emptyMessage || "Chưa có dữ liệu."}</p>
                    </div>
                ) : (
                    <div className="me-records">
                        {items.map((row, index) => <article className="me-record" key={row.id} aria-label={`${KPI_LABEL[kind]} ${index + 1}`}>
                            <header><span>Hồ sơ {String(index + 1).padStart(2, "0")}</span>
                                <span className="att-badge info">Thông tin cá nhân</span></header>
                            <dl>{columns.map((c) => <div key={c.key}>
                                <dt>{c.label}</dt><dd>{c.render ? c.render(row) : row[c.key] || "—"}</dd>
                            </div>)}</dl>
                        </article>)}
                    </div>
                )}
                {!loading && !error && <p className="me-footnote">Thông tin do bộ phận Nhân sự quản lý. Nếu có sai lệch, vui lòng liên hệ Nhân sự để đối chiếu và cập nhật.</p>}
            </div>
        </AppLayout>
    );
};

export default EntityDetailPage;
