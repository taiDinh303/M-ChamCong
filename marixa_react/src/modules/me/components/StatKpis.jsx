import { hoursShort } from "./statUtils";

export default function StatKpis({ stats }) {
    const cards = [
        { label: "Công thường", value: hoursShort(stats.hoursWorked), icon: "◷",
            hint: `Mục tiêu tháng: ${hoursShort(stats.standardMin)} · Không gồm tăng ca` },
        { label: "Ngày có công", value: `${stats.daysWorked} / ${stats.expectedWorkdays}`, icon: "▦",
            hint: "Ngày ghi nhận / ngày làm việc dự kiến" },
        { label: "Tăng ca được duyệt", value: hoursShort(stats.otWeekday + stats.otWeekend + stats.otHoliday), icon: "↗",
            hint: `Thường ${hoursShort(stats.otWeekday)} · Cuối tuần ${hoursShort(stats.otWeekend)} · Lễ ${hoursShort(stats.otHoliday)}` },
        { label: "Đi trễ", value: `${stats.late} lần`, icon: "!",
            hint: stats.late ? "Xem ngày và giờ chi tiết trong lịch sử công" : "Không ghi nhận đi trễ trong tháng" },
        { label: "Nghỉ phép trong tháng", value: `${stats.leaveDays} ngày`, icon: "☼",
            hint: "Ngày chấm công ghi nhận trạng thái nghỉ phép" },
        { label: "Số dư phép năm", value: stats.hasQuota ? `${stats.remainingLeave} ngày` : "—", icon: "◇",
            hint: stats.hasQuota ? "Theo sổ phép được cấp và điều chỉnh" : "Chưa tải được số dư sổ phép" },
    ];
    return <div className="me-stat-metrics" aria-label="Tổng hợp công trong tháng">
        {cards.map(({ label, value, icon, hint }) => <article className="me-stat-metric" key={label}>
            <span className="me-stat-icon" aria-hidden="true">{icon}</span>
            <div><h2>{label}</h2><strong>{value}</strong><p>{hint}</p></div>
        </article>)}
    </div>;
}
