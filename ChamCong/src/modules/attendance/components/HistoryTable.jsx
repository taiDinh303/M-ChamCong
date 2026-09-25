const statusLabel = (s) =>
    ({
        1: "Đủ mặt",
        2: "Đi trễ",
        3: "Về sớm",
        4: "Vắng mặt",
        5: "Nghỉ phép",
        6: "Lễ",
        7: "Ngoại tuần",
    })[s] || "Chưa đánh giá";

const approvalLabel = (s) =>
    ({ 0: "Chờ duyệt", 1: "Đã duyệt", 2: "Từ chối" })[s] ?? "Chờ duyệt";

const HistoryTable = ({ history }) => {
    if (!history || history.length === 0) {
        return (
            <section className="att-card">
                <h2>Lịch sử chấm công</h2>
                <p className="att-muted">Chưa có bản ghi chấm công nào.</p>
            </section>
        );
    }

    return (
        <section className="att-card">
            <h2>Lịch sử chấm công</h2>
            <div className="att-table-wrap">
                <table className="att-table">
                    <thead>
                        <tr>
                            <th>Ngày</th>
                            <th>Ca</th>
                            <th>Trạng thái</th>
                            <th>Giờ thực tế</th>
                            <th>Duyệt</th>
                        </tr>
                    </thead>
                    <tbody>
                        {history.map((row) => (
                            <tr key={row.id}>
                                <td>{new Date(row.attendanceDate).toLocaleDateString("vi-VN")}</td>
                                <td>{row.plannedShiftName || "—"}</td>
                                <td>
                                    <span className={`att-badge ${statusClass(row.status)}`}>
                                        {statusLabel(row.status)}
                                    </span>
                                </td>
                                <td>{row.actualHours != null ? `${row.actualHours}h` : "—"}</td>
                                <td>
                                    <span className={`att-badge ${approvalClass(row.approvalStatus)}`}>
                                        {approvalLabel(row.approvalStatus)}
                                    </span>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </section>
    );
};

const statusClass = (s) => ({ 1: "ok", 2: "warn", 3: "warn", 4: "bad", 5: "info", 6: "info", 7: "info" })[s] || "";
const approvalClass = (s) => ({ 0: "warn", 1: "ok", 2: "bad" })[s] || "";

export default HistoryTable;
