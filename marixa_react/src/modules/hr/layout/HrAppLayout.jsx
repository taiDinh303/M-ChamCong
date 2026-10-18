import AuthenticatedShell from "../../../components/layout/AuthenticatedShell";
import HrSidebar from "./HrSidebar";
import "../employee.css";

export default function HrAppLayout({ profile, children }) {
    return <AuthenticatedShell storageKey="marixa_hr_sidebar_collapsed" profile={profile}
        sidebar={(collapsed) => <HrSidebar collapsed={collapsed} />}>
        {children}
    </AuthenticatedShell>;
}
