import test from "node:test";
import assert from "node:assert/strict";
import { canonicalModulePath } from "../src/constants/routes.js";

test("legacy module roots and nested pages resolve to their canonical routes", () => {
    for (const [before, after] of [["attendance", "me"], ["employees", "hr"]]) {
        for (const suffix of ["", "/", "/statistics", "/attendance-history", "/nested/detail"]) {
            assert.equal(canonicalModulePath(`/${before}${suffix}`), `/${after}${suffix}`);
        }
    }
});

test("module migration does not rewrite similar names or unrelated routes", () => {
    for (const path of ["/attendance-history", "/attendance-rules", "/employees-old", "/admin/employees", "/home", "/api/Attendance/get-all"]) {
        assert.equal(canonicalModulePath(path), path);
    }
});

test("canonical paths stay stable when normalized repeatedly", () => {
    for (const path of ["/me", "/me/statistics", "/hr", "/hr/attendance-history", "/admin/roles"]) {
        assert.equal(canonicalModulePath(canonicalModulePath(path)), path);
    }
});
