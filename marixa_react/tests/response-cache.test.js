import test from "node:test";
import assert from "node:assert/strict";
import { createResponseCache, cacheKey, cacheableRead, sessionScope, responseCache } from "../src/services/api/responseCache.js";
import { clearAuth, saveAuth } from "../src/services/auth/auth.js";

const result = (value) => ({ status: 200, data: { data: value } });
const deferred = () => { let resolve, reject; const promise = new Promise((yes, no) => { resolve = yes; reject = no; }); return { promise, resolve, reject }; };
const offline = () => Object.assign(new Error("offline"), { code: "ERR_NETWORK" });
const staleOnError = (error) => error.code === "ERR_NETWORK";

test("prefetched JSON is reused and callers cannot mutate the shared response", async () => {
    const cache = createResponseCache(); let calls = 0;
    const load = async () => { calls++; return result([{ name: "original" }]); };
    const first = await cache.read("employees", "A", load);
    first.data.data[0].name = "editing";
    assert.equal((await cache.read("employees", "A", load)).data.data[0].name, "original");
    assert.equal(calls, 1);
});
test("concurrent identical reads share one request", async () => {
    const cache = createResponseCache(); const gate = deferred(); let calls = 0;
    const load = () => { calls++; return gate.promise; };
    const first = cache.read("same", "A", load); const second = cache.read("same", "A", load);
    gate.resolve(result([1]));
    assert.deepEqual(await first, await second); assert.equal(calls, 1);
});
test("a shared failed request rejects all consumers and allows a clean retry", async () => {
    const cache = createResponseCache(); const gate = deferred(); let calls = 0;
    const load = () => { calls++; return gate.promise; };
    const first = cache.read("same", "A", load); const second = cache.read("same", "A", load);
    gate.reject(offline());
    const results = await Promise.allSettled([first, second]);
    assert.ok(results.every((value) => value.status === "rejected")); assert.equal(calls, 1);
    assert.equal((await cache.read("same", "A", async () => result("ok"))).data.data, "ok");
});
test("changing account never returns previous private data, including a late request", async () => {
    const cache = createResponseCache(); const gate = deferred();
    const old = cache.read("salary", "A", () => gate.promise);
    await cache.read("salary", "B", async () => result("B-private"));
    gate.resolve(result("A-private")); await old;
    assert.equal(cache.peek("salary", "B").data.data, "B-private");
    assert.equal(cache.peek("salary", "A"), null);
});
test("successful mutation invalidation cannot be undone by an older GET finishing late", async () => {
    const cache = createResponseCache(); const gate = deferred();
    const old = cache.read("leaves", "A", () => gate.promise);
    cache.clear();
    await cache.read("leaves", "A", async () => result("after approval"));
    gate.resolve(result("before approval")); await old;
    assert.equal(cache.peek("leaves", "A").data.data, "after approval");
});
test("recording a report view only invalidates report data, preserving unrelated preloaded pages", async () => {
    const cache = createResponseCache();
    await cache.read("/api/EmployeeReport/mine", "A", async () => result("report"));
    await cache.read("/api/EmployeeSalary/by-employee/A", "A", async () => result("salary"));
    cache.invalidateWhere((key) => key.includes("/EmployeeReport/"));
    assert.equal(cache.peek("/api/EmployeeReport/mine", "A"), null);
    assert.equal(cache.peek("/api/EmployeeSalary/by-employee/A", "A").data.data, "salary");
});
test("expired reads refresh; stale cache is only a fallback for connection failure", async () => {
    let now = 0; const cache = createResponseCache({ now: () => now, freshMs: 10, staleMs: 50 });
    await cache.read("list", "A", async () => result("cached")); now = 20;
    const stale = await cache.read("list", "A", async () => { throw offline(); }, { staleOnError });
    assert.equal(stale.data.data, "cached"); assert.equal(stale.fromStaleCache, true);
    for (const status of [401, 403, 429, 500]) {
        const error = { response: { status } };
        await assert.rejects(cache.read("list", "A", async () => { throw error; }, { staleOnError }), (value) => value === error);
    }
    now = 51;
    await assert.rejects(cache.read("list", "A", async () => { throw offline(); }, { staleOnError }));
});
test("manual force-refresh bypasses fresh data and reports errors instead of pretending success", async () => {
    const cache = createResponseCache();
    await cache.read("list", "A", async () => result("old"));
    await assert.rejects(cache.read("list", "A", async () => { throw offline(); }, { force: true, staleOnError }));
    assert.equal((await cache.read("list", "A", async () => result("new"), { force: true })).data.data, "new");
});
test("an earlier pending read cannot replace the result of a later explicit refresh", async () => {
    const cache = createResponseCache(); const gate = deferred();
    const pending = cache.read("same", "A", () => gate.promise);
    await cache.read("same", "A", async () => result("refreshed"), { force: true });
    gate.resolve(result("old")); await pending;
    assert.equal(cache.peek("same", "A").data.data, "refreshed");
});
test("failed or caller-aborted requests do not enter cache or poison later reads", async () => {
    const cache = createResponseCache(); const controller = new AbortController(); const gate = deferred();
    const pending = cache.read("one", "A", () => gate.promise, { signal: controller.signal });
    controller.abort(); gate.resolve(result("discard")); await pending;
    assert.equal(cache.size, 0);
    await assert.rejects(cache.read("one", "A", async () => { throw offline(); }));
    assert.equal(cache.size, 0);
    assert.equal((await cache.read("one", "A", async () => result("recovered"))).data.data, "recovered");
});
test("cache has entry/byte limits and skips oversized responses", async () => {
    const cache = createResponseCache({ maxEntries: 2, maxBytes: 100 });
    for (const key of ["a", "b", "c"]) await cache.read(key, "A", async () => result(key));
    assert.equal(cache.size, 2); assert.equal(cache.peek("a", "A"), null);
    await cache.read("huge", "A", async () => result("x".repeat(1000)));
    assert.equal(cache.peek("huge", "A"), null); assert.ok(cache.bytes <= 100);
});
test("cache keys normalize parameter order; roles/token/employee changes separate the scope", () => {
    assert.equal(cacheKey("/api/items?b=2&a=1"), cacheKey("/api/items?a=1&b=2"));
    const auth = { userId: "A", token: "token-one", employeeId: "employee-A", roles: ["HR", "Employee"] };
    assert.equal(sessionScope(auth), sessionScope({ ...auth, roles: ["Employee", "HR"] }));
    for (const change of [{ token: "token-two" }, { employeeId: "employee-B" }, { roles: ["Employee"] }])
        assert.notEqual(sessionScope(auth), sessionScope({ ...auth, ...change }));
});
test("binary documents, activation codes, role assignments, snapshots and anonymous data bypass cache", () => {
    const auth = { token: "isolated" };
    for (const url of ["/Auth/login", "/ActivationCode/get-all", "/UserRole/get-roles/A", "/EmployeeReport/download/A", "/AttendancePeriod/2026/10/export.xlsx", "/AttendancePeriod/2026/10/snapshot"])
        assert.equal(cacheableRead({ url }, auth), false, url);
    assert.equal(cacheableRead({ url: "/EmployeeSalary/by-employee/A" }, auth), true);
    assert.equal(cacheableRead({ url: "/EmployeeSalary/by-employee/A" }, null), false);
    assert.equal(cacheableRead({ url: "/file", responseType: "blob" }, auth), false);
});
test("logout clears private in-memory data even when the next login would have the same token", async () => {
    const store = () => { const values = new Map(); return { getItem: (key) => values.get(key), setItem: (key, value) => values.set(key, value), removeItem: (key) => values.delete(key) }; };
    globalThis.localStorage = store(); globalThis.sessionStorage = store();
    saveAuth({ token: "isolated", userId: "A" });
    await responseCache.read("private", "A", async () => result("salary"));
    clearAuth(); assert.equal(responseCache.size, 0);
});
