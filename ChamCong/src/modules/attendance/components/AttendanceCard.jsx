import { useState } from "react";
import relatedApi from "../api/relatedApi";
import {
    getVnNowHour,
    getAdministrativeShift,
    getAdministrativeShiftWindow,
    formatVnTime,
} from "../../../utils/vnTime";

const AttendanceCard = ({ employeeId, record, onChanged }) => {
    const [busy, setBusy] = useState(false);
    const [done, setDone] = useState("");
    const [error, setError] = useState("");

    // Ca hành chính theo giờ Việt Nam: 7h -> sáng, 12h -> chiều, 18h -> tối
    const adminShift = getAdministrativeShift(getVnNowHour());

    // Giờ thực tế (chấm giờ nào, về giờ đó)
    const checkInTime = record?.checkInTime
        ? formatVnTime(record.checkInTime)
        : "";
    const checkOutTime = record?.checkOutTime
        ? formatVnTime(record.checkOutTime)
        : "";

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
                <span className="att-shift-name">
                    Ca hành chính: {adminShift}
                </span>
                <span className="att-shift-window">
                    {getAdministrativeShiftWindow(getVnNowHour())}
                </span>
            </div>

            <div className="att-checkin-photos">
                <div className="att-photo-box">
                    <span className="att-photo-label">GIỜ VÀO CA</span>
                    {checkInTime ? checkInTime : "Chưa chấm"}
                </div>
                <div className="att-photo-box">
                    <span className="att-photo-label">GIỜ RA CA</span>
                    {checkOutTime ? checkOutTime : "Chưa chấm"}
                </div>
            </div>

            <div className="att-checkin-actions">
                <button
                    type="button"
                    onClick={() => check(1)}
                    disabled={busy || !!checkInTime}
                >
                    Vào ca
                </button>
                <button
                    type="button"
                    onClick={() => check(2)}
                    disabled={busy || !!checkOutTime}
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
