import test from "node:test";
import assert from "node:assert/strict";
import { notificationTarget } from "../src/components/layout/notificationRoutes.js";

test("personal notifications open personal pages", () => {
    assert.equal(notificationTarget("leave", ["Employee"]), "/me/leave");
    assert.equal(notificationTarget("report", ["Employee"]), "/me/reports");
    assert.equal(notificationTarget("handover", []), "/me/handover");
});

test("HR and managers open their review pages while admins keep their own pages", () => {
    for (const role of ["HR", "Manager"]) {
        assert.equal(notificationTarget("leave", [role]), "/hr/leaves");
        assert.equal(notificationTarget("report", [role]), "/hr/reports");
        assert.equal(notificationTarget("handover", [role]), "/hr/handover");
    }
    assert.equal(notificationTarget("leave", ["Admin", "HR"]), "/admin/leaves");
    assert.equal(notificationTarget("report", ["Admin"]), "/admin/reports");
    assert.equal(notificationTarget("handover", ["Admin"]), "/hr/handover");
});

test("unknown notification types cannot invent navigation targets", () => {
    for (const kind of ["unknown", "toString", "__proto__", "https://example.test"]) {
        assert.equal(notificationTarget(kind, ["Admin"]), null);
    }
});
