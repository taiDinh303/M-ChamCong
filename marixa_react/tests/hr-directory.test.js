import test from "node:test";
import assert from "node:assert/strict";
import { peekPages } from "../src/services/api/contracts.js";
import { createResponseCache } from "../src/services/api/responseCache.js";
import { HR_DIRECTORY, peekDirectory, loadDirectory, verificationRows, isDirectoryRequest } from "../src/modules/hr/api/directoryData.js";
import { workspacePlan } from "../src/services/api/prefetchPlan.js";

const response = (data) => ({ status: 200, data: { data } });
const page = (items, totalPages, hasNextPage) => response({ items, totalPages, hasNextPage });

test("directory can render synchronously from all fresh server pages without fetching", async () => {
    const cache = createResponseCache(); let reads = 0;
    for (const [, url] of HR_DIRECTORY) {
        await cache.read(`${url}:1`, "HR", async () => page([{ id: "A" }], 2, true));
        await cache.read(`${url}:2`, "HR", async () => page([{ id: "B" }], 2, false));
    }
    const data = peekDirectory((url) => peekPages((number) => { reads++; return cache.peek(`${url}:${number}`, "HR"); }));
    assert.equal(reads, 18);
    assert.equal(data.employees.length, 2);
    assert.equal(data.contracts.length, 2);
    assert.deepEqual(data.archivedEmployees[0], { id: "A", status: 4, isArchived: true });
    assert.equal(cache.peek("/Employee/get-archived:1", "HR").data.data.items[0].isArchived, undefined);
});

test("missing, expired or other-session directory pages cannot pretend to be a complete preload", async () => {
    let clock = 0; const cache = createResponseCache({ now: () => clock, freshMs: 10 });
    await cache.read("1", "HR", async () => page([{ id: 1 }], 2, true));
    const peek = (number) => cache.peek(String(number), "HR");
    assert.equal(peekPages(peek), null);
    await cache.read("2", "HR", async () => page([{ id: 2 }], 2, false));
    assert.equal(peekPages(peek).data.data.items.length, 2);
    clock = 11;
    assert.equal(peekPages(peek), null);
    assert.equal(peekPages((number) => cache.peek(String(number), "Other")), null);
});

test("empty cached directories are valid, malformed or missing collections are a miss", () => {
    const empty = peekDirectory(() => page([], 0, false));
    assert.ok(empty); assert.ok(Object.values(empty).every((rows) => rows.length === 0));
    assert.equal(peekDirectory((url) => url.includes("EmployeeContract") ? null : response([])), null);
    assert.equal(peekPages(() => response({ wrong: [] })), null);
    assert.equal(peekPages((number) => number === 1 ? page([1], 3, true) : page([], 3, true)), null);
    assert.deepEqual(peekPages(() => response([1, 2])).data.data, [1, 2]);
});

test("HR directory load does not wait for activation codes and refuses incomplete business results", async () => {
    const urls = [];
    const data = await loadDirectory(async (url) => { urls.push(url); return response([{ id: url }]); });
    assert.equal(Object.keys(data).length, 9);
    assert.ok(!urls.some((url) => url.includes("ActivationCode")));
    await assert.rejects(loadDirectory(async (url) => {
        if (url.includes("EmployeeSalary")) throw new Error("salary failed");
        return response([]);
    }), /salary failed/);
});

test("HR, Manager and Admin preload the entire directory first without duplicate requests", () => {
    for (const role of ["HR", "Manager", "Admin"]) {
        const plan = workspacePlan({ token: "isolated", employeeId: "E", userId: "U", roles: [role] });
        assert.deepEqual(plan.slice(0, 9).map(({ url }) => url), HR_DIRECTORY.map(([, url]) => url));
        assert.ok(plan.slice(0, 9).every(({ paginated }) => paginated));
        assert.equal(new Set(plan.map(({ url }) => url)).size, plan.length);
        assert.ok(!plan.some(({ url }) => url.includes("ActivationCode")));
    }
    assert.ok(!workspacePlan({ token: "isolated", roles: ["Employee"] }).some(({ url }) => url === "/Employee/get-archived"));
});

test("verification retains status metadata without retaining activation secrets", () => {
    const data = verificationRows(response([{ employeeId: "E", isUsed: false, expiresAt: "2026-11-01", code: "do-not-retain", id: "secret-record" }]));
    assert.deepEqual(data, [{ employeeId: "E", isUsed: false, expiresAt: "2026-11-01" }]);
});

test("manual directory refresh invalidates all directory pages while preserving other modules", async () => {
    const cache = createResponseCache();
    const directory = "https://api.example/api/Employee/get-all?pageNumber=2&pageSize=200";
    const personal = "https://api.example/api/EmployeeSalary/by-employee/E";
    const report = "https://api.example/api/EmployeeReport/get-all";
    for (const url of [directory, personal, report]) await cache.read(url, "HR", async () => response([1]));
    cache.invalidateWhere(isDirectoryRequest);
    assert.equal(cache.peek(directory, "HR"), null);
    assert.ok(cache.peek(personal, "HR")); assert.ok(cache.peek(report, "HR"));
});
