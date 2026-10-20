import { useEffect, useSyncExternalStore } from "react";
import { useLocation } from "react-router-dom";
import { connection } from "../../services/api/connection.js";
import { responseCache } from "../../services/api/responseCache.js";
import { resetWarmup } from "../../services/api/workspaceWarmup.js";
import "./connection.css";

export default function ConnectionStatus() {
    const status = useSyncExternalStore(connection.subscribe, connection.getSnapshot);
    const { pathname } = useLocation();
    useEffect(() => {
        const offline = () => connection.offline();
        const online = () => { connection.ready({ force: true }).catch(() => {}); };
        const changed = (event) => {
            if (event.key === "marixa_auth") { responseCache.clear(); resetWarmup(); }
            if (event.key === "marixa_data_revision") responseCache.clear();
        };
        window.addEventListener("offline", offline); window.addEventListener("online", online);
        window.addEventListener("storage", changed);
        if (!navigator.onLine) offline();
        return () => {
            window.removeEventListener("offline", offline); window.removeEventListener("online", online);
            window.removeEventListener("storage", changed);
        };
    }, []);
    if (status.phase === "online" || pathname === "/login") return null;
    return <aside className="mx-connection-notice" role="status" aria-live="polite">
        <span className={status.phase === "connecting" ? "mx-connection-pulse" : ""} aria-hidden="true">●</span>
        <div><strong>{status.phase === "offline" ? "Thiết bị đang mất kết nối mạng" : status.phase === "connecting" ? "Đang kết nối lại với máy chủ…" : "Chưa kết nối được máy chủ"}</strong>
            <p>Dữ liệu đã tải vẫn được giữ trong phiên này. Các thao tác gửi dữ liệu cần có kết nối.</p></div>
        <button type="button" disabled={status.phase === "connecting"} onClick={() => connection.ready({ force: true }).catch(() => {})}>Thử kết nối lại</button>
    </aside>;
}
