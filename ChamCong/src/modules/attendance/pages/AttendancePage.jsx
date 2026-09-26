import { useState, useEffect, useCallback } from "react";
import { getAuth } from "../../../services/auth/auth";
import ProfileCard from "../components/ProfileCard";
import AttendanceCard from "../components/AttendanceCard";
import HistoryTable from "../components/HistoryTable";
import RelatedList from "../components/RelatedList";
import relatedApi from "../api/relatedApi";
import "../attendance.css";

const AttendancePage = () => {
    const auth = getAuth();
    const userId = auth?.userId;
    const employeeId = auth?.employeeId;

    const [profile, setProfile] = useState(null);
    const [history, setHistory] = useState([]);
    const [leaves, setLeaves] = useState([]);
    const [contracts, setContracts] = useState([]);
    const [salaries, setSalaries] = useState([]);
    const [insurance, setInsurance] = useState([]);
    const [accounts, setAccounts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    // Bản ghi chấm công của HÔM NAY (để card chấm công + lịch sử đồng bộ)
    const todayRecord =
        history.find(
            (r) =>
                r.attendanceDate &&
                new Date(r.attendanceDate).toDateString() ===
                    new Date().toDateString()
        ) || null;

    const loadAttendance = useCallback(async () => {
        if (!employeeId) return [];
        const res = await relatedApi.attendanceByEmployee(employeeId);
        const data = res.data.data || [];
        setHistory(data);
        return data;
    }, [employeeId]);

    useEffect(() => {
        const load = async () => {
            if (!userId) {
                setError("Chưa xác định được tài khoản đang đăng nhập.");
                setLoading(false);
                return;
            }

            try {
                const [profileRes, leavesRes, contractsRes, salariesRes, insuranceRes, accountsRes] =
                    await Promise.allSettled([
                        relatedApi.employeeByUser(userId),
                        employeeId
                            ? relatedApi.leavesByEmployee(employeeId)
                            : Promise.resolve(null),
                        employeeId
                            ? relatedApi.contractsByEmployee(employeeId)
                            : Promise.resolve(null),
                        employeeId
                            ? relatedApi.salariesByEmployee(employeeId)
                            : Promise.resolve(null),
                        employeeId
                            ? relatedApi.insuranceByEmployee(employeeId)
                            : Promise.resolve(null),
                        employeeId
                            ? relatedApi.bankAccountsByEmployee(employeeId)
                            : Promise.resolve(null),
                    ]);

                setProfile(
                    profileRes.status === "fulfilled"
                        ? profileRes.value.data.data
                        : null
                );
                const data = (r) =>
                    r && r.status === "fulfilled" ? r.value.data.data : [];
                setLeaves(data(leavesRes) || []);
                setContracts(data(contractsRes) || []);
                setSalaries(data(salariesRes) || []);
                setInsurance(data(insuranceRes) || []);
                setAccounts(data(accountsRes) || []);

                if (employeeId) {
                    await loadAttendance();
                }
            } catch (err) {
                setError(
                    err.response?.data?.message ||
                        err.message ||
                        "Không thể tải dữ liệu chấm công."
                );
            } finally {
                setLoading(false);
            }
        };

        load();
    }, [userId, employeeId, loadAttendance]);

    // Sau khi chấm công: làm mới lại lịch sử (thêm/đánh dấu đúng ngày)
    const handleChecked = () => {
        loadAttendance().catch(() => {});
    };

    return (
        <div className="att-page">
            <header className="att-header">
                <div>
                    <h1>Trang cá nhân &amp; Chấm công</h1>
                    <p>Theo dõi thông tin nhân sự và hoạt động chấm công của bạn</p>
                </div>
                <button
                    type="button"
                    className="att-logout"
                    onClick={() => (window.location.href = "/login")}
                >
                    Đăng xuất
                </button>
            </header>

            {error && <div className="att-error">{error}</div>}
            {loading && <div className="att-loading">Đang tải...</div>}

            {!loading && (
                <div className="att-grid">
                    <ProfileCard profile={profile} auth={auth} />
                    <AttendanceCard
                        employeeId={employeeId}
                        record={todayRecord}
                        onChanged={handleChecked}
                    />

                    <div className="att-span-2">
                        <HistoryTable history={history} />
                    </div>

                    <RelatedList
                        title="Đơn xin nghỉ phép"
                        items={leaves}
                        empty="Chưa có đơn nghỉ phép nào."
                        to="/leave"
                        render={(x) => (
                            <div>
                                <strong>{x.leaveTypeName || "Nghỉ phép"}</strong>
                                <span>
                                    {formatDate(x.fromDate)} – {formatDate(x.toDate)} · {statusLabel(x.status)}
                                </span>
                            </div>
                        )}
                    />
                    <RelatedList
                        title="Hợp đồng lao động"
                        items={contracts}
                        empty="Chưa có hợp đồng nào."
                        to="/contracts"
                        render={(x) => (
                            <div>
                                <strong>{x.contractNumber}</strong>
                                <span>
                                    {contractTypeLabel(x.contractType)} · {formatDate(x.startDate)}
                                    {x.endDate ? ` – ${formatDate(x.endDate)}` : ""}
                                </span>
                            </div>
                        )}
                    />
                    <RelatedList
                        title="Bảng lương gần đây"
                        items={salaries}
                        empty="Chưa có dữ liệu lương."
                        to="/salary"
                        render={(x) => (
                            <div>
                                <strong>{formatMoney(x.basicSalary)}</strong>
                                <span>
                                    {paymentTypeLabel(x.paymentType)} · hiệu lực từ {formatDate(x.effectiveFrom)}
                                </span>
                            </div>
                        )}
                    />
                    <RelatedList
                        title="Bảo hiểm &amp; thuế"
                        items={insurance}
                        empty="Chưa có thông tin bảo hiểm."
                        to="/insurance"
                        render={(x) => (
                            <div>
                                <strong>{x.socialInsuranceNumber || "Chưa có số BHXH"}</strong>
                                <span>{insuranceStatusLabel(x.status)}</span>
                            </div>
                        )}
                    />
                    <RelatedList
                        title="Tài khoản ngân hàng"
                        items={accounts}
                        empty="Chưa có tài khoản ngân hàng."
                        to="/bank-accounts"
                        render={(x) => (
                            <div>
                                <strong>{x.accountNumber}</strong>
                                <span>
                                    {x.bankName || ""} {x.isPrimary ? "· chính" : ""}
                                </span>
                            </div>
                        )}
                    />
                </div>
            )}
        </div>
    );
};

// ===== Tiện ích định dạng (an toàn khi giá trị null) =====
const formatDate = (d) =>
    d ? new Date(d).toLocaleDateString("vi-VN") : "—";

const formatMoney = (n) =>
    n == null ? "—" : Number(n).toLocaleString("vi-VN") + " ₫";

const statusLabel = (s) =>
    ({ 1: "Chờ duyệt", 2: "Đã duyệt", 3: "Từ chối", 4: "Đã hủy" })[s] || "—";

const contractTypeLabel = (t) =>
    ({
        1: "Thử việc",
        2: "Hạn định",
        3: "Không xác định",
        4: "Mùa vụ",
    })[t] || "—";

const paymentTypeLabel = (t) =>
    ({
        1: "Theo tháng",
        2: "Theo ngày",
        3: "Theo giờ",
        4: "Theo sản phẩm",
    })[t] || "—";

const insuranceStatusLabel = (s) =>
    ({ 1: "Đang đóng", 2: "Tạm dừng", 3: "Đã chốt sổ" })[s] || "—";

export default AttendancePage;
