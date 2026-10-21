import { hoursShort } from "./statUtils";
import MetricCard from "../../../components/common/MetricCard";

export default function StatKpis({ stats }) {
    const cards = [
        { label: "Công thường", value: hoursShort(stats.hoursWorked), icon: "◷", tone: "blue",
            hint: `Mục tiêu tháng: ${hoursShort(stats.standardMin)} · Không gồm tăng ca` },
        { label: "Ngày có công", value: `${stats.daysWorked} / ${stats.expectedWorkdays}`, icon: "▦", tone: "green",
            hint: "Ngày ghi nhận / ngày làm việc dự kiến" },
        { label: "Tăng ca được duyệt", value: hoursShort(stats.otWeekday + stats.otWeekend + stats.otHoliday), icon: "↗", tone: "purple",
            hint: `Thường ${hoursShort(stats.otWeekday)} · Cuối tuần ${hoursShort(stats.otWeekend)} · Lễ ${hoursShort(stats.otHoliday)}` },
        { label: "Đi trễ", value: `${stats.late} lần`, icon: "!", tone: stats.late ? "gold" : "green",
            hint: stats.late ? "Xem ngày và giờ chi tiết trong lịch sử công" : "Không ghi nhận đi trễ trong tháng" },
        { label: "Nghỉ phép trong tháng", value: `${stats.leaveDays} ngày`, icon: "☼", tone: "pink",
            hint: "Ngày chấm công ghi nhận trạng thái nghỉ phép" },
        { label: "Số dư phép năm", value: stats.hasQuota ? `${stats.remainingLeave} ngày` : "—", icon: "◇", tone: "teal",
            hint: stats.hasQuota ? "Theo sổ phép được cấp và điều chỉnh" : "Chưa tải được số dư sổ phép" },
    ];
    return <div className="me-stat-metrics" aria-label="Tổng hợp công trong tháng">
        {cards.map((card) => <MetricCard key={card.label} {...card} className="me-stat-metric" />)}
    </div>;
}
