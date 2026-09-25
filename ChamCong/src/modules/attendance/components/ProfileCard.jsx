const fields = [
    ["Họ và tên", (p) => p?.fullName || "—"],
    ["Mã nhân viên", (p) => p?.employeeCode || "—"],
    ["Phòng ban", (p) => p?.departmentName || "—"],
    ["Chức vụ", (p) => p?.positionName || "—"],
    ["Email", (p) => p?.email || "—"],
    ["Điện thoại", (p) => p?.phoneNumber || "—"],
    ["Ngày vào làm", (p) => (p?.startDate ? new Date(p.startDate).toLocaleDateString("vi-VN") : "—")],
    ["Trạng thái", (p) => statusLabel(p?.status)],
];

const statusLabel = (s) =>
    ({
        1: "Thử việc",
        2: "Đang làm",
        3: "Tạm nghỉ",
        4: "Đã nghỉ việc",
        5: "Chấm dứt",
    })[s] || "—";

const ProfileCard = ({ profile, auth }) => {
    const source = profile || {
        fullName: `${auth?.userName || ""}`.trim(),
        employeeCode: auth?.employeeCode,
    };

    return (
        <section className="att-card">
            <h2>Thông tin cá nhân</h2>
            <dl className="att-profile">
                {fields.map(([label, getter]) => (
                    <div className="att-profile-row" key={label}>
                        <dt>{label}</dt>
                        <dd>{getter(source)}</dd>
                    </div>
                ))}
            </dl>
        </section>
    );
};

export default ProfileCard;
