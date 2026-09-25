import { useState } from "react";
import relatedApi from "../api/relatedApi";

const AttendanceCard = ({ employeeId, record, shifts, onChanged }) => {
    const [busy, setBusy] = useState(false);
    const [done, setDone] = useState("");
    const [error, setError] = useState("");

    const activeShift =
        (shifts || []).find(
            (s) =>
                s.shiftName &&
                (!s.effectiveFrom || new Date(s.effectiveFrom) <= new Date()) &&
                (!s.effectiveTo || new Date(s.effectiveTo) >= new Date())
        ) || null;

    const check = async (type) => {
        if (!employeeId) {
            setError("Chưa xác định được nhân viên. Vui lòng đăng nhập lại.");
            return;
        }
        setBusy(true);
        setError("");
        setDone("");

        try {
            const response = await relatedApi.checkin(employeeId, type);
            const data = response.data.data;

            setDone(
                data?.alreadyRecorded
                    ? data.message || "Đã có lần chấm cùng loại gần đây."
                    : data?.message || "Đã ghi nhận chấm công."
            );
            onChanged?.(data?.attendance || null);
        } catch (err) {
            setError(
                err.response?.data?.message ||
                    err.message ||
                    "Chấm công thất bại."
            );
        } finally {
            setBusy(false);
        }
    };

    return (
        <section className="att-card att-checkin">
            <h2>Chấm công hôm nay</h2>

            <div className="att-checkin-shift">
                {activeShift ? (
                    <span className="att-shift-name">
                        Ca: {activeShift.shiftName}
                    </span>
                ) : (
                    <span className="att-muted">Chưa gán ca làm việc</span>
                )}
            </div>

            <div className="att-checkin-photos">
                <div className="att-photo-box">
                    <span className="att-photo-label">VÀO CA</span>
                    {record?.checkInPhoto || record?.status != null
                        ? "✓ Đã chấm"
                        : "Chưa"}
                </div>
                <div className="att-photo-box">
                    <span className="att-photo-label">RA CA</span>
                    {record?.checkOutPhoto ? "✓ Đã chấm" : "Chưa"}
                </div>
            </div>

            <div className="att-checkin-actions">
                <button
                    type="button"
                    onClick={() => check(1)}
                    disabled={busy || !!record?.checkInPhoto}
                >
                    Vào ca
                </button>
                <button
                    type="button"
                    onClick={() => check(2)}
                    disabled={busy || !!record?.checkOutPhoto}
                >
                    Ra ca
                </button>
            </div>

            {done && <p className="att-checkin-done">{done}</p>}
            {error && <p className="att-checkin-error">{error}</p>}
        </section>
    );
};

export default AttendanceCard;
