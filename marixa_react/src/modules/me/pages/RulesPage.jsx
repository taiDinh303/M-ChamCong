import { useState, useEffect } from "react";
import { getAuth } from "../../../services/auth/auth";
import AppLayout from "../../../components/layout/AppLayout";
import relatedApi from "../api/relatedApi";
import { breakLabel } from "../../../utils/vnTime";
import RuleEditor from "../components/RuleEditor";
import PageBanner from "../components/PageBanner";
import "../attendance.css";

const AttendanceRulesPage = () => {
    const auth = getAuth();
    const [rules, setRules] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [profile, setProfile] = useState(null);
    const [editing, setEditing] = useState(null);
    const [reloadVersion, setReloadVersion] = useState(0);

    useEffect(() => {
        if (!auth?.userId) {
            setLoading(false);
            return;
        }
        (async () => {
            setLoading(true);
            setError("");
            try {
                const [res, profRes] = await Promise.all([
                    relatedApi.attendanceRulesActive(),
                    relatedApi.employeeByUser(auth.userId).catch(() => ({ data: { data: null } })),
                ]);
                setProfile(profRes.data?.data || null);
                setRules(res.data?.data || []);
            } catch (err) {
                setError(err.response?.data?.message || err.message || "Không tải được quy định.");
            } finally {
                setLoading(false);
            }
        })();
    }, [auth?.userId, reloadVersion]);

    // Format phút -> "Xh Ym"
    const fmtMin = (m) => {
        if (!m) return "0 phút";
        const h = Math.floor(m / 60), r = m % 60;
        return h ? `${h}h${r ? ` ${r}m` : ""}` : `${r} phút`;
    };

    const fmtHours = (h) => (h ? `${h}h` : "—");

    const timeToBadge = (t) => {
        if (!t) return "—";
        return <span className="att-badge info">{t}</span>;
    };

    const ruleCard = (r, idx) => {
        const accent = ["#1647C8", "#167347", "#00BFD0", "#9A5A00"][idx % 4];
        return (
            <article
                key={r.id}
                className="att-card rule-card"
                style={{ borderTop: `3px solid ${accent}` }}
            >
                <header className="rule-head">
                    <div className="rule-head-left">
                        <span className="rule-ava" style={{ background: accent }}>
                            {(r.code || "?").slice(0, 2).toUpperCase()}
                        </span>
                        <div>
                            <h3>{r.name}</h3>
                            <p>{r.description || "Chưa có mô tả"}</p>
                        </div>
                    </div>
                    <i className="att-badge ok">Hiệu lực</i>
                </header>

                {r.description && <p className="rule-desc">{r.description}</p>}

                <div className="rule-grid">
                    <div className="rule-tile">
                        <span>Giờ vào</span>
                        <strong>{timeToBadge(r.checkInTime)}</strong>
                    </div>
                    <div className="rule-tile">
                        <span>Giờ ra</span>
                        <strong>{timeToBadge(r.checkOutTime)}</strong>
                    </div>
                    <div className="rule-tile">
                        <span>Số giờ chuẩn</span>
                        <strong>{fmtHours(r.standardHours)}</strong>
                    </div>
                    <div className="rule-tile rule-tile--break">
                        <span>Nghỉ giữa ca</span>
                        <strong>{breakLabel(r)}</strong>
                        <small>{r.breakMinutes ? `${r.breakMinutes} phút theo cấu hình ca` : "Không trừ giờ nghỉ"}</small>
                    </div>
                    <div className="rule-tile">
                        <span>Dung sai đi trễ</span>
                        <strong>{fmtMin(r.lateGraceMinutes)}</strong>
                    </div>
                    <div className="rule-tile">
                        <span>Ngưỡng về sớm</span>
                        <strong>{fmtMin(r.earlyLeaveThresholdMinutes)}</strong>
                    </div>
                    <div className="rule-tile">
                        <span>Chụp ảnh</span>
                        <strong>
                            <i className={`att-badge ${r.photoRequired ? "ok" : "warn"}`}>
                                {r.photoRequired ? "Bắt buộc" : "Tùy chọn"}
                            </i>
                        </strong>
                    </div>
                    <div className="rule-tile">
                        <span>Định vị GPS</span>
                        <strong>
                            <i className={`att-badge ${r.gpsRequired ? "ok" : "warn"}`}>
                                {r.gpsRequired ? "Bắt buộc" : "Tùy chọn"}
                            </i>
                        </strong>
                    </div>
                </div>

                <footer className="rule-foot">
                    {auth?.roles?.includes("Admin") && <button type="button" className="wf-btn" onClick={() => setEditing(r)}>Chỉnh quy định</button>}
                    <span className="rule-code">{r.code}</span>
                    <small>Tạo ngày {new Date(r.createdTime).toLocaleDateString("vi-VN")}</small>
                </footer>
            </article>
        );
    };

    return (
        <AppLayout profile={profile}>
            {editing && <RuleEditor rule={editing} onClose={() => setEditing(null)} onSaved={async () => {
                setEditing(null);
                try { const response = await relatedApi.attendanceRulesActive(); setRules(response.data.data || []); }
                catch (err) { setError(err.message); }
            }} />}
            <div className="att-content">
                {!loading && !error && rules.length > 0 && (
                    <PageBanner
                        icon="📋"
                        accent="#00BFD0"
                        kpis={[
                            { icon: "§", label: "Quy định", value: rules.length, tone: "blue" },
                            { icon: "⏱", label: "Giờ vào", value: rules[0]?.checkInTime || "—", tone: "green" },
                            { icon: "⏰", label: "Giờ ra", value: rules[0]?.checkOutTime || "—", tone: "gold" },
                        ]}
                    />
                )}
                {error && (
                    <div className="att-error" role="alert">
                        {error}
                        <button type="button" className="me-action" onClick={() => setReloadVersion((value) => value + 1)}>Thử lại</button>
                    </div>
                )}
                {loading ? (
                    <div className="att-loading">Đang tải...</div>
                ) : error ? null : rules.length === 0 ? (
                    <div className="att-card">
                        <p className="att-muted">
                            Chưa có quy định chấm công nào được công bố. Vui lòng liên hệ quản trị viên.
                        </p>
                    </div>
                ) : (
                    <div className="rules-page">
                        <div className="rules-grid">
                            {rules.map((r, i) => ruleCard(r, i))}
                        </div>
                    </div>
                )}
            </div>
        </AppLayout>
    );
};

export default AttendanceRulesPage;
