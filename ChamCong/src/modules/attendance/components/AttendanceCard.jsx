import { useState } from "react";
import axiosClient from "../../../services/api/axiosClient";

const AttendanceCard = ({ today: record, shifts }) => {
    const [photo, setPhoto] = useState(null);
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
        if (!record?.id) {
            setError("Chưa có phiên chấm công hôm nay. Vui lòng liên hệ quản trị.");
            return;
        }
        setBusy(true);
        setError("");
        try {
            await axiosClient.post("/AttendanceLog/create", {
                attendanceId: record.id,
                logTime: new Date().toISOString(),
                type,
                method: 4, // Phone
                photoUrl: photo ? "pending-upload" : undefined,
            });
            setDone(type === 1 ? "Đã ghi nhận VÀO CA." : "Đã ghi nhận RA CA.");
        } catch (err) {
            setError(err.response?.data?.message || err.message || "Chấm công thất bại.");
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
                    {record?.checkInPhoto ? "✓ Đã chụp" : "Chưa"}
                </div>
                <div className="att-photo-box">
                    <span className="att-photo-label">RA CA</span>
                    {record?.checkOutPhoto ? "✓ Đã chụp" : "Chưa"}
                </div>
            </div>

            <label className="att-photo">
                <input
                    type="file"
                    accept="image/*"
                    capture="environment"
                    onChange={(e) => setPhoto(e.target.files?.[0] || null)}
                />
                <span>{photo ? photo.name : "Chọn ảnh (chụp nhanh)"}</span>
            </label>

            <div className="att-checkin-actions">
                <button type="button" onClick={() => check(1)} disabled={busy || !!record?.checkInPhoto}>
                    Vào ca
                </button>
                <button type="button" onClick={() => check(2)} disabled={busy || !!record?.checkOutPhoto}>
                    Ra ca
                </button>
            </div>

            {done && <p className="att-checkin-done">{done}</p>}
            {error && <p className="att-checkin-error">{error}</p>}
        </section>
    );
};

export default AttendanceCard;
