import EntityDetailPage from "../../attendance/components/EntityDetailPage";

const statusLabel = (s) =>
    ({ 1: "Chờ duyệt", 2: "Đã duyệt", 3: "Từ chối", 4: "Đã hủy" })[s] || "—";

const fmt = (d) => (d ? new Date(d).toLocaleDateString("vi-VN") : "—");

const LeavePage = () => (
    <EntityDetailPage
        kind="leave"
        title="Đơn xin nghỉ phép"
        subtitle="Lịch sử và trạng thái đơn nghỉ phép của bạn"
        emptyMessage="Chưa có đơn nghỉ phép nào."
        columns={[
            { key: "leaveTypeName", label: "Loại nghỉ", render: (r) => r.leaveTypeName || "Nghỉ phép" },
            { key: "fromDate", label: "Từ ngày", render: (r) => fmt(r.fromDate) },
            { key: "toDate", label: "Đến ngày", render: (r) => fmt(r.toDate) },
            { key: "totalDays", label: "Số ngày", render: (r) => (r.totalDays ?? "—") },
            { key: "status", label: "Trạng thái", render: (r) => statusLabel(r.status) },
            { key: "reason", label: "Lý do" },
        ]}
    />
);

export default LeavePage;
