import { Navigate, Outlet } from "react-router-dom";
import { getAuth } from "../../services/auth/auth";
import { useEffect, useSyncExternalStore } from "react";
import { warmWorkspace, warmupSnapshot, subscribeWarmup, resetWarmup } from "../../services/api/workspaceWarmup.js";
import { sessionScope } from "../../services/api/responseCache.js";
import ConnectionScreen from "./ConnectionScreen.jsx";

export default function RequireSession() {
    const auth = getAuth();
    const scope = sessionScope(auth);
    const warmup = useSyncExternalStore(subscribeWarmup, warmupSnapshot);
    useEffect(() => {
        if (scope && (warmup.scope !== scope || warmup.phase === "idle")) warmWorkspace(getAuth());
    }, [scope, warmup.scope, warmup.phase]);
    if (!auth?.token) return <Navigate to="/login" replace />;
    if (warmup.scope !== scope || warmup.phase !== "ready") return <ConnectionScreen
        preparing={warmup.phase === "loading"} completed={warmup.completed} total={warmup.total}
        error={warmup.phase === "error" ? "Hãy kiểm tra kết nối mạng và thử lại. Dữ liệu và phiên đăng nhập của bạn vẫn được giữ." : undefined}
        onRetry={warmup.phase === "error" ? () => { resetWarmup(); warmWorkspace(getAuth()); } : undefined} />;
    return <Outlet />;
}
