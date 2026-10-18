import AuthenticatedShell from "./AuthenticatedShell";
import Sidebar from "./Sidebar";

export default function AppLayout({ profile, children }) {
    return <AuthenticatedShell storageKey="marixa_sidebar_collapsed" profile={profile}
        sidebar={(collapsed) => <Sidebar collapsed={collapsed} />}>
        {children}
    </AuthenticatedShell>;
}
