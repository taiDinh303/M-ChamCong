import { useState, useEffect } from "react";
import relatedApi from "../api/relatedApi";
import { formatVnTime, VN_TIMEZONE } from "../../../utils/vnTime";
import CameraCapture from "./CameraCapture";

const dayNames = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];

// Dải tuần hiện tại (T2..CN) + số ngày đã chấm.
const buildWeek = (records) => {
    const now = new Date();
    const day = now.getDay();
    const mondayOffset = day === 0 ? -6 : 1 - day;
    const monday = new Date(
        now.getFullYear(),
        now.getMonth(),
        now.getDate() + mondayOffset
    );
    const keySet = new Set(
        records.map((r) => new Date(r.attendanceDate).toDateString())
    );
    const todayKey = now.toDateString();
    const days = [];
    for (let i = 0; i < 7; i++) {
        const d = new Date(
            monday.getFullYear(),
            monday.getMonth(),
            monday.getDate() + i
        );
        const key = d.toDateString();
        days.push({
            label: dayNames[i],
            num: d.getDate(),
            isToday: key === todayKey,
            checked: keySet.has(key),
        });
    }
    return { days, count: days.filter((d) => d.checked).length };
};

// Đồng hồ giờ Việt Nam (UTC+7) cập nhật mỗi giây.
const useVnClock = () => {
    const [now, setNow] = useState(() => new Date());
    useEffect(() => {
        const id = setInterval(() => setNow(new Date()), 1000);
        return () => clearInterval(id);
    }, []);
    const date = now.toLocaleDateString("vi-VN", {
        weekday: "long",
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        timeZone: VN_TIMEZONE,
    });
    const time = now.toLocaleTimeString("vi-VN", {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
        hour12: false,
        timeZone: VN_TIMEZONE,
    });
    return { date, time };
};

// Vị trí GPS hiện tại (vị trí chấm công thực tế).
const useGeolocation = () => {
    const [loc, setLoc] = useState({ status: "pending", text: "Đang xác định vị trí..." });
    useEffect(() => {
        if (typeof navigator === "undefined" || !navigator.geolocation) {
            setLoc({ status: "err", text: "Trình duyệt không hỗ trợ định vị" });
            return;
        }
        let alive = true;
        navigator.geolocation.getCurrentPosition(
            (p) => {
                if (!alive) return;
                setLoc({
                    status: "ok",
                    text: `${p.coords.latitude.toFixed(5)}, ${p.coords.longitude.toFixed(5)}`,
                });
            },
            (e) => {
                if (!alive) return;
                const t =
                    e.code === 1
                        ? "Bạn chưa cấp quyền định vị cho trình duyệt"
                        : "Chưa xác định được vị trí";
                setLoc({ status: "err", text: t });
            },
            { enableHighAccuracy: false, timeout: 8000, maximumAge: 60000 }
        );
        return () => {
            alive = false;
        };
    }, []);
    return loc;
};

const AttendanceCard = ({ employeeId, record, history, onChanged }) => {
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    // Ảnh vừa chụp, chờ gửi kèm lần chấm công
    const [pendingPhoto, setPendingPhoto] = useState(null);
    // Đổi key sau mỗi lần chấm công -> camera về trạng thái "chưa chụp"
    const [camKey, setCamKey] = useState(0);

    const { date: vnDate, time: vnTimeNow } = useVnClock();
    const location = useGeolocation();

    const checkInTime = record?.checkInTime
        ? formatVnTime(record.checkInTime)
        : "";
    const checkOutTime = record?.checkOutTime
        ? formatVnTime(record.checkOutTime)
        : "";
    const isCheckedIn = !!checkInTime;
    const isCheckedOut = !!checkOutTime;

    const week = buildWeek(history || []);

    const check = async (type) => {
        if (!employeeId) {
            setError("Chưa xác định được nhân viên. Vui lòng đăng nhập lại.");
            return;
        }
        // Bắt buộc phải có ảnh chụp mới trước khi chấm công (vào ca / ra ca)
        if (!pendingPhoto) {
            setError(
                type === 1
                    ? "Hãy chụp ảnh trước khi nhấn Vào ca."
                    : "Hãy chụp ảnh trước khi nhấn Ra ca."
            );
            return;
        }
        setBusy(true);
        setError("");

        try {
            let photoUrl = null;
            if (pendingPhoto) {
                const up = await relatedApi.uploadPhoto(
                    pendingPhoto,
                    type === 1 ? "checkin" : "checkout"
                );
                photoUrl = up.data?.data;
                if (!photoUrl) {
                    // Upload thất bại (VD: server lỗi) -> báo lỗi, không chấm
                    // công im lặng với ảnh rỗng.
                    throw new Error(
                        up.data?.message || "Không tải được ảnh chụp."
                    );
                }
            }

            const response = await relatedApi.checkin(
                employeeId,
                type,
                photoUrl
            );
            const data = response.data.data;
            // Dùng xong ảnh -> xóa luôn để lần ra/vào ca sau phải chụp mới
            setPendingPhoto(null);
            setCamKey((k) => k + 1);
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
        <section className="att-hero">
            {/* ===== Đồng hồ + ngày (giờ Việt Nam) ===== */}
            <div className="att-clock">
                <span className="att-clock-date">{vnDate}</span>
                <strong className="att-clock-time">{vnTimeNow}</strong>
                <span className="att-clock-tz">Giờ Việt Nam · GMT+7</span>
            </div>

            {/* ===== Chụp ảnh (chưa ra ca) ===== */}
            {!isCheckedOut && (
                <CameraCapture
                    key={camKey}
                    onPhoto={setPendingPhoto}
                    locked={busy}
                />
            )}

            {/* ===== CTA vào ca / ra ca (màu xanh) ===== */}
            {!isCheckedIn && !isCheckedOut ? (
                <div className="att-hero-actions">
                    <button
                        type="button"
                        className="att-hero-btn"
                        onClick={() => check(1)}
                        disabled={busy || !pendingPhoto}
                    >
                        Vào ca
                    </button>
                    {!pendingPhoto && (
                        <span className="att-cam-hint att-cam-hint--center">
                            Chụp ảnh trước khi nhấn Vào ca.
                        </span>
                    )}
                </div>
            ) : isCheckedIn && !isCheckedOut ? (
                <div className="att-hero-actions">
                    <button
                        type="button"
                        className="att-hero-btn"
                        onClick={() => check(2)}
                        disabled={busy || !pendingPhoto}
                    >
                        Ra ca
                    </button>
                    {!pendingPhoto && (
                        <span className="att-cam-hint att-cam-hint--center">
                            Chụp ảnh trước khi nhấn Ra ca.
                        </span>
                    )}
                </div>
            ) : (
                <div className="att-hero-done">
                    Đã chấm công hôm nay. Cảm ơn bạn!
                </div>
            )}

            {/* ===== Vị trí chấm công (GPS) ===== */}
            <div
                className={`att-location${
                    location.status === "err" ? " att-location--err" : ""
                }`}
            >
                <div className="att-location-info">
                    <span>Vị trí chấm công</span>
                    <strong>
                        {location.status === "ok"
                            ? location.text
                            : location.text}
                    </strong>
                </div>
            </div>

            {/* ===== 2 mốc: Vào ca / Ra ca (bỏ Trạng thái) ===== */}
            <div className="att-hero-times att-hero-times--two">
                <div className="att-hero-slot">
                    <span className="att-hero-slot-ico att-hero-slot-ico--blue" aria-hidden="true">
                        ⏱
                    </span>
                    <span className="att-hero-label">Vào ca</span>
                    <strong>{checkInTime || "—"}</strong>
                </div>
                <div className="att-hero-slot">
                    <span className="att-hero-slot-ico att-hero-slot-ico--green" aria-hidden="true">
                        ⏰
                    </span>
                    <span className="att-hero-label">Ra ca</span>
                    <strong>{checkOutTime || "—"}</strong>
                </div>
            </div>

            {/* ===== Tuần ===== */}
            <div className="att-week-section">
                <div className="att-week-head">
                    <h2>Tuần này</h2>
                    <span className="att-week-count">
                        <strong>{week.count}</strong> ngày đã chấm
                    </span>
                </div>
                <div className="att-week">
                    {week.days.map((d) => (
                        <div
                            key={d.label}
                            className={`att-week-day${d.isToday ? " today" : ""}${
                                d.checked ? " checked" : ""
                            }`}
                        >
                            <span className="att-week-num">{d.num}</span>
                            <span className="att-week-name">{d.label}</span>
                        </div>
                    ))}
                </div>
            </div>

            {error && <p className="att-checkin-error">{error}</p>}
        </section>
    );
};

export default AttendanceCard;
