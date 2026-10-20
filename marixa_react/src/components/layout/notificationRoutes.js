export function notificationTarget(kind, roles = []) {
    const admin = roles.includes("Admin");
    const staff = admin || roles.some((role) => ["HR", "Manager"].includes(role));
    const targets = {
        leave: admin ? "/admin/leaves" : staff ? "/hr/leaves" : "/me/leave",
        report: admin ? "/admin/reports" : staff ? "/hr/reports" : "/me/reports",
        handover: staff ? "/hr/handover" : "/me/handover",
    };
    return Object.hasOwn(targets, kind) ? targets[kind] : null;
}
