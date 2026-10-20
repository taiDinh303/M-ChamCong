// Match complete path segments so /attendance-history and /employees-old stay distinct.
export function canonicalModulePath(pathname) {
    return pathname.replace(/^\/(attendance|employees)(?=\/|$)/,
        (_, module) => module === "attendance" ? "/me" : "/hr");
}
