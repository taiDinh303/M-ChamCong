import EmployeeLeavePage from "../../../hr/leaves/pages/EmployeeLeavePage";

// Share the HR leave workflow while retaining the Admin navigation shell.
export default function LeaveAdminPage({ hrMode = false }) {
    return <EmployeeLeavePage layout={hrMode ? "hr" : "admin"} />;
}
