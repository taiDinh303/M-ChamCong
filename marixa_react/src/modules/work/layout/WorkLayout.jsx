import AuthenticatedShell from "../../../components/layout/AuthenticatedShell";
import WorkSidebar from "./WorkSidebar";
import "../../me/attendance.css";
import "../../hr/employee.css";
import "../../admin/admin.css";
import "../work.css";

export default function WorkLayout({ search, onSearchChange, children }) {
    return <AuthenticatedShell storageKey="marixa_work_sidebar_collapsed"
        sidebar={(collapsed) => <WorkSidebar collapsed={collapsed} search={search} setSearch={onSearchChange} />}>
        {children}
    </AuthenticatedShell>;
}
