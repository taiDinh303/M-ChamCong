import useModalDialog from "./useModalDialog";
import ConnectionScreen from "./ConnectionScreen.jsx";

export default function ConnectionOverlay({ onCancel }) {
    const ref = useModalDialog();
    return <dialog className="login-connection-layer" ref={ref} aria-label="Đang đăng nhập"
        onCancel={(event) => { event.preventDefault(); onCancel(); }}>
        <ConnectionScreen onCancel={onCancel} />
    </dialog>;
}
