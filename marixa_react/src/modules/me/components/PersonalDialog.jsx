import useModalDialog from "../../../components/common/useModalDialog";

export default function PersonalDialog({ label, busy = false, onClose, children }) {
    const ref = useModalDialog();
    return <dialog ref={ref} className="me-dialog" aria-label={label}
        onCancel={(event) => { event.preventDefault(); if (!busy) onClose(); }}
        onClick={(event) => { if (event.target === event.currentTarget && !busy) onClose(); }}>
        {children}
    </dialog>;
}
