import test, { beforeEach } from "node:test";
import assert from "node:assert/strict";
import { saveAuth, getAuth, clearAuth } from "../src/services/auth/auth.js";
import { canAccessModule } from "../src/services/auth/permission.js";

class MemoryStorage {
    values = new Map();
    getItem(key) { return this.values.get(key) ?? null; }
    setItem(key, value) { this.values.set(key, value); }
    removeItem(key) { this.values.delete(key); }
}
beforeEach(() => { globalThis.localStorage = new MemoryStorage(); globalThis.sessionStorage = new MemoryStorage(); });
const fixture = (roles = ["Employee"]) => ({ token: "isolated-test-token", userId: "fake-user", roles, expiredAt: new Date(Date.now() + 3600000).toISOString() });
test("unchecked remember-me keeps the login in session storage only", () => {
    saveAuth(fixture(), false);
    assert.equal(localStorage.getItem("marixa_auth"), null);
    assert.equal(getAuth().userId, "fake-user");
});
test("remember-me persists the login and clears the prior session", () => {
    saveAuth(fixture(), false);
    saveAuth(fixture(["HR"]), true);
    assert.equal(sessionStorage.getItem("marixa_auth"), null);
    assert.deepEqual(getAuth().roles, ["HR"]);
});
test("expired and malformed persisted sessions are not reused", () => {
    localStorage.setItem("marixa_auth", "invalid json");
    assert.equal(getAuth(), null);
    saveAuth({ ...fixture(), expiredAt: "2000-01-01T00:00:00Z" }, true);
    assert.equal(getAuth(), null);
    assert.equal(localStorage.getItem("marixa_auth"), null);
});
test("logout removes both legacy and session credentials", () => {
    localStorage.setItem("marixa_auth", "legacy"); sessionStorage.setItem("marixa_auth", "session");
    clearAuth();
    assert.equal(getAuth(), null);
});
test("unregistered admin subroutes inherit the admin boundary", () => {
    saveAuth(fixture());
    assert.equal(canAccessModule("/admin/block-accounts"), false);
    assert.equal(canAccessModule("/admin/roles"), false);
    assert.equal(canAccessModule("/hr/statistics"), false);
    assert.equal(canAccessModule("/me/statistics"), true);
});
test("HR can reach HR routes without gaining admin role management", () => {
    saveAuth(fixture(["HR"]));
    assert.equal(canAccessModule("/hr/statistics"), true);
    assert.equal(canAccessModule("/admin/roles"), false);
    saveAuth(fixture(["Admin"]));
    assert.equal(canAccessModule("/admin/roles"), true);
});

test("only Admin and Accountant can open accounting; Accountant does not gain HR or admin access", () => {
    for (const role of ["Admin", "Accountant", "HR", "Manager", "Employee"]) {
        saveAuth(fixture([role]));
        for (const path of ["/accounting", "/accounting/payroll", "/accounting/unknown", "/finance", "/hr/payroll"])
            assert.equal(canAccessModule(path), ["Admin", "Accountant"].includes(role), `${role}: ${path}`);
    }
    saveAuth(fixture(["Accountant"]));
    assert.equal(canAccessModule("/hr"), false);
    assert.equal(canAccessModule("/admin/roles"), false);
    assert.equal(canAccessModule("/me/salary"), true);
});

test("legacy and canonical HR URLs keep the same role boundary", () => {
    for (const roles of [["Employee"], ["HR"], ["Manager"], ["Admin"]]) {
        saveAuth(fixture(roles));
        const allowed = roles[0] !== "Employee";
        for (const path of ["/hr", "/hr/statistics", "/hr/unregistered-detail", "/employees", "/employees/statistics"]) {
            assert.equal(canAccessModule(path), allowed, `${roles[0]}: ${path}`);
        }
        assert.equal(canAccessModule("/me/statistics"), true);
        assert.equal(canAccessModule("/attendance/statistics"), true);
        assert.equal(canAccessModule("/admin/payroll"), roles[0] === "Admin");
    }
});
