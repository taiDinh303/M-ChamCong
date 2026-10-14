import { useState, useEffect } from "react";
import { translate, useLanguage } from "../../../../services/i18n/LanguageProvider";

// Modal thêm / sửa bản ghi chấm công (admin).
// row = null -> thêm mới; row có giá trị -> sửa.
const STATUS_OPTIONS = [
    { v: "", l: "Chưa đánh giá" },
    { v: 1, l: "Đúng giờ" },
    { v: 2, l: "Đi trễ" },
    { v: 3, l: "Về sớm" },
    { v: 4, l: "Vắng mặt" },
    { v: 5, l: "Nghỉ phép" },
    { v: 6, l: "Lễ" },
    { v: 7, l: "Ngoại tuần" },
];

const APPROVAL_OPTIONS = [
    { v: 0, l: "Chờ duyệt" },
    { v: 1, l: "Đã duyệt" },
    { v: 2, l: "Từ chối" },
];

const normalizeEmployeeSearch = (value) =>
    String(value || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .replace(/đ/gi, "d")
        .toLocaleLowerCase("vi-VN");

// Khớp với AttendanceTimeCalculator (backend): cửa sổ nghỉ trưa 12:00–13:00
const LUNCH_START_MIN = 720; // 12:00
const LUNCH_END_MIN = 780;   // 13:00

/**
 * Tính actualHours (net) khớp AttendanceTimeCalculator.Compute:
 * 1. elapsed = |out − in| (quá nửa đêm → +24h)
 * 2. lunchOverlap = phần trùng cửa sổ 12:00–13:00 (max 60 phút)
 * 3. net = elapsed − lunchOverlap, actualHours = round(net / 60)
 */
const computeActualHours = (inTime, outTime) => {
    const [inH, inM] = inTime.split(":").map(Number);
    const [outH, outM] = outTime.split(":").map(Number);
    const inMin = inH * 60 + inM;
    const outMin = outH * 60 + outM;

    let elapsed = outMin - inMin;
    if (elapsed < 0) elapsed += 24 * 60;

    let lunch = 0;
    if (inMin <= outMin) {
        const s = Math.max(inMin, LUNCH_START_MIN);
        const e = Math.min(outMin, LUNCH_END_MIN);
        if (e > s) lunch = Math.min(60, e - s);
    } else {
        // Ca quá nửa đêm
        if (inMin < LUNCH_END_MIN) lunch += 1440 - Math.max(inMin, LUNCH_START_MIN);
        if (outMin > LUNCH_START_MIN) lunch += outMin - LUNCH_START_MIN;
        lunch = Math.min(60, lunch);
    }

    const netMin = Math.max(0, elapsed - lunch);
    return String(Math.round(netMin / 60));
};

// Chuyển ISO (UTC) → "HH:MM" VN (khớp formatVnTime / formatVnTime dùng ở bảng)
const toVnHHMM = (iso) =>
    iso
        ? new Date(iso).toLocaleTimeString("en-GB", {
              hour: "2-digit",
              minute: "2-digit",
              hour12: false,
              timeZone: "Asia/Ho_Chi_Minh",
          })
        : "";

const buildDraft = (row) =>
    row
        ? {
              employeeId: row.employeeId,
              attendanceDate: (row.attendanceDate || "").slice(0, 10),
              checkInTime: toVnHHMM(row.checkInTime),
              checkOutTime: toVnHHMM(row.checkOutTime),
              status: row.status ?? "",
              actualHours: row.actualHours ?? "",
              approvalStatus: row.approvalStatus ?? 0,
              note: row.note || "",
          }
        : {
              employeeId: "",
              attendanceDate: new Date().toISOString().slice(0, 10),
              checkInTime: "",
              checkOutTime: "",
              status: "",
              actualHours: "",
              approvalStatus: 0,
              note: "",
          };

const AttendanceFormModal = ({ open, row, employees, onClose, onSubmit }) => {
    const [form, setForm] = useState(null);
    const [error, setError] = useState("");
    const [employeeSearch, setEmployeeSearch] = useState("");
    const [showEmployeeOptions, setShowEmployeeOptions] = useState(false);
    const { language } = useLanguage();
    const L = (text) => translate(text, language);

    // Nạp form mỗi lần mở (thêm: trống, sửa: từ row)
    useEffect(() => {
        if (open) {
            setForm(buildDraft(row));
            setError("");
            const selected = employees.find((employee) => employee.id === row?.employeeId);
            setEmployeeSearch(selected ? `${selected.fullName} · ${selected.employeeCode}` : "");
            setShowEmployeeOptions(false);
        }
    }, [open, row, employees]);

    if (!open || !form) return null;

    const set = (k) => (e) =>
        setForm({ ...form, [k]: e.target.value });

    const setTime = (key) => (event) => {
        const next = { ...form, [key]: event.target.value };
        if (next.checkInTime && next.checkOutTime) {
            next.actualHours = computeActualHours(next.checkInTime, next.checkOutTime);
        }
        setForm(next);
    };

    const searchTerm = normalizeEmployeeSearch(employeeSearch.trim());
    const employeeOptions = searchTerm
        ? employees
              .filter((employee) => normalizeEmployeeSearch(`${employee.fullName} ${employee.employeeCode}`).includes(searchTerm))
              .slice(0, 8)
        : [];

    const submit = async (e) => {
        e.preventDefault();
        if (!form.employeeId || !form.attendanceDate) {
            setError("Chọn nhân viên và ngày là bắt buộc.");
            return;
        }
        if ([1, 2].includes(Number(form.approvalStatus)) && !form.status) {
            setError("Vui lòng chọn Trạng thái trước khi duyệt hoặc từ chối.");
            return;
        }
        if (Number(form.approvalStatus) === 2 && !form.note.trim()) {
            setError("Vui lòng nhập lý do từ chối.");
            return;
        }
        setError("");
        try {
            await onSubmit(form, !!row);
            setForm(null);
        } catch {
            // giữ form để người dùng sửa lỗi
        }
    };

    return (
        <div className="att-guide-overlay">
            <div className="att-form-modal" onClick={(e) => e.stopPropagation()}>
                <h2>{row?._approvalDecision ? (row.approvalStatus === 1 ? "Duyệt bản ghi chấm công" : "Từ chối bản ghi chấm công") : row ? "Sửa bản ghi chấm công" : "Thêm bản ghi chấm công"}</h2>
                <form onSubmit={submit} className="att-form">
                    <label>
                        {L("Nhân viên")}
                        <div className="att-employee-picker" onBlur={(event) => {
                            if (!event.currentTarget.contains(event.relatedTarget)) setShowEmployeeOptions(false);
                        }}>
                            <input
                                required
                                role="combobox"
                                aria-autocomplete="list"
                                aria-expanded={showEmployeeOptions && employeeOptions.length > 0}
                                aria-controls="att-employee-options"
                                value={employeeSearch}
                                placeholder={L("Nhập họ tên hoặc mã nhân viên...")}
                                onFocus={() => setShowEmployeeOptions(true)}
                                onChange={(event) => {
                                    setEmployeeSearch(event.target.value);
                                    setShowEmployeeOptions(true);
                                    setForm({ ...form, employeeId: "" });
                                }}
                            />
                            {showEmployeeOptions && employeeOptions.length > 0 && (
                                <div className="att-employee-options" id="att-employee-options" role="listbox">
                                    {employeeOptions.map((employee) => (
                                        <button
                                            type="button"
                                            role="option"
                                            aria-selected={form.employeeId === employee.id}
                                            key={employee.id}
                                            onClick={() => {
                                                setForm({ ...form, employeeId: employee.id });
                                                setEmployeeSearch(`${employee.fullName} · ${employee.employeeCode}`);
                                                setShowEmployeeOptions(false);
                                            }}
                                        >
                                            <strong>{employee.fullName}</strong>
                                            <span>{employee.employeeCode}</span>
                                        </button>
                                    ))}
                                </div>
                            )}
                            {showEmployeeOptions && searchTerm && employeeOptions.length === 0 && (
                                <div className="att-employee-empty">{L("Không tìm thấy nhân viên phù hợp.")}</div>
                            )}
                        </div>
                    </label>

                    <div className="att-form-row">
                        <label>
                            {L("Ngày")}
                            <input
                                type="date"
                                required
                                value={form.attendanceDate}
                                onChange={set("attendanceDate")}
                            />
                        </label>

                        <label>
                            Trạng thái{[1, 2].includes(Number(form.approvalStatus)) ? " *" : ""}
                            <select required={[1, 2].includes(Number(form.approvalStatus))} value={form.status} onChange={set("status")}>
                                {STATUS_OPTIONS.map((o) => (
                                    <option key={o.l} value={o.v}>
                                        {L(o.l)}
                                    </option>
                                ))}
                            </select>
                        </label>
                    </div>

                    <div className="att-form-row">
                            <label>{L("Giờ vào ca")}<input type="time" value={form.checkInTime} onChange={setTime("checkInTime")} /></label>
                            <label>{L("Giờ ra ca")}<input type="time" value={form.checkOutTime} onChange={setTime("checkOutTime")} /></label>
                        </div>

                    <div className="att-form-row">
                        <label>
                            {L("Giờ thực tế")}
                            <input
                                type="number"
                                min="0"
                                step="1"
                                value={form.actualHours}
                                onChange={set("actualHours")}
                                placeholder={L("Tùy chọn")}
                            />
                        </label>

                        <label>
                            {L("Phê duyệt")}
                            <select
                                value={form.approvalStatus}
                                onChange={set("approvalStatus")}
                            >
                                {APPROVAL_OPTIONS.map((o) => (
                                    <option key={o.v} value={o.v}>
                                        {L(o.l)}
                                    </option>
                                ))}
                            </select>
                        </label>
                    </div>

                    <label>
                        {Number(form.approvalStatus) === 2 ? "Lý do từ chối *" : "Ghi chú"}
                        <textarea
                            rows="2"
                            required={Number(form.approvalStatus) === 2}
                            value={form.note}
                            onChange={set("note")}
                            placeholder={Number(form.approvalStatus) === 2 ? "Nhập lý do từ chối" : "Tùy chọn"}
                        />
                    </label>

                    {error && <p className="att-cam-error">{error}</p>}

                    <div className="att-form-actions">
                        <button
                            type="button"
                            className="att-cam-btn-remove"
                            onClick={onClose}
                        >
                            {L("Hủy")}
                        </button>
                        <button type="submit" className="admin-link-btn">
                            {row?._approvalDecision ? (row.approvalStatus === 1 ? "Xác nhận duyệt" : "Xác nhận từ chối") : row ? "Lưu sửa" : "Thêm mới"}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};

export default AttendanceFormModal;
