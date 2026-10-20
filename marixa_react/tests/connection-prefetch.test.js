import test from "node:test";
import assert from "node:assert/strict";
import { createConnectionManager, recoverableRead, isConnectionFailure } from "../src/services/api/connection.js";
import { workspacePlan, runPrefetch } from "../src/services/api/prefetchPlan.js";
const offlineError = () => Object.assign(new Error("network"), { code: "ERR_NETWORK" });
const fixture = (roles = ["Employee"]) => ({ token: "isolated", employeeId: "employee-A", userId: "user-A", roles });

test("Employee preload only uses own/public reads; HR never preloads Admin payroll/roles", () => {
    const own = workspacePlan(fixture(), "2026-10-20");
    assert.ok(own.some((task) => task.url === "/LeaveLedger/summary/employee-A/year/2026"));
    assert.ok(!own.some((task) => /Employee\/get-all|Role\/|User\/|Payroll\//.test(task.url)));
    const hr = workspacePlan(fixture(["HR"]));
    assert.ok(hr.some((task) => task.url === "/Employee/get-all" && task.paginated));
    assert.ok(!hr.some((task) => /\/Payroll\/|\/Role\//.test(task.url)));
    assert.ok(workspacePlan(fixture(["Admin"])).some((task) => task.url === "/Payroll/get-all"));
    assert.ok(own.every((task) => !/download|ActivationCode|UserRole/.test(task.url)));
    assert.equal(workspacePlan(null).length, 0);
    assert.ok(!workspacePlan({ ...fixture(), employeeId: null }).some((task) => /by-employee|get-by-user/.test(task.url)));
});
test("prefetch has bounded concurrency, survives one forbidden read and reports real progress", async () => {
    let active = 0, maximum = 0; const progress = [];
    const tasks = Array.from({ length: 12 }, (_, id) => ({ id }));
    const result = await runPrefetch(tasks, async ({ id }) => {
        maximum = Math.max(maximum, ++active);
        await new Promise((resolve) => setTimeout(resolve, 2)); active--;
        if (id === 3) throw { response: { status: 403 } };
    }, { concurrency: 3, onProgress: (value) => progress.push(value.completed) });
    assert.equal(maximum, 3); assert.deepEqual(result, { completed: 12, failed: 1, total: 12 });
    assert.deepEqual(progress, Array.from({ length: 12 }, (_, i) => i + 1));
});
test("cancelled prefetch stops scheduling additional private reads", async () => {
    const controller = new AbortController(); let calls = 0;
    await runPrefetch([1, 2, 3, 4], async () => { calls++; controller.abort(); }, { concurrency: 1, signal: controller.signal });
    assert.equal(calls, 1);
});
test("a cold start uses one shared health sequence for simultaneous callers", async () => {
    let clock = 0, calls = 0;
    const manager = createConnectionManager({ now: () => clock, pause: async (ms) => { clock += ms; } });
    manager.configure(async () => { if (++calls < 3) throw { response: { status: 503 } }; });
    await Promise.all([manager.ready(), manager.ready(), manager.ready()]);
    assert.equal(calls, 3); assert.equal(manager.getSnapshot().phase, "online");
    await manager.ready(); assert.equal(calls, 3);
    await manager.ready({ force: true }); assert.equal(calls, 4);
});
test("cold-start wait is bounded and leaves an actionable unavailable state", async () => {
    let clock = 0;
    const manager = createConnectionManager({ now: () => clock, maxWaitMs: 4000, pause: async (ms) => { clock += ms; } });
    manager.configure(async () => { throw offlineError(); });
    await assert.rejects(manager.ready(), /thử lại/);
    assert.equal(manager.getSnapshot().phase, "unavailable");
});
test("offline login fails before sending credentials; retry can recover when online", async () => {
    let online = false, probes = 0;
    const manager = createConnectionManager({ online: () => online });
    manager.configure(async () => { probes++; });
    await assert.rejects(manager.ready(), /mất kết nối/); assert.equal(probes, 0);
    online = true; await manager.ready(); assert.equal(probes, 1);
});
test("GET recovery retries exactly once, but authentication/business/rate-limit failures never retry", async () => {
    let calls = 0, probes = 0;
    const manager = createConnectionManager(); manager.configure(async () => { probes++; });
    const data = await recoverableRead(async () => { if (++calls === 1) throw offlineError(); return "recovered"; }, { manager });
    assert.equal(data, "recovered"); assert.equal(calls, 2); assert.equal(probes, 1);
    for (const status of [400, 401, 403, 409, 429, 500]) {
        let attempts = 0;
        await assert.rejects(recoverableRead(async () => { attempts++; throw { response: { status } }; }, { manager }));
        assert.equal(attempts, 1);
    }
});
test("explicit prefetch/cancellation never schedules an automatic retry", async () => {
    let calls = 0, probes = 0;
    const manager = createConnectionManager(); manager.configure(async () => { probes++; });
    await assert.rejects(recoverableRead(async () => { calls++; throw offlineError(); }, { recover: false, manager }));
    assert.equal(calls, 1); assert.equal(probes, 0);
    assert.equal(isConnectionFailure({ code: "ERR_CANCELED" }), false);
    assert.equal(isConnectionFailure({ name: "AbortError" }), false);
    assert.equal(isConnectionFailure(null), false);
});

test("a cancelled consumer waiting for recovery never replays its GET", async () => {
    const controller = new AbortController(); let reads = 0;
    const manager = createConnectionManager(); manager.configure(async () => { controller.abort(); });
    await assert.rejects(recoverableRead(async () => { reads++; throw offlineError(); }, { manager, signal: controller.signal }));
    assert.equal(reads, 1);
});
test("a GET that still fails after recovery stops and reports unavailable", async () => {
    let reads = 0;
    const manager = createConnectionManager(); manager.configure(async () => {});
    await assert.rejects(recoverableRead(async () => { reads++; throw offlineError(); }, { manager }));
    assert.equal(reads, 2); assert.equal(manager.getSnapshot().phase, "unavailable");
});
test("a slow request ending with a business error does not leave a permanent reconnect spinner", async () => {
    const manager = createConnectionManager(); manager.slow();
    await assert.rejects(recoverableRead(async () => { throw { response: { status: 403 } }; }, { manager }));
    assert.equal(manager.getSnapshot().phase, "online");
});
