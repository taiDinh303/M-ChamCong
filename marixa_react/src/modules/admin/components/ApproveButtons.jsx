// Cặp nút Duyệt / Từ chối (hiện khi bản ghi đang chờ duyệt).
const ApproveButtons = ({ onApprove, onReject, pending, disabled = false }) => {
    if (!pending) return <span className="att-muted">—</span>;
    return (
        <div className="admin-row-actions">
            <button
                type="button"
                className="admin-link-btn"
                disabled={disabled}
                onClick={onApprove}
            >
                Duyệt
            </button>
            <button
                type="button"
                className="admin-link-btn admin-link-btn--danger"
                disabled={disabled}
                onClick={onReject}
            >
                Từ chối
            </button>
        </div>
    );
};

export default ApproveButtons;
