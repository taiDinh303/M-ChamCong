import { Navigate, Outlet } from "react-router-dom";
import { getAuth } from "../../services/auth/auth";

export default function RequireSession() {
    return getAuth()?.token ? <Outlet /> : <Navigate to="/login" replace />;
}
