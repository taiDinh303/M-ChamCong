import { Link } from "react-router-dom";

// Khối cảnh báo "Bạn cần kiểm tra" (chỉ hiện khi có việc cần xử lý).
const StatWarnings = ({ warnings }) => {
    const hasItems = warnings.list.length > 0 || warnings.noDataDays > 0;
    if (!hasItems) return null;
    const count = warnings.list.length + (warnings.noDataDays > 0 ? 1 : 0);

    return (
        <aside className="me-stat-warning" aria-label="Công cần kiểm tra">
                <span className="att-stat-tile-icon warn" aria-hidden="true">!</span>
            <div>
                <h2>{count} mục cần kiểm tra</h2>
                <details><summary>Xem các ngày cần đối chiếu</summary>
                    <ul>{warnings.list.map((item, index) => <li key={index}>{item}</li>)}
                        {warnings.noDataDays > 0 && <li>{warnings.noDataDays} ngày chưa có dữ liệu chấm công</li>}</ul>
                </details>
            </div>
            <Link className="me-action" to="/me/attendance-history">Kiểm tra lịch sử công →</Link>
        </aside>
    );
};

export default StatWarnings;
