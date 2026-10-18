import AuthenticatedShell from "../../../components/layout/AuthenticatedShell";
import AdminSidebar from "./AdminSidebar";

export default function AdminAppLayout({ profile, children }) {
    return <AuthenticatedShell storageKey="marixa_admin_sidebar_collapsed" profile={profile}
        sidebar={(collapsed) => <AdminSidebar collapsed={collapsed} />}>
        {children}
    </AuthenticatedShell>;
}
