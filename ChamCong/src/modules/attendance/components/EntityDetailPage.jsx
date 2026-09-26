import { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { getAuth } from "../../../services/auth/auth";
import relatedApi from "../api/relatedApi";
import "../attendance.css";

// Trang chi tiết chung cho các entity của nhân viên đang đăng nhập.
// Reuse style của module attendance để giữ giao diện nhất quán.
const FETCHERS = {
    leave: (id) => relatedApi.leavesByEmployee(id),
    contract: (id) => relatedApi.contractsByEmployee(id),
    salary: (id) => relatedApi.salariesByEmployee(id),
    insurance: (id) => relatedApi.insuranceByEmployee(id),
    bank: (id) => relatedApi.bankAccountsByEmployee(id),
};

const EntityDetailPage = ({ kind, title, subtitle, columns, emptyMessage }) => {
    const auth = getAuth();
    const employeeId = auth?.employeeId;

    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        const load = async () => {
            try {
                if (!employeeId) {
                    setError("Chưa xác định được nhân viên đang đăng nhập.");
                    setLoading(false);
                    return;
                }
                const res = await FETCHERS[kind](employeeId);
                setItems(res.data.data || []);
            } catch (err) {
                setError(
                    err.response?.data?.message ||
                        err.message ||
                        "Không thể tải dữ liệu."
                );
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [kind, employeeId]);

    return (
        <div className="att-page">
            <header className="att-header">
                <div>
                    <h1>{title}</h1>
                    <p>{subtitle}</p>
                </div>
                <Link to="/attendance" className="att-logout">
                    &larr; Chấm công
                </Link>
            </header>

            <div className="att-content">
                {error && <div className="att-error">{error}</div>}

                {loading ? (
                    <div className="att-loading">Đang tải...</div>
                ) : items.length === 0 ? (
                    <div className="att-card">
                        <p className="att-muted">{emptyMessage}</p>
                    </div>
                ) : (
                    <div className="att-card">
                        <div className="att-table-wrap">
                            <table className="att-table">
                                <thead>
                                    <tr>
                                        {columns.map((c) => (
                                            <th key={c.key}>{c.label}</th>
                                        ))}
                                    </tr>
                                </thead>
                                <tbody>
                                    {items.map((row) => (
                                        <tr key={row.id}>
                                            {columns.map((c) => (
                                                <td key={c.key}>
                                                    {c.render
                                                        ? c.render(row)
                                                        : row[c.key] ?? "—"}
                                                </td>
                                            ))}
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default EntityDetailPage;
