import "./connection.css";

export default function ConnectionScreen({ preparing = false, completed = 0, total = 0, onCancel, error, onRetry }) {
    return <section className="mx-connection-screen" aria-label={preparing ? "Chuẩn bị không gian làm việc" : "Đang kết nối"} aria-busy={!error}>
        <div className={`mx-connection-card${error ? " mx-connection-card--error" : ""}`}>
            <div className="mx-connection-orbit" aria-hidden="true"><span>M</span><i /></div>
            <p className="mx-connection-brand">MARIXA</p>
            <h2>{error ? "Chưa kết nối được máy chủ" : preparing ? "Chuẩn bị không gian làm việc" : "Đang kết nối đến Marixa"}</h2>
            <p role={error ? "alert" : undefined}>{error || (preparing ? "Đang tải trước dữ liệu để bạn chuyển trang nhanh hơn." : "Máy chủ có thể đang khởi động lại. Vui lòng giữ trang này mở.")}</p>
            {preparing && total > 0
                ? <><progress value={completed} max={total} aria-label="Tiến độ tải dữ liệu" /><p role="status">Đã xử lý {completed}/{total} nhóm dữ liệu</p></>
                : !error && <div className="mx-connection-dots" role="status" aria-label="Vui lòng chờ"><i /><i /><i /></div>}
            {onRetry && <button type="button" onClick={onRetry}>Thử kết nối lại</button>}
            {onCancel && <button type="button" onClick={onCancel}>Quay lại đăng nhập</button>}
        </div>
    </section>;
}
