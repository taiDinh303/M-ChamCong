import { Navigate, useLocation } from "react-router-dom";
import { canonicalModulePath } from "../../constants/routes.js";

export default function LegacyModuleRedirect() {
    const { pathname, search, hash } = useLocation();
    return <Navigate to={`${canonicalModulePath(pathname)}${search}${hash}`} replace />;
}
