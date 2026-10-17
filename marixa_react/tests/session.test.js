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
    assert.equal(canAccessModule("/employees/statistics"), false);
    assert.equal(canAccessModule("/attendance/statistics"), true);
});
test("HR can reach HR routes without gaining admin role management", () => {
    saveAuth(fixture(["HR"]));
    assert.equal(canAccessModule("/employees/statistics"), true);
    assert.equal(canAccessModule("/admin/roles"), false);
    saveAuth(fixture(["Admin"]));
    assert.equal(canAccessModule("/admin/roles"), true);
});
